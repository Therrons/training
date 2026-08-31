using fraud_poc_project_buss.Models.Fraud;
using Moq;

namespace fraud_poc_project_buss.Tests.Helpers
{
    /// <summary>
    /// Helper class to build mocked fraud rules for testing FraudEvaluationService.
    /// Simplifies the setup of complex mock objects.
    /// </summary>
    public class MockFraudRuleBuilder
    {
        private readonly List<Mock<IFraudRule>> _rules = new();

        public MockFraudRuleBuilder AddTriggeredRule(
            string ruleCode = "TEST_RULE",
            string ruleDescription = "Test fraud rule",
            decimal scoreContribution = 25m)
        {
            var mock = new Mock<IFraudRule>();
            mock.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns(new FraudRuleSetRecord
                {
                    RuleCode = ruleCode,
                    RuleDescription = ruleDescription,
                    IsTriggered = true,
                    ScoreContribution = scoreContribution
                });

            _rules.Add(mock);
            return this;
        }

        public MockFraudRuleBuilder AddNonTriggeredRule(
            string ruleCode = "TEST_RULE",
            string ruleDescription = "Test fraud rule")
        {
            var mock = new Mock<IFraudRule>();
            mock.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns(new FraudRuleSetRecord
                {
                    RuleCode = ruleCode,
                    RuleDescription = ruleDescription,
                    IsTriggered = false,
                    ScoreContribution = 0m
                });

            _rules.Add(mock);
            return this;
        }

        public MockFraudRuleBuilder AddRuleWithCondition(
            string ruleCode,
            Func<TransactionEvent, bool> condition,
            string ruleDescription = "Conditional fraud rule",
            decimal scoreContribution = 20m)
        {
            var mock = new Mock<IFraudRule>();
            mock.Setup(r => r.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns<TransactionEvent>(tx =>
                {
                    var triggered = condition(tx);
                    return new FraudRuleSetRecord
                    {
                        RuleCode = ruleCode,
                        RuleDescription = ruleDescription,
                        IsTriggered = triggered,
                        ScoreContribution = triggered ? scoreContribution : 0m
                    };
                });

            _rules.Add(mock);
            return this;
        }

        public IEnumerable<IFraudRule> Build()
        {
            return _rules.Select(m => m.Object).ToList();
        }
    }
}
