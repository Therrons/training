using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_buss.Tests.FraudRules;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project_buss.Tests
{
    // Covers how FraudEvaluationService combines history-based rules with the transaction rules
    public class HistoricalFraudEvaluationTests
    {
        private readonly FraudRulesSettings _settings = new()
        {
            VelocityRule = new VelocityRuleSettings { MaxTransactions = 5, WindowMinutes = 10, ScoreContribution = 30m, Enabled = true },
            AmountDeviationRule = new AmountDeviationRuleSettings { Multiplier = 5m, LookbackDays = 30, MinimumBaselineCount = 10, ScoreContribution = 30m, Enabled = true },
            FlagThreshold = 40m,
            MaxScore = 100m
        };

        private FraudEvaluationService CreateService(ICustomerHistoryProvider? provider, FraudRulesSettings? settings = null)
        {
            var options = Options.Create(settings ?? _settings);
            var historicalRules = new List<IHistoricalFraudRule>
            {
                new VelocityRule(options),
                new AmountDeviationRule(options)
            };
            return new FraudEvaluationService(
                new List<IFraudRule>(),
                options,
                historicalRules,
                provider);
        }

        private static TransactionEvent Transaction(decimal amount = 100m) =>
            new TransactionEventBuilder()
                .WithCustomerId("CUST-1")
                .WithTransactionId(Guid.NewGuid())
                .WithAmount(amount)
                .WithTransactionTime(new DateTime(2026, 11, 18, 12, 0, 0, DateTimeKind.Utc))
                .Build();

        [Fact]
        public async Task EvaluateAsync_PassesWindowsAndCustomerToProvider()
        {
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(0, 0, 0m));
            var transaction = Transaction();

            await CreateService(provider).EvaluateAsync(transaction);

            provider.CallCount.Should().Be(1);
            provider.LastCustomerId.Should().Be("CUST-1");
            provider.LastTransactionId.Should().Be(transaction.TransactionId);
            provider.LastVelocityFrom.Should().Be(transaction.TransactionTime.AddMinutes(-10));
            provider.LastLookbackFrom.Should().Be(transaction.TransactionTime.AddDays(-30));
        }

        [Fact]
        public async Task EvaluateAsync_BurstOfTransactions_FlagsVelocity()
        {
            // 6 earlier transactions in the window: with this one that is 7, over the limit of 5
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(6, 6, 100m));

            var result = await CreateService(provider).EvaluateAsync(Transaction());

            result.RuleResults.Single(r => r.RuleCode == "VELOCITY").IsTriggered.Should().BeTrue();
            result.FraudScore.Should().Be(30m);
            result.IsFlagged.Should().BeFalse("30 is below the flag threshold of 40 on its own");
        }

        [Fact]
        public async Task EvaluateAsync_VelocityAndAmountDeviationTogether_FlagsTransaction()
        {
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(6, 20, 1000m));

            var result = await CreateService(provider).EvaluateAsync(Transaction(amount: 9000m));

            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(60m);
            result.FlaggedReason.Should().Contain("VELOCITY").And.Contain("AMOUNT_DEVIATION");
        }

        [Fact]
        public async Task EvaluateAsync_WhenHistoryRulesDisabled_DoesNotQueryProvider()
        {
            var settings = new FraudRulesSettings
            {
                VelocityRule = new VelocityRuleSettings { Enabled = false },
                AmountDeviationRule = new AmountDeviationRuleSettings { Enabled = false },
                FlagThreshold = 40m,
                MaxScore = 100m
            };
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(50, 50, 1m));

            var result = await CreateService(provider, settings).EvaluateAsync(Transaction(amount: 50000m));

            provider.CallCount.Should().Be(0);
            result.RuleResults.Where(r => r.IsTriggered).Should().BeEmpty();
        }

        [Fact]
        public async Task EvaluateAsync_WhenCustomerIdMissing_DoesNotQueryProvider()
        {
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(50, 50, 1m));
            var transaction = new TransactionEventBuilder().WithCustomerId(null!).WithAmount(100m).Build();

            var result = await CreateService(provider).EvaluateAsync(transaction);

            provider.CallCount.Should().Be(0);
            result.RuleResults.Where(r => r.IsTriggered).Should().BeEmpty();
        }

        [Fact]
        public async Task EvaluateAsync_WhenNoProviderRegistered_HistoryRulesDoNotTrigger()
        {
            var result = await CreateService(provider: null).EvaluateAsync(Transaction(amount: 50000m));

            result.RuleResults.Single(r => r.RuleCode == "VELOCITY").IsTriggered.Should().BeFalse();
            result.RuleResults.Single(r => r.RuleCode == "AMOUNT_DEVIATION").IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_Synchronous_DoesNotRunHistoryRules()
        {
            var provider = new FakeCustomerHistoryProvider(new CustomerHistory(50, 50, 1m));

            var result = CreateService(provider).Evaluate(Transaction());

            provider.CallCount.Should().Be(0);
            result.RuleResults.Should().BeEmpty("no transaction-only rules were registered in this test");
        }

        private sealed class FakeCustomerHistoryProvider : ICustomerHistoryProvider
        {
            private readonly CustomerHistory _history;

            public FakeCustomerHistoryProvider(CustomerHistory history) => _history = history;

            public int CallCount { get; private set; }
            public string? LastCustomerId { get; private set; }
            public Guid LastTransactionId { get; private set; }
            public DateTime LastVelocityFrom { get; private set; }
            public DateTime LastLookbackFrom { get; private set; }

            public Task<CustomerHistory> GetHistoryAsync(
                string customerId,
                Guid transactionId,
                DateTime transactionTime,
                DateTime velocityFrom,
                DateTime lookbackFrom)
            {
                CallCount++;
                LastCustomerId = customerId;
                LastTransactionId = transactionId;
                LastVelocityFrom = velocityFrom;
                LastLookbackFrom = lookbackFrom;
                return Task.FromResult(_history);
            }
        }
    }
}
