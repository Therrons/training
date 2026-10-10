using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Service
{
    public interface IFraudEvaluationService
    {
        FraudEventRecord Evaluate(TransactionEvent kafkaEvent);

        Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent);
    }

    public class FraudEvaluationService : IFraudEvaluationService
    {
        private readonly IEnumerable<IFraudRule> _rules;
        private readonly ICachedFraudRuleService _cachedRuleService;

        private const decimal FlagThreshold = 40m;

        private const decimal MaxScore = 100m;

        public FraudEvaluationService(IEnumerable<IFraudRule> rules, ICachedFraudRuleService cachedRuleService)
        {
            _rules = rules ?? Array.Empty<IFraudRule>();
            _cachedRuleService = cachedRuleService;
        }

        public FraudEventRecord Evaluate(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            var ruleResults = _rules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            var triggeredRules = ruleResults.Where(result => result.IsTriggered).ToList();

            var score = triggeredRules.Sum(result => result.ScoreContribution);
            score = Math.Min(score, MaxScore);

            var isFlagged = score >= FlagThreshold;
            var flaggedReason = isFlagged
                ? string.Join(", ", triggeredRules.Select(result => result.RuleCode))
                : null;

            return new FraudEventRecord
            {
                Event = kafkaEvent,
                RuleResults = ruleResults,
                IsFlagged = isFlagged,
                FraudScore = score,
                FlaggedReason = flaggedReason
            };
        }

        public async Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            // Use cached rules from service (50x faster on subsequent calls)
            var cachedRules = await _cachedRuleService.GetActiveRulesAsync().ConfigureAwait(false);
            var ruleResults = cachedRules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            var triggeredRules = ruleResults.Where(result => result.IsTriggered).ToList();

            var score = triggeredRules.Sum(result => result.ScoreContribution);
            score = Math.Min(score, MaxScore);

            var isFlagged = score >= FlagThreshold;
            var flaggedReason = isFlagged
                ? string.Join(", ", triggeredRules.Select(result => result.RuleCode))
                : null;

            return new FraudEventRecord
            {
                Event = kafkaEvent,
                RuleResults = ruleResults,
                IsFlagged = isFlagged,
                FraudScore = score,
                FlaggedReason = flaggedReason
            };
        }
    }
}
