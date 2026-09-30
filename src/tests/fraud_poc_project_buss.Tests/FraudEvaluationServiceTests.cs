using FluentAssertions;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Service;
using Moq;
using Xunit;

namespace fraud_poc_project_buss.Tests
{
    /// <summary>
    /// Tests for FraudEvaluationService - orchestrates fraud rule evaluation.
    /// Verifies rule aggregation, scoring, and decision logic.
    /// </summary>
    public class FraudEvaluationServiceTests
    {
        private readonly Mock<IFraudRule> _mockRule1;
        private readonly Mock<IFraudRule> _mockRule2;
        private readonly Mock<IFraudRule> _mockRule3;
        private readonly FraudEvaluationService _service;

        public FraudEvaluationServiceTests()
        {
            _mockRule1 = new Mock<IFraudRule>();
            _mockRule2 = new Mock<IFraudRule>();
            _mockRule3 = new Mock<IFraudRule>();

            var rules = new List<IFraudRule> { _mockRule1.Object, _mockRule2.Object, _mockRule3.Object };
            _service = new FraudEvaluationService(rules);
        }

        [Fact]
        public void Evaluate_WithNoRulesFlagged_ReturnsFraudFalse()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE1",
                RuleDescription = "Rule 1",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                RuleDescription = "Rule 2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                RuleDescription = "Rule 3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse();
            result.FraudScore.Should().Be(0m);
            result.FlaggedReason.Should().BeNull();
            result.RuleResults.Should().HaveCount(3);
            result.RuleResults.All(r => !r.IsTriggered).Should().BeTrue();
        }

        [Fact]
        public void Evaluate_WithOneRuleTriggered_ReturnsScoreButNotFlagged()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "HIGH_AMOUNT",
                RuleDescription = "High Amount Rule",
                IsTriggered = true,
                ScoreContribution = 40m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                RuleDescription = "Rule 2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                RuleDescription = "Rule 3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Score is exactly at threshold of 40");
            result.FraudScore.Should().Be(40m);
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
        }

        [Fact]
        public void Evaluate_WithMultipleRulesTriggered_ReturnsCombinedScore()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "HIGH_AMOUNT",
                RuleDescription = "High Amount Rule",
                IsTriggered = true,
                ScoreContribution = 40m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "FOREIGN_CNP",
                RuleDescription = "Foreign CNP Rule",
                IsTriggered = true,
                ScoreContribution = 35m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                RuleDescription = "Rule 3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Combined score is 75, exceeds 40 threshold");
            result.FraudScore.Should().Be(75m);
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
            result.FlaggedReason.Should().Contain("FOREIGN_CNP");
        }

        [Fact]
        public void Evaluate_WithAllRulesTriggered_ReturnsCappedScore()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE1",
                RuleDescription = "Rule 1",
                IsTriggered = true,
                ScoreContribution = 40m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                RuleDescription = "Rule 2",
                IsTriggered = true,
                ScoreContribution = 35m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                RuleDescription = "Rule 3",
                IsTriggered = true,
                ScoreContribution = 30m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(100m, "Score is capped at maximum of 100");
            result.FlaggedReason.Should().Contain("RULE1", "RULE2", "RULE3");
        }

        [Fact]
        public void Evaluate_WithEmptyRulesList_ReturnsFraudFalse()
        {
            // Arrange
            var serviceWithNoRules = new FraudEvaluationService(new List<IFraudRule>());
            var transaction = new TransactionEventBuilder().Build();

            // Act
            var result = serviceWithNoRules.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse();
            result.FraudScore.Should().Be(0m);
            result.RuleResults.Should().BeEmpty();
        }

        [Fact]
        public void Evaluate_PreservesTransactionEvent()
        {
            // Arrange
            var customerId = "CUST-999";
            var accountId = "ACC-111";
            var transaction = new TransactionEventBuilder()
                .WithCustomerId(customerId)
                .WithAccountId(accountId)
                .Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE1",
                RuleDescription = "Rule 1",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                RuleDescription = "Rule 2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                RuleDescription = "Rule 3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.Event.Should().Be(transaction);
            result.Event.CustomerId.Should().Be(customerId);
            result.Event.AccountId.Should().Be(accountId);
        }

        [Fact]
        public void Evaluate_CallsAllRulesWithTransaction()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>())).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE1",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule2.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>())).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>())).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            _service.Evaluate(transaction);

            // Assert
            _mockRule1.Verify(r => r.Evaluate(transaction), Times.Once);
            _mockRule2.Verify(r => r.Evaluate(transaction), Times.Once);
            _mockRule3.Verify(r => r.Evaluate(transaction), Times.Once);
        }

        [Fact]
        public void Evaluate_BelowThresholdIsNotFlagged()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "SMALL_RULE",
                RuleDescription = "Small contribution",
                IsTriggered = true,
                ScoreContribution = 20m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse("Score 20 is below threshold of 40");
            result.FraudScore.Should().Be(20m);
            result.FlaggedReason.Should().BeNull();
        }

        [Fact]
        public void Evaluate_ThrowsOnNullTransaction()
        {
            // Arrange
            TransactionEvent? nullTransaction = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Evaluate(nullTransaction!));
        }

        [Fact]
        public async Task EvaluateAsync_ReturnsResultAsync()
        {
            // Arrange
            var transaction = new TransactionEventBuilder().Build();

            _mockRule1.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE1",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule2.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE2",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            _mockRule3.Setup(r => r.Evaluate(transaction)).Returns(new FraudRuleSetRecord
            {
                RuleCode = "RULE3",
                IsTriggered = false,
                ScoreContribution = 0m
            });

            // Act
            var result = await _service.EvaluateAsync(transaction);

            // Assert
            result.Should().NotBeNull();
            result.IsFlagged.Should().BeFalse();
            result.Event.Should().Be(transaction);
        }
    }
}
