using FluentAssertions;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for AtmWithdrawalLimitRule - flags ATM cash withdrawals exceeding the configured limit.
    /// </summary>
    public class AtmWithdrawalLimitRuleTests
    {
        private IOptions<FraudRulesSettings> CreateOptions(decimal limit = 5000m, bool enabled = true)
        {
            var settings = new FraudRulesSettings
            {
                AtmWithdrawalLimit = new AtmWithdrawalLimitRuleSettings
                {
                    Limit = limit,
                    ScoreContribution = 30m,
                    Enabled = enabled
                }
            };
            return Options.Create(settings);
        }

        [Fact]
        public void Evaluate_WithAtmWithdrawalBelowLimit_DoesNotTrigger()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
        public void Evaluate_WithDisabledRule_DoesNotTrigger()
        {
            // Arrange
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m, enabled: false));
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(6000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Disabled rule should not trigger");
            result.ScoreContribution.Should().Be(0m);
            result.RuleDescription.Should().Be("Rule disabled");
        }

        [Fact]
        public void Evaluate_WithCaseSensitivities_TriggersCorrectly()
        {
            // Arrange - Test case-insensitivity
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
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
            var rule = new AtmWithdrawalLimitRule(CreateOptions(5000m));
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("ATM")
                .WithAmount(7000m)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("ATM_WITHDRAWAL_LIMIT");
            result.RuleDescription.Should().Contain("5,000");
        }
    }
}
