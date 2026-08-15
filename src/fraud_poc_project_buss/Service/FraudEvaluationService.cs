using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_buss.Service
{
    public interface IFraudEvaluationService
    {
        FraudEventRecord Evaluate(TransactionEvent kafkaEvent);

        Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent);
    }

    // This is the "brain" that decides if a transaction looks like fraud.
    // Steps:
    //   1. Run every fraud rule (see FraudRules.cs) against the transaction.
    //   2. Add up the score points from every rule that triggered.
    //   3. If the total score is 40 or more, flag the transaction as suspicious.
    public class FraudEvaluationService : IFraudEvaluationService
    {
        private readonly IEnumerable<IFraudRule> _rules;

        // A transaction is flagged once its total score reaches this many points.
        private const decimal FlagThreshold = 40m;

        // The highest possible score, even if the rules would add up to more.
        private const decimal MaxScore = 100m;

        public FraudEvaluationService(IEnumerable<IFraudRule> rules)
        {
            _rules = rules ?? Array.Empty<IFraudRule>();
        }

        public FraudEventRecord Evaluate(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            // Step 1: ask every rule what it thinks of this transaction.
            var ruleResults = _rules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            var triggeredRules = ruleResults.Where(result => result.IsTriggered).ToList();

            // Step 2: add up the points from only the rules that triggered, capped at 100.
            var score = triggeredRules.Sum(result => result.ScoreContribution);
            score = Math.Min(score, MaxScore);

            // Step 3: flag it if the score is high enough, and list which rules caused it.
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

        // Same as Evaluate, but runs on a background thread so callers can await it
        // without blocking.
        public async Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent)
         => await Task.Run(() => Evaluate(kafkaEvent));
    }
}
