using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;

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
        private readonly decimal _flagThreshold;
        private readonly decimal _maxScore;

        public FraudEvaluationService(
            IEnumerable<IFraudRule> rules,
            IOptions<FraudRulesSettings> options)
        {
            _rules = rules ?? Array.Empty<IFraudRule>();
            var settings = options.Value;
            _flagThreshold = settings.FlagThreshold;
            _maxScore = settings.MaxScore;
        }

        public FraudEventRecord Evaluate(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            var ruleResults = _rules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            var triggeredRules = ruleResults.Where(result => result.IsTriggered).ToList();

            var score = triggeredRules.Sum(result => result.ScoreContribution);
            score = Math.Min(score, _maxScore);

            var isFlagged = score >= _flagThreshold;
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

        public Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent)
            => Task.FromResult(Evaluate(kafkaEvent));

    }
}
