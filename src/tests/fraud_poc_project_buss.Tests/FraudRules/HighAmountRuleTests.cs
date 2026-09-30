using FluentAssertions;
using fraud_poc_project_buss;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for HighAmountRule - flags transactions exceeding the configured amount threshold.
    /// </summary>
    public class HighAmountRuleTests
    {
        [Fact]
        public void Evaluate_WithAmountBelowThreshold_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(10000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
            result.RuleCode.Should().Be("HIGH_AMOUNT");
        }

        [Fact]
        public void Evaluate_WithAmountExceedingThreshold_Triggers()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(75000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(40m);
            result.RuleCode.Should().Be("HIGH_AMOUNT");
        }

        [Fact]
        public void Evaluate_WithAmountEqualToThreshold_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(50000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Amount equal to threshold should not trigger");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithNegativeAmountBelowThreshold_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(-30000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Absolute value -30000 is below threshold");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithNegativeAmountExceedingThreshold_Triggers()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(-75000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Absolute value of -75000 exceeds threshold");
            result.ScoreContribution.Should().Be(40m);
        }

        [Fact]
        public void Evaluate_WithDefaultThreshold_Uses50000()
        {
            // Arrange
            var rule = new HighAmountRule();
            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Default threshold is 50000");
        }

        [Fact]
        public void Evaluate_WithZeroAmount_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighAmountRule(threshold: 50000m);
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
            var rule = new HighAmountRule(threshold: 50000m);
            var transaction = new TransactionEventBuilder()
                .WithAmount(75000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("HIGH_AMOUNT");
            result.RuleDescription.Should().Contain("50000");
        }
    }
}
