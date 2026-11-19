using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    public class AmountDeviationRuleTests
    {
        private static AmountDeviationRule CreateRule(int minimumBaselineCount = 10, bool enabled = true) =>
            new(Options.Create(new FraudRulesSettings
            {
                AmountDeviationRule = new AmountDeviationRuleSettings
                {
                    Multiplier = 5m,
                    LookbackDays = 30,
                    MinimumBaselineCount = minimumBaselineCount,
                    ScoreContribution = 30m,
                    Enabled = enabled
                }
            }));

        private static TransactionEvent WithAmount(decimal amount) =>
            new TransactionEventBuilder().WithCustomerId("CUST-1").WithAmount(amount).Build();

        [Fact]
        public void Evaluate_WhenAmountIsFarAboveAverage_Triggers()
        {
            // Average 1,000 with multiplier 5 means anything above 5,000 triggers
            var result = CreateRule().Evaluate(WithAmount(6000m), new CustomerHistory(0, 20, 1000m));

            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(30m);
            result.RuleCode.Should().Be("AMOUNT_DEVIATION");
        }

        [Fact]
        public void Evaluate_WhenAmountIsExactlyAtMultiple_DoesNotTrigger()
        {
            var result = CreateRule().Evaluate(WithAmount(5000m), new CustomerHistory(0, 20, 1000m));

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_WhenBaselineHasTooFewTransactions_DoesNotTrigger()
        {
            // Only 3 earlier transactions, below the minimum of 10, so the average is not trusted
            var result = CreateRule().Evaluate(WithAmount(50000m), new CustomerHistory(0, 3, 1000m));

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_WhenAverageIsZero_DoesNotTrigger()
        {
            var result = CreateRule().Evaluate(WithAmount(50000m), new CustomerHistory(0, 20, 0m));

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_WhenHistoryIsNull_DoesNotTrigger()
        {
            var result = CreateRule().Evaluate(WithAmount(50000m), null);

            result.IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_ComparesAbsoluteAmount()
        {
            // A large negative amount (for example a refund) is compared by its size
            var result = CreateRule().Evaluate(WithAmount(-6000m), new CustomerHistory(0, 20, 1000m));

            result.IsTriggered.Should().BeTrue();
        }

        [Fact]
        public void Evaluate_WhenDisabled_NeverTriggers()
        {
            var rule = CreateRule(enabled: false);

            var result = rule.Evaluate(WithAmount(50000m), new CustomerHistory(0, 20, 1000m));

            rule.IsEnabled.Should().BeFalse();
            result.IsTriggered.Should().BeFalse();
        }
    }
}
