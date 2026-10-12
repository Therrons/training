using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss
{
    /// <summary>
    /// Extension methods for fraud rule evaluation.
    /// </summary>
    public static class FraudRuleExtensions
    {
        /// <summary>
        /// Creates a standard disabled rule result with IsTriggered=false and ScoreContribution=0m.
        /// All fraud rules that are disabled should return this result to ensure consistent behavior.
        /// </summary>
        public static FraudRuleSetRecord CreateDisabledRuleResult(this IFraudRule rule)
            => new()
            {
                RuleCode = rule.RuleCode,
                RuleDescription = "Rule disabled",
                IsTriggered = false,
                ScoreContribution = 0m
            };
    }
}
