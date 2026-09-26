using FluentAssertions;
using fraud_poc_project_buss;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for AtmWithdrawalLimitRule - flags ATM cash withdrawals exceeding the configured limit.
    /// </summary>
    public class AtmWithdrawalLimitRuleTests
    {
        [Fact]
        public void Evaluate_WithAtmWithdrawalBelowLimit_DoesNotTrigger()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(3000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithAtmWithdrawalExceedingLimit_Triggers()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(30m);
        }

        [Fact]
        public void Evaluate_WithAtmWithdrawalEqualToLimit_DoesNotTrigger()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(5000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Amount equal to limit should not trigger");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithAtmChannelAndHighAmount_Triggers()
        {
            // Arrange - Channel ATM also counts
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithChannel("ATM")
                .WithAmount(6000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Channel ATM + high amount should trigger");
            result.ScoreContribution.Should().Be(30m);
        }

        [Fact]
        public void Evaluate_WithNonAtmTransaction_DoesNotTrigger()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("POS")
                .WithAmount(7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Non-ATM transaction should not trigger regardless of amount");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithAtmAndNegativeAmount_TriggersOnAbsoluteValue()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(-7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Absolute value of -7000 exceeds limit");
            result.ScoreContribution.Should().Be(30m);
        }

        [Fact]
        public void Evaluate_WithDefaultLimit_Uses5000()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule();
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(6000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Default limit is 5000");
        }

        [Fact]
        public void Evaluate_WithCaseSensitivities_TriggersCorrectly()
        {
            // Arrange - Test case-insensitivity
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("atm")
                .WithAmount(7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Rule should be case-insensitive");
            result.ScoreContribution.Should().Be(30m);
        }

        [Fact]
        public void Evaluate_ReturnsCorrectRuleMetadata()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(limit: 5000m);
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("ATM_WITHDRAWAL_LIMIT");
            result.RuleDescription.Should().Contain("5000");
        }
    }
}
