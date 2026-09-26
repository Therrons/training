using FluentAssertions;
using fraud_poc_project_buss;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for RoundAmountRule - flags large round-number transactions.
    /// </summary>
    public class RoundAmountRuleTests
    {
        [Fact]
        public void Evaluate_WithRoundAmountBelowMinimum_DoesNotTrigger()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(500m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithRoundAmountAboveMinimum_Triggers()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(5000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(10m);
        }

        [Fact]
        public void Evaluate_WithNonRoundAmountAboveMinimum_DoesNotTrigger()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(5250m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Amount is not a multiple of 1000");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithAmountEqualToMinimum_AndRound_Triggers()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(1000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("1000 is round and meets minimum");
            result.ScoreContribution.Should().Be(10m);
        }

        [Fact]
        public void Evaluate_WithNegativeRoundAmount_Triggers()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(-5000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Absolute value -5000 is round and meets minimum");
            result.ScoreContribution.Should().Be(10m);
        }

        [Fact]
        public void Evaluate_With10000_Triggers()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(10000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(10m);
        }

        [Fact]
        public void Evaluate_With2000_Triggers()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(2000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(10m);
        }

        [Fact]
        public void Evaluate_With2100_DoesNotTrigger()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(2100m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("2100 is not a multiple of 1000");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithDefaultMinimum_Uses1000()
        {
            // Arrange
            var rule = new RoundAmountRule();
            var transaction = new TransactionEventBuilder()
                .WithAmount(1000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Default minimum is 1000");
        }

        [Fact]
        public void Evaluate_BelowDefaultMinimum_DoesNotTrigger()
        {
            // Arrange
            var rule = new RoundAmountRule();
            var transaction = new TransactionEventBuilder()
                .WithAmount(500m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("500 is below default minimum");
        }

        [Fact]
        public void Evaluate_WithZeroAmount_DoesNotTrigger()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(0m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_ReturnsCorrectRuleMetadata()
        {
            // Arrange
            var rule = new RoundAmountRule(minimumAmount: 1000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(5000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("ROUND_AMOUNT");
            result.RuleDescription.Should().Contain("round-number");
        }
    }
}
