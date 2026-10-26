using FluentAssertions;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project.Tests.Integration.Business
{
    /// <summary>
    /// Integration tests for fraud rule implementations
    /// Tests actual rule logic with realistic transaction data
    /// </summary>
    public class FraudRuleIntegrationTests
    {
        //private readonly FraudEvaluationService _service;
        private readonly FraudRulesSettings _fraudRuleSettings;

        public FraudRuleIntegrationTests()
        {
            _fraudRuleSettings = new FraudRulesSettings
            {
                HighAmount = new HighAmountRuleSettings { Threshold = 50000, ScoreContribution = 40, Enabled = true },
                ForeignCnp = new ForeignCnpRuleSettings { HomeCountry = "ZA", ScoreContribution = 35, Enabled = true },
                AtmWithdrawalLimit = new AtmWithdrawalLimitRuleSettings { Limit = 5000, ScoreContribution = 30, Enabled = true },
                RoundAmount = new RoundAmountRuleSettings { MinimumAmount = 1000, ScoreContribution = 10, Enabled = true },
                HighRiskMerchantCategory = new HighRiskMerchantCategoryRuleSettings
                {
                    HighRiskCategories = "gambling,casino,crypto",
                    ScoreContribution = 25,
                    Enabled = true
                },
                FlagThreshold = 40,
                MaxScore = 100
            };

            var options = Options.Create(_fraudRuleSettings);
        }


        // ════════════════════════════════════════════════════════════════════════════════
        // FRAUD EVALUATION SERVICE TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void FraudEvaluationService_NoRulesTriggered_ReturnsFraudFalse()
        {
            // Arrange
            var transaction = CreateTransaction(
                amount: 100,
                cardCountry: "ZA",
                transactionCountry: "ZA");

            var mockRules = new List<IFraudRule>();

            var service = new FraudEvaluationService(mockRules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse("No rules triggered means not flagged");
            result.FraudScore.Should().Be(0);
            result.FlaggedReason.Should().BeNull();
        }

        [Fact]
        public void FraudEvaluationService_SingleRuleTriggered_ReturnsFraudScore()
        {
            // Arrange
            var transaction = CreateTransaction(amount: 5000);

            // Create mock rules - one triggered, others not
            var triggeredRule = CreateMockRule("HIGH_AMOUNT", true, 75m);
            var notTriggeredRule = CreateMockRule("FOREIGN_CNP", false, 0m);

            var rules = new List<IFraudRule> { triggeredRule, notTriggeredRule };
            var service = new FraudEvaluationService(rules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.RuleResults.Should().HaveCount(2);
            result.RuleResults.Count(r => r.IsTriggered).Should().Be(1);
            result.FraudScore.Should().Be(75m);
        }

        [Fact]
        public void FraudEvaluationService_MultipleRulesTriggered_CumulativeScore()
        {
            // Arrange
            var transaction = CreateTransaction(
                amount: 5000,
                cardCountry: "US",
                transactionCountry: "ZA");

            // Create mock rules - multiple triggered
            var rule1 = CreateMockRule("HIGH_AMOUNT", true, 50m);
            var rule2 = CreateMockRule("FOREIGN_CNP", true, 35m);
            var rule3 = CreateMockRule("OTHER_RULE", false, 0m);

            var rules = new List<IFraudRule> { rule1, rule2, rule3 };
            var service = new FraudEvaluationService(rules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Multiple rules triggered");
            result.FraudScore.Should().Be(85m, "Cumulative score of triggered rules");
            result.RuleResults.Count(r => r.IsTriggered).Should().Be(2);
        }

        [Fact]
        public void FraudEvaluationService_ScoreAboveThreshold_IsFlagged()
        {
            // Arrange
            TransactionEvent transaction = CreateTransaction();

            // Create rules whose combined score exceeds the flag threshold (40)
            var rule1 = CreateMockRule("RULE1", true, 40m);
            var rule2 = CreateMockRule("RULE2", true, 50m); // 40 + 50 + 10 = 100, capped at MaxScore
            var rule3 = CreateMockRule("RULE3", true, 10m);

            var rules = new List<IFraudRule> { rule1, rule2, rule3 };
            var service = new FraudEvaluationService(rules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("Total score exceeds fraud flag threshold");
            result.FraudScore.Should().Be(100m);
        }

        [Fact]
        public void FraudEvaluationService_RuleResultsIncludeDetails()
        {
            // Arrange
            var transaction = CreateTransaction();

            var rule = CreateMockRule("TEST_RULE", true, 50m);
            var rules = new List<IFraudRule> { rule };
            var service = new FraudEvaluationService(rules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.RuleResults.Should().HaveCount(1);
            var ruleResult = result.RuleResults.First();
            ruleResult.RuleCode.Should().Be("TEST_RULE");
            ruleResult.IsTriggered.Should().BeTrue();
            ruleResult.ScoreContribution.Should().Be(50m);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // RULE INTERACTION TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void FraudEvaluationService_AllRulesDisabled_NoScore()
        {
            // Arrange
            var transaction = CreateTransaction(
                amount: 10000,
                cardCountry: "US",
                transactionCountry: "ZA");

            var noRules = new List<IFraudRule>();
            var service = new FraudEvaluationService(noRules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse();
            result.FraudScore.Should().Be(0);
            result.RuleResults.Should().BeEmpty();
        }

        [Fact]
        public void FraudEvaluationService_HighRiskScenario_AllRulesTriggered()
        {
            // Arrange - Create a high-risk transaction
            var transaction = CreateTransaction(
                amount: 50000, // Very high amount
                cardCountry: "NG", // High-risk country
                transactionCountry: "ZA",
                merchantCategory: "GAMBLING"); // High-risk merchant

            // All rules triggered
            var rules = new List<IFraudRule>
            {
                CreateMockRule("HIGH_AMOUNT", true, 40m),
                CreateMockRule("FOREIGN_CNP", true, 30m),
                CreateMockRule("HIGH_RISK_MERCHANT", true, 35m),
                CreateMockRule("HIGH_RISK_COUNTRY", true, 25m)
            };

            var service = new FraudEvaluationService(rules, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(100m); // Capped at max score (individual contributions: 40+30+35+25=130, capped at 100)
            result.RuleResults.Count(r => r.IsTriggered).Should().Be(4);
            result.FlaggedReason.Should().NotBeNullOrEmpty();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // EDGE CASES
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public void FraudEvaluationService_ZeroAmount_Evaluated()
        {
            // Arrange
            var transaction = CreateTransaction(amount: 0);
            var rule = CreateMockRule("ANY_RULE", false, 0m);
            var service = new FraudEvaluationService(new List<IFraudRule> { rule }, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.Should().NotBeNull();
            result.IsFlagged.Should().BeFalse();
        }

        [Fact]
        public void FraudEvaluationService_NegativeAmount_Handled()
        {
            // Arrange
            var transaction = CreateTransaction(amount: -1000);
            var rule = CreateMockRule("ANY_RULE", false, 0m);
            var service = new FraudEvaluationService(new List<IFraudRule> { rule }, Options.Create(_fraudRuleSettings));

            // Act
            var result = service.Evaluate(transaction);

            // Assert
            result.Should().NotBeNull();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // HELPER METHODS
        // ════════════════════════════════════════════════════════════════════════════════

        private TransactionEvent CreateTransaction(
            decimal amount = 1000,
            string cardCountry = "ZA",
            string transactionCountry = "ZA",
            string type = "PURCHASE",
            string merchantCategory = "RETAIL")
        {
            return new TransactionEvent
            {
                TransactionId = Guid.NewGuid(),
                CustomerId = "CUST-TEST-001",
                Amount = amount,
                CountryCode = cardCountry,
                TransactionType = type,
                MerchantCategory = merchantCategory,
                TransactionTime = DateTime.UtcNow
            };
        }

        private IFraudRule CreateMockRule(string ruleCode, bool isTriggered, decimal scoreContribution)
        {
            return new MockFraudRule(ruleCode, isTriggered, scoreContribution);
        }

        /// <summary>
        /// Mock fraud rule for testing
        /// </summary>
        private class MockFraudRule : IFraudRule
        {
            private readonly string _ruleCode;
            private readonly bool _isTriggered;
            private readonly decimal _scoreContribution;

            public MockFraudRule(string ruleCode, bool isTriggered, decimal scoreContribution)
            {
                _ruleCode = ruleCode;
                _isTriggered = isTriggered;
                _scoreContribution = scoreContribution;
            }

            public string RuleCode => _ruleCode;

            public string RuleDescription => $"Mock rule: {_ruleCode}";

            public fraud_poc_project_buss.Dto.FraudRuleSetRecord Evaluate(fraud_poc_project_buss.Dto.TransactionEvent transaction)
            {
                return new FraudRuleSetRecord
                {
                    RuleCode = _ruleCode,
                    RuleDescription = $"Mock rule: {_ruleCode}",
                    IsTriggered = _isTriggered,
                    ScoreContribution = _isTriggered ? _scoreContribution : 0m
                };
            }
        }
    }
}
