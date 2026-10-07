using FluentAssertions;
using fraud_poc_project.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fraud_poc_project.Tests.Integration.Metrics
{
    /// <summary>
    /// Integration tests for MetricsService
    /// Verifies metrics collection, aggregation, and reporting
    /// </summary>
    public class MetricsServiceTests
    {
        private readonly IMetricsService _metricsService;

        public MetricsServiceTests()
        {
            var mockLogger = new Mock<ILogger<MetricsService>>();
            _metricsService = new MetricsService(mockLogger.Object);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // TRANSACTION METRICS TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordTransactionProcessed_SingleTransaction_UpdatesMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordTransactionProcessed(false, 25m, 150);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalTransactions.Should().Be(1);
            snapshot.FlaggedTransactions.Should().Be(0);
            snapshot.FraudRate.Should().Be(0);
        }

        [Fact]
        public void RecordTransactionProcessed_FlaggedTransaction_IncrementsFlaggedCount()
        {
            // Arrange
            // Act
            _metricsService.RecordTransactionProcessed(true, 75m, 200);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalTransactions.Should().Be(1);
            snapshot.FlaggedTransactions.Should().Be(1);
            snapshot.FraudRate.Should().Be(1.0m);
        }

        [Fact]
        public void RecordTransactionProcessed_MultipleTransactions_CalculatesFraudRate()
        {
            // Arrange
            // Act
            _metricsService.RecordTransactionProcessed(false, 10m, 100);
            _metricsService.RecordTransactionProcessed(true, 70m, 150);
            _metricsService.RecordTransactionProcessed(true, 80m, 120);
            _metricsService.RecordTransactionProcessed(false, 20m, 110);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalTransactions.Should().Be(4);
            snapshot.FlaggedTransactions.Should().Be(2);
            snapshot.FraudRate.Should().Be(0.5m); // 2 out of 4
        }

        [Fact]
        public void RecordTransactionProcessed_TracksProcessingTime()
        {
            // Arrange
            // Act
            _metricsService.RecordTransactionProcessed(false, 0, 100);
            _metricsService.RecordTransactionProcessed(false, 0, 200);
            _metricsService.RecordTransactionProcessed(false, 0, 150);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.AvgProcessingTime.Should().Be(150); // (100+200+150)/3
            snapshot.MinProcessingTime.Should().Be(100);
            snapshot.MaxProcessingTime.Should().Be(200);
        }

        [Fact]
        public void RecordTransactionProcessed_CalculatesPercentiles()
        {
            // Arrange
            // Act
            for (int i = 1; i <= 100; i++)
            {
                _metricsService.RecordTransactionProcessed(false, 0, i); // 1ms to 100ms
            }

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.P95ProcessingTime.Should().BeGreaterThanOrEqualTo(90);
            snapshot.P99ProcessingTime.Should().BeGreaterThanOrEqualTo(98);
        }

        [Fact]
        public void RecordTransactionProcessed_CalculatesAverageFraudScore()
        {
            // Arrange
            // Act
            _metricsService.RecordTransactionProcessed(true, 50m, 100);
            _metricsService.RecordTransactionProcessed(true, 75m, 100);
            _metricsService.RecordTransactionProcessed(false, 25m, 100);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.AvgFraudScore.Should().Be(50m); // (50+75+25)/3
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // RULE METRICS TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordRuleExecution_SingleRule_TracksMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordRuleExecution("HIGH_AMOUNT", true, 50);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.RuleMetrics.Should().HaveCount(1);
            var ruleMetric = snapshot.RuleMetrics.First();
            ruleMetric.RuleCode.Should().Be("HIGH_AMOUNT");
            ruleMetric.ExecutionCount.Should().Be(1);
            ruleMetric.TriggeredCount.Should().Be(1);
        }

        [Fact]
        public void RecordRuleExecution_MultipleRules_TracksSeparately()
        {
            // Arrange
            // Act
            _metricsService.RecordRuleExecution("HIGH_AMOUNT", true, 50);
            _metricsService.RecordRuleExecution("FOREIGN_CNP", false, 40);
            _metricsService.RecordRuleExecution("HIGH_AMOUNT", false, 45);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.RuleMetrics.Should().HaveCount(2);

            var highAmountRule = snapshot.RuleMetrics.First(r => r.RuleCode == "HIGH_AMOUNT");
            highAmountRule.ExecutionCount.Should().Be(2);
            highAmountRule.TriggeredCount.Should().Be(1);
            highAmountRule.TriggeredRate.Should().Be(0.5m);

            var foreignCnpRule = snapshot.RuleMetrics.First(r => r.RuleCode == "FOREIGN_CNP");
            foreignCnpRule.ExecutionCount.Should().Be(1);
            foreignCnpRule.TriggeredCount.Should().Be(0);
            foreignCnpRule.TriggeredRate.Should().Be(0);
        }

        [Fact]
        public void RecordRuleExecution_CalculatesAverageExecutionTime()
        {
            // Arrange
            // Act
            _metricsService.RecordRuleExecution("TEST_RULE", true, 100);
            _metricsService.RecordRuleExecution("TEST_RULE", false, 200);
            _metricsService.RecordRuleExecution("TEST_RULE", true, 150);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            var ruleMetric = snapshot.RuleMetrics.First();
            ruleMetric.AvgExecutionTime.Should().Be(150); // (100+200+150)/3
            ruleMetric.TotalExecutionTime.Should().Be(450);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // AUTHENTICATION METRICS TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordAuthenticationAttempt_SuccessfulLogin_TracksMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordAuthenticationAttempt(true);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalAuthenticationAttempts.Should().Be(1);
            snapshot.SuccessfulAuthentications.Should().Be(1);
            snapshot.AuthenticationSuccessRate.Should().Be(1.0m);
        }

        [Fact]
        public void RecordAuthenticationAttempt_FailedLogin_UpdatesMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordAuthenticationAttempt(true);
            _metricsService.RecordAuthenticationAttempt(false);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalAuthenticationAttempts.Should().Be(2);
            snapshot.SuccessfulAuthentications.Should().Be(1);
            snapshot.AuthenticationSuccessRate.Should().Be(0.5m);
        }

        [Fact]
        public void RecordAuthenticationAttempt_MultipleAttempts_CalculatesSuccessRate()
        {
            // Arrange
            // Act - 7 successful, 3 failed = 70% success rate
            for (int i = 0; i < 7; i++)
                _metricsService.RecordAuthenticationAttempt(true);
            for (int i = 0; i < 3; i++)
                _metricsService.RecordAuthenticationAttempt(false);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalAuthenticationAttempts.Should().Be(10);
            snapshot.SuccessfulAuthentications.Should().Be(7);
            snapshot.AuthenticationSuccessRate.Should().Be(0.7m);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // DATABASE METRICS TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordDatabaseOperation_SingleOperation_TracksMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordDatabaseOperation("SELECT", 50);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.DatabaseOperationMetrics.Should().HaveCount(1);
            var opMetric = snapshot.DatabaseOperationMetrics.First();
            opMetric.OperationType.Should().Be("SELECT");
            opMetric.Count.Should().Be(1);
            opMetric.TotalTime.Should().Be(50);
            opMetric.AvgTime.Should().Be(50);
        }

        [Fact]
        public void RecordDatabaseOperation_MultipleOperationTypes_TracksSeparately()
        {
            // Arrange
            // Act
            _metricsService.RecordDatabaseOperation("SELECT", 50);
            _metricsService.RecordDatabaseOperation("INSERT", 100);
            _metricsService.RecordDatabaseOperation("SELECT", 75);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.DatabaseOperationMetrics.Should().HaveCount(2);

            var selectOps = snapshot.DatabaseOperationMetrics.First(m => m.OperationType == "SELECT");
            selectOps.Count.Should().Be(2);
            selectOps.AvgTime.Should().Be(62); // (50+75)/2

            var insertOps = snapshot.DatabaseOperationMetrics.First(m => m.OperationType == "INSERT");
            insertOps.Count.Should().Be(1);
            insertOps.TotalTime.Should().Be(100);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // KAFKA METRICS TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordKafkaEvent_SingleEvent_TracksMetrics()
        {
            // Arrange
            // Act
            _metricsService.RecordKafkaEvent("PRODUCE", 30);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.KafkaOperationMetrics.Should().HaveCount(1);
            var kafkaMetric = snapshot.KafkaOperationMetrics.First();
            kafkaMetric.OperationType.Should().Be("PRODUCE");
            kafkaMetric.Count.Should().Be(1);
        }

        [Fact]
        public void RecordKafkaEvent_MultipleEvents_TracksSeparately()
        {
            // Arrange
            // Act
            _metricsService.RecordKafkaEvent("PRODUCE", 30);
            _metricsService.RecordKafkaEvent("CONSUME", 40);
            _metricsService.RecordKafkaEvent("PRODUCE", 35);

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.KafkaOperationMetrics.Should().HaveCount(2);

            var produceOps = snapshot.KafkaOperationMetrics.First(m => m.OperationType == "PRODUCE");
            produceOps.Count.Should().Be(2);
            produceOps.AvgTime.Should().Be(32); // (30+35)/2

            var consumeOps = snapshot.KafkaOperationMetrics.First(m => m.OperationType == "CONSUME");
            consumeOps.Count.Should().Be(1);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // SNAPSHOT AND RESET TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void GetSnapshot_WithNoData_ReturnsDefaults()
        {
            // Act
            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalTransactions.Should().Be(0);
            snapshot.FlaggedTransactions.Should().Be(0);
            snapshot.TotalAuthenticationAttempts.Should().Be(0);
            snapshot.RuleMetrics.Should().BeEmpty();
            snapshot.DatabaseOperationMetrics.Should().BeEmpty();
            snapshot.KafkaOperationMetrics.Should().BeEmpty();
        }

        [Fact]
        public void GetSnapshot_IncludesTimestamp()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var snapshot = _metricsService.GetSnapshot();

            var after = DateTime.UtcNow;

            // Assert
            snapshot.Timestamp.Should().BeOnOrAfter(before);
            snapshot.Timestamp.Should().BeOnOrBefore(after.AddSeconds(1));
        }

        [Fact]
        public void Reset_ClearsAllMetrics()
        {
            // Arrange
            _metricsService.RecordTransactionProcessed(true, 50m, 100);
            _metricsService.RecordAuthenticationAttempt(true);
            _metricsService.RecordRuleExecution("TEST", true, 50);
            var beforeReset = _metricsService.GetSnapshot();
            beforeReset.TotalTransactions.Should().Be(1);

            // Act
            _metricsService.Reset();

            var afterReset = _metricsService.GetSnapshot();

            // Assert
            afterReset.TotalTransactions.Should().Be(0);
            afterReset.FlaggedTransactions.Should().Be(0);
            afterReset.TotalAuthenticationAttempts.Should().Be(0);
            afterReset.RuleMetrics.Should().BeEmpty();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // STRESS AND EDGE CASES
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void RecordTransactionProcessed_HighVolume_HandlesEfficiently()
        {
            // Arrange
            const int transactionCount = 10000;

            // Act
            for (int i = 0; i < transactionCount; i++)
            {
                bool isFlagged = i % 100 == 0; // 1% fraud rate
                _metricsService.RecordTransactionProcessed(isFlagged, isFlagged ? 75m : 10m, 100);
            }

            var snapshot = _metricsService.GetSnapshot();

            // Assert
            snapshot.TotalTransactions.Should().Be(transactionCount);
            snapshot.FlaggedTransactions.Should().Be(100); // 1% of 10000
            snapshot.FraudRate.Should().BeApproximately(0.01m, 0.001m);
        }

        [Fact]
        public void MetricsSnapshot_ToString_FormatsCorrectly()
        {
            // Arrange
            _metricsService.RecordTransactionProcessed(true, 50m, 100);
            _metricsService.RecordAuthenticationAttempt(true);
            _metricsService.RecordRuleExecution("TEST_RULE", true, 50);

            var snapshot = _metricsService.GetSnapshot();

            // Act
            var formattedString = snapshot.ToString();

            // Assert
            formattedString.Should().Contain("FRAUD DETECTION METRICS");
            formattedString.Should().Contain("Total Transactions");
            formattedString.Should().Contain("Fraud Rate");
            formattedString.Should().Contain("AUTHENTICATION METRICS");
            formattedString.Should().Contain("RULE EXECUTION METRICS");
        }
    }
}
