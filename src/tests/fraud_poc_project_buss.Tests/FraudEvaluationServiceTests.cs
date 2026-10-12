using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project_buss.Tests
{
    /// <summary>
    /// Integration tests for FraudEvaluationService - orchestrates fraud rule evaluation.
    /// Uses real fraud rules with meaningful transaction data to verify:
    /// - Correct rule aggregation and scoring
    /// - Proper decision logic (flagged/not flagged)
    /// - Score capping and threshold enforcement
    /// - Preservation of transaction data
    /// </summary>
    public class FraudEvaluationServiceTests
    {
        private readonly FraudEvaluationService _service;
        private readonly IOptions<FraudRulesSettings> _options;

        public FraudEvaluationServiceTests()
        {
            var fraudRulesSettings = new FraudRulesSettings
            {
                HighAmount = new HighAmountRuleSettings { Threshold = 50000m, ScoreContribution = 40m, Enabled = true },
                ForeignCnp = new ForeignCnpRuleSettings { HomeCountry = "ZA", ScoreContribution = 35m, Enabled = true },
                AtmWithdrawalLimit = new AtmWithdrawalLimitRuleSettings { Limit = 5000m, ScoreContribution = 30m, Enabled = true },
                RoundAmount = new RoundAmountRuleSettings { MinimumAmount = 1000m, RoundnessMultiple = 1000m, ScoreContribution = 10m, Enabled = true },
                HighRiskMerchantCategory = new HighRiskMerchantCategoryRuleSettings
                {
                    HighRiskCategories = "gambling,casino,crypto",
                    ScoreContribution = 25m,
                    Enabled = true
                },
                FlagThreshold = 40m,
                MaxScore = 100m
            };

            _options = Options.Create(fraudRulesSettings);
            var rules = new List<IFraudRule>
            {
                new HighAmountRule(_options),
                new ForeignCnpRule(_options),
                new AtmWithdrawalLimitRule(_options),
                new RoundAmountRule(_options),
                new HighRiskMerchantCategoryRule(_options)
            };

            _service = new FraudEvaluationService(rules, _options);
        }

        [Fact]
        public void Evaluate_WithNoRulesTriggered_ReturnsFraudFalse()
        {
            // Arrange: Create a low-risk domestic transaction
            var transaction = new TransactionEventBuilder()
                .WithAmount(1000m)                    // Below 50,000 threshold
                .WithTransactionType("POS")           // Not CNP, not ATM
                .WithCountryCode("ZA")                // Domestic
                .WithMerchantCategory("retail")       // Not high-risk
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse("No rules should trigger");
            result.FraudScore.Should().Be(0m, "No score contributions");
            result.FlaggedReason.Should().BeNull("No triggered rules");
            result.RuleResults.Should().HaveCount(5, "All 5 rules evaluated");
            result.RuleResults.All(r => !r.IsTriggered).Should().BeTrue("No rules triggered");
            result.Event.Should().Be(transaction, "Transaction preserved");
        }

        [Fact]
        public void Evaluate_WithOneRuleTriggered_FlagsAtThreshold()
        {
            // Arrange: Create a high-amount domestic transaction (triggers only HighAmountRule)
            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)                   // Exceeds 50,000 threshold → score 40
                .WithTransactionType("POS")           // Not CNP/ATM
                .WithCountryCode("ZA")                // Domestic
                .WithMerchantCategory("retail")       // Not high-risk
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Score 40 equals threshold");
            result.FraudScore.Should().Be(40m, "Only HighAmountRule contributes 40");
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
            var highAmountResult = result.RuleResults.First(r => r.RuleCode == "HIGH_AMOUNT");
            highAmountResult.IsTriggered.Should().BeTrue();
            highAmountResult.ScoreContribution.Should().Be(40m);
        }

        [Fact]
        public void Evaluate_WithMultipleRulesTriggered_AggregatesScore()
        {
            // Arrange: High-amount foreign CNP transaction
            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)                   // HIGH_AMOUNT: +40
                .WithTransactionType("CNP")           // FOREIGN_CNP trigger (with foreign country)
                .WithCountryCode("US")                // Foreign
                .WithMerchantCategory("retail")       // Not high-risk
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Combined score 75 exceeds threshold 40");
            result.FraudScore.Should().Be(75m, "40 from HighAmount + 35 from ForeignCnp");
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
            result.FlaggedReason.Should().Contain("FOREIGN_CNP");

            var triggeredRules = result.RuleResults.Where(r => r.IsTriggered).ToList();
            triggeredRules.Should().HaveCount(2);
            triggeredRules.Sum(r => r.ScoreContribution).Should().Be(75m);
        }

        [Fact]
        public void Evaluate_WithAllRulesTriggered_CapsScopeAtMaximum()
        {
            // Arrange: Maximally suspicious transaction triggering all 5 rules
            // - High amount (60,000) → +40
            // - Foreign CNP (Online + US) → +35
            // - ATM over limit (6,000) → +30
            // - Round amount (5,000) → +10
            // - High-risk merchant (crypto) → +25
            // Total = 140, but capped at 100
            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)                   // HIGH_AMOUNT: +40
                .WithChannel("Online")                // Treated as CNP
                .WithCountryCode("US")                // FOREIGN_CNP: +35
                .WithTransactionType("ATM")           // ATM_WITHDRAWAL_LIMIT: +30 (6000 exceeds 5000)
                .WithMerchantCategory("crypto")       // HIGH_RISK_MERCHANT: +25
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert - Score should be capped at 100
            result.FraudScore.Should().Be(100m, "Score capped at maximum of 100");
            result.IsFlagged.Should().BeTrue();

            // Verify triggered rules
            result.RuleResults.Where(r => r.IsTriggered).Select(r => r.RuleCode)
                .Should().Contain("HIGH_AMOUNT", "FOREIGN_CNP", "HIGH_RISK_MERCHANT");

            // Raw contributions sum to more than 100
            var rawSum = result.RuleResults.Where(r => r.IsTriggered).Sum(r => r.ScoreContribution);
            rawSum.Should().BeGreaterThan(100m);
        }

        [Fact]
        public void Evaluate_PreservesTransactionEventData()
        {
            // Arrange: Transaction with specific customer/account identifiers
            var customerId = "CUST-999";
            var accountId = "ACC-111";
            var transaction = new TransactionEventBuilder()
                .WithCustomerId(customerId)
                .WithAccountId(accountId)
                .WithAmount(1000m)
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert: Original transaction should be preserved in result
            result.Event.Should().Be(transaction);
            result.Event.CustomerId.Should().Be(customerId);
            result.Event.AccountId.Should().Be(accountId);
            result.Event.Amount.Should().Be(1000m);
            result.Event.TransactionType.Should().Be("POS");
        }

        [Fact]
        public void Evaluate_AlwaysEvaluatesAllRules()
        {
            // Arrange: Transaction that doesn't trigger any rules
            var transaction = new TransactionEventBuilder()
                .WithAmount(1000m)
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .WithMerchantCategory("retail")
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert: All 5 rules should be evaluated, even if none trigger
            result.RuleResults.Should().HaveCount(5, "All 5 rules must be evaluated");
            result.RuleResults.Select(r => r.RuleCode)
                .Should().Contain("HIGH_AMOUNT", "FOREIGN_CNP", "ATM_WITHDRAWAL_LIMIT", "ROUND_AMOUNT", "HIGH_RISK_MERCHANT");
        }

        [Fact]
        public void Evaluate_BelowThresholdIsNotFlagged()
        {
            // Arrange: Transaction with small round amount (only RoundAmountRule triggers)
            var transaction = new TransactionEventBuilder()
                .WithAmount(2000m)                    // ROUND_AMOUNT: +10 (only rule triggered)
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .WithMerchantCategory("retail")
                .Build();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert: Score 10 is below threshold 40
            result.IsFlagged.Should().BeFalse("Score 10 is below threshold of 40");
            result.FraudScore.Should().Be(10m);
            result.FlaggedReason.Should().BeNull("No flag reason when not flagged");

            var roundAmountRule = result.RuleResults.First(r => r.RuleCode == "ROUND_AMOUNT");
            roundAmountRule.IsTriggered.Should().BeTrue();
        }

        [Fact]
        public void Evaluate_RoundAmountTriggersOnThousands()
        {
            // Arrange: Multiple round amounts to verify RoundAmountRule logic
            var transaction1 = new TransactionEventBuilder()
                .WithAmount(1000m)                    // Multiple of 1000, ≥ 1000 → triggers
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .Build();

            var transaction2 = new TransactionEventBuilder()
                .WithAmount(5000m)                    // Multiple of 1000, ≥ 1000 → triggers
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .Build();

            var transaction3 = new TransactionEventBuilder()
                .WithAmount(5250m)                    // NOT multiple of 1000 → doesn't trigger
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .Build();

            // Act & Assert
            var result1 = _service.Evaluate(transaction1);
            result1.RuleResults.First(r => r.RuleCode == "ROUND_AMOUNT").IsTriggered.Should().BeTrue();

            var result2 = _service.Evaluate(transaction2);
            result2.RuleResults.First(r => r.RuleCode == "ROUND_AMOUNT").IsTriggered.Should().BeTrue();

            var result3 = _service.Evaluate(transaction3);
            result3.RuleResults.First(r => r.RuleCode == "ROUND_AMOUNT").IsTriggered.Should().BeFalse();
        }

        [Fact]
        public void Evaluate_DisabledRulesDoNotContribute()
        {
            // Arrange: Disabled HighAmountRule - shouldn't trigger even with high amount
            var disabledSettings = new FraudRulesSettings
            {
                HighAmount = new HighAmountRuleSettings { Threshold = 50000m, ScoreContribution = 40m, Enabled = false },
                ForeignCnp = new ForeignCnpRuleSettings { HomeCountry = "ZA", ScoreContribution = 35m, Enabled = true },
                AtmWithdrawalLimit = new AtmWithdrawalLimitRuleSettings { Limit = 5000m, ScoreContribution = 30m, Enabled = true },
                RoundAmount = new RoundAmountRuleSettings { MinimumAmount = 1000m, RoundnessMultiple = 1000m, ScoreContribution = 10m, Enabled = true },
                HighRiskMerchantCategory = new HighRiskMerchantCategoryRuleSettings
                {
                    HighRiskCategories = "gambling,casino,crypto",
                    ScoreContribution = 25m,
                    Enabled = true
                },
                FlagThreshold = 40m,
                MaxScore = 100m
            };

            var options = Options.Create(disabledSettings);
            var rules = new List<IFraudRule>
            {
                new HighAmountRule(options),
                new ForeignCnpRule(options),
                new AtmWithdrawalLimitRule(options),
                new RoundAmountRule(options),
                new HighRiskMerchantCategoryRule(options)
            };
            var service = new FraudEvaluationService(rules, options);

            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)                   // Would trigger HIGH_AMOUNT if enabled
                .WithTransactionType("POS")
                .WithCountryCode("ZA")
                .Build();

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            var highAmountResult = result.RuleResults.First(r => r.RuleCode == "HIGH_AMOUNT");
            highAmountResult.IsTriggered.Should().BeFalse("Rule is disabled");
            highAmountResult.RuleDescription.Should().Be("Rule disabled");
            result.FraudScore.Should().Be(0m, "No contributions from disabled rule");
        }

        [Fact]
        public async Task EvaluateAsync_ReturnsCompletedTask()
        {
            // Arrange
            var transaction = new TransactionEventBuilder()
                .WithAmount(60000m)
                .WithTransactionType("CNP")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = await _service.EvaluateAsync(transaction);

            // Assert: Should complete without error and return valid result
            result.Should().NotBeNull();
            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(75m, "HIGH_AMOUNT (40) + FOREIGN_CNP (35)");
            result.Event.Should().Be(transaction);
        }
    }
}
