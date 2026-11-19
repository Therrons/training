using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    public class VelocityRuleTests
    {
        private static VelocityRule CreateRule(int maxTransactions = 5, bool enabled = true) =>
            new(Options.Create(new FraudRulesSettings
            {
                VelocityRule = new VelocityRuleSettings
                {
                    MaxTransactions = maxTransactions,
                    WindowMinutes = 10,
                    ScoreContribution = 30m,
                    Enabled = enabled
                }
            }));

        private static TransactionEvent AnyTransaction() =>
            new TransactionEventBuilder().WithCustomerId("CUST-1").WithAmount(100m).Build();

        [Fact]
        public void Evaluate_WhenCountIncludingCurrentIsAtLimit_DoesNotTrigger()
        {
            // 4 earlier + this one = 5, which equals the limit of 5
            var result = CreateRule().Evaluate(AnyTransaction(), new CustomerHistory(4, 4, 100m));

            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WhenCountIncludingCurrentExceedsLimit_Triggers()
        {
            // 5 earlier + this one = 6, which is over the limit of 5
            var result = CreateRule().Evaluate(AnyTransaction(), new CustomerHistory(5, 5, 100m));

            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(30m);
            result.RuleCode.Should().Be("VELOCITY");
        }

        [Fact]
        public void Evaluate_WithNoHistory_DoesNotTrigger()
        {
            var result = CreateRule().Evaluate(AnyTransaction(), new CustomerHistory(0, 0, 0m));

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_WhenHistoryIsNull_DoesNotTrigger()
        {
            var result = CreateRule().Evaluate(AnyTransaction(), null);

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_WhenDisabled_NeverTriggers()
        {
            var rule = CreateRule(enabled: false);

            var result = rule.Evaluate(AnyTransaction(), new CustomerHistory(50, 50, 100m));

            rule.IsEnabled.Should().BeFalse();
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }
    }
}
