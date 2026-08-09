using fraud_poc_project_buss.Models.Fraud;

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
        private const decimal FlagThreshold = 40m;

        public FraudEvaluationService(IEnumerable<IFraudRule> rules)
        {
            _rules = rules ?? Array.Empty<IFraudRule>();
        }

        public FraudEventRecord Evaluate(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            var ruleResults = _rules.Select(r => r.Evaluate(kafkaEvent)).ToList();

            var score = ruleResults
                .Where(r => r.IsTriggered)
                .Sum(r => r.ScoreContribution);

            // Cap score at 100
            score = Math.Min(score, 100m);

            var isFlagged = score >= FlagThreshold;

            var flaggedReason = isFlagged
                ? string.Join(", ", ruleResults.Where(r => r.IsTriggered).Select(r => r.RuleCode))
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
         => await Task.Run(() => Evaluate(kafkaEvent));
    }
}
