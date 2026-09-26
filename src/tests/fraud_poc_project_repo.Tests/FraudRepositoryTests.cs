using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_repo.Interfaces;
using Moq;
using Xunit;

namespace fraud_poc_project_repo.Tests
{
    /// <summary>
    /// Tests for IFraudRepository interface.
    /// These are contract tests that verify the repository interface behaves as expected.
    /// Integration tests with actual database should be in a separate file.
    /// </summary>
    public class FraudRepositoryTests
    {
        private readonly Mock<IFraudRepository> _mockRepository;

        public FraudRepositoryTests()
        {
            _mockRepository = new Mock<IFraudRepository>();
        }

        [Fact]
        public async Task SaveFraudEvaluationAsync_WithValidRecord_ReturnsEventId()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();
            var ruleResults = new List<FraudRuleSetRecord>
            {
                new FraudRuleSetRecord
                {
                    RuleCode = "HIGH_AMOUNT",
                    RuleDescription = "High Amount Rule",
                    IsTriggered = true,
                    ScoreContribution = 40m
                }
            };

            var fraudRecord = new FraudEventRecord
            {
                Event = transaction,
                RuleResults = ruleResults,
                IsFlagged = true,
                FraudScore = 40m,
                FlaggedReason = "HIGH_AMOUNT"
            };

            const long expectedEventId = 123;

            _mockRepository
                .Setup(r => r.SaveFraudEvaluationAsync(fraudRecord))
                .ReturnsAsync(expectedEventId);

            // Act
            var result = await _mockRepository.Object.SaveFraudEvaluationAsync(fraudRecord);

            // Assert
            result.Should().Be(expectedEventId);
            _mockRepository.Verify(r => r.SaveFraudEvaluationAsync(fraudRecord), Times.Once);
        }

        [Fact]
        public async Task SavedltErrorAsync_WithValidParameters_CallsRepository()
        {
            // Arrange
            const string topic = "transactions";
            const string messageData = "{ \"transactionId\": \"TXN-001\" }";
            const string error = "Failed to process message";

            _mockRepository
                .Setup(r => r.SavedltErrorAsync(topic, messageData, error))
                .Returns(Task.CompletedTask);

            // Act
            await _mockRepository.Object.SavedltErrorAsync(topic, messageData, error);

            // Assert
            _mockRepository.Verify(r => r.SavedltErrorAsync(topic, messageData, error), Times.Once);
        }

        [Fact]
        public async Task QueryFraudEventsAsync_WithValidQuery_ReturnsEvents()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                CustomerId = "CUST-001"
            };

            var expectedRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder().WithCustomerId("CUST-001").Build())
                    .WithIsFlagged(true)
                    .WithFraudScore(50m)
                    .Build(),
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder().WithCustomerId("CUST-001").Build())
                    .WithIsFlagged(false)
                    .WithFraudScore(20m)
                    .Build()
            };

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(expectedRecords);

            // Act
            var result = await _mockRepository.Object.QueryFraudEventsAsync(query);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r => r.Event.CustomerId.Should().Be("CUST-001"));
            _mockRepository.Verify(r => r.QueryFraudEventsAsync(query), Times.Once);
        }

        [Fact]
        public async Task QueryFraudEventsAsync_WithEmptyResult_ReturnsEmptyList()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                CustomerId = "NON-EXISTENT"
            };

            var emptyRecords = new List<FraudEventRecord>();

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(emptyRecords);

            // Act
            var result = await _mockRepository.Object.QueryFraudEventsAsync(query);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task QueryFlaggedOnlyFraudEventsAsync_ReturnsOnlyFlaggedEvents()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                IsFlaggedOnly = true
            };

            var flaggedRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithIsFlagged(true)
                    .WithFraudScore(50m)
                    .Build()
            };

            _mockRepository
                .Setup(r => r.QueryFlaggedOnlyFraudEventsAsync(query))
                .ReturnsAsync(flaggedRecords);

            // Act
            var result = await _mockRepository.Object.QueryFlaggedOnlyFraudEventsAsync(query);

            // Assert
            result.Should().HaveCount(1);
            result.All(r => r.IsFlagged).Should().BeTrue();
        }

        [Fact]
        public async Task GetRuleResultsForEventAsync_WithValidEventId_ReturnsRuleResults()
        {
            // Arrange
            const long fraudEventId = 123;

            var ruleResults = new List<FraudRuleSetRecord>
            {
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "HIGH_AMOUNT",
                    RuleDescription = "High Amount Rule",
                    IsTriggered = true,
                    ScoreContribution = 40m
                },
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "FOREIGN_CNP",
                    RuleDescription = "Foreign CNP Rule",
                    IsTriggered = false,
                    ScoreContribution = 0m
                }
            };

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(ruleResults);

            // Act
            var result = await _mockRepository.Object.GetRuleResultsForEventAsync(fraudEventId);

            // Assert
            result.Should().HaveCount(2);
            result.Should().AllSatisfy(r => r.FraudEventId.Should().Be(fraudEventId));
            result.First().IsTriggered.Should().BeTrue();
            result.Last().IsTriggered.Should().BeFalse();
        }

        [Fact]
        public async Task GetRuleResultsForEventAsync_WithNoRuleResults_ReturnsEmptyList()
        {
            // Arrange
            const long fraudEventId = 999;
            var emptyResults = new List<FraudRuleSetRecord>();

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(emptyResults);

            // Act
            var result = await _mockRepository.Object.GetRuleResultsForEventAsync(fraudEventId);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task QueryFraudEventsAsync_WithMinFraudScore_FiltersCorrectly()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                MinFraudScore = 50m
            };

            var highScoreRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder().WithFraudScore(75m).Build(),
                new FraudEventRecordBuilder().WithFraudScore(60m).Build()
            };

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(highScoreRecords);

            // Act
            var result = await _mockRepository.Object.QueryFraudEventsAsync(query);

            // Assert
            result.Should().AllSatisfy(r => r.FraudScore.Should().BeGreaterThanOrEqualTo(50m));
        }

        [Fact]
        public async Task QueryFraudEventsAsync_WithTransactionType_FiltersCorrectly()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                TransactionType = "CNP"
            };

            var cnpRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder().WithTransactionType("CNP").Build())
                    .Build()
            };

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(cnpRecords);

            // Act
            var result = await _mockRepository.Object.QueryFraudEventsAsync(query);

            // Assert
            result.Should().AllSatisfy(r => r.Event.TransactionType.Should().Be("CNP"));
        }
    }
}
