using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;

namespace fraud_poc_project_buss.Service
{
    public class FraudEvaluationService : IFraudEvaluationService
    {
        private readonly IEnumerable<IFraudRule> _rules;
        private readonly IReadOnlyList<IHistoricalFraudRule> _historicalRules;
        private readonly ICustomerHistoryProvider? _historyProvider;
        private readonly decimal _flagThreshold;
        private readonly decimal _maxScore;
        private readonly TimeSpan _velocityWindow;
        private readonly TimeSpan _lookbackWindow;

        public FraudEvaluationService(
            IEnumerable<IFraudRule> rules,
            IOptions<FraudRulesSettings> options,
            IEnumerable<IHistoricalFraudRule>? historicalRules = null,
            ICustomerHistoryProvider? historyProvider = null)
        {
            _rules = rules ?? Array.Empty<IFraudRule>();
            _historicalRules = (historicalRules ?? Array.Empty<IHistoricalFraudRule>()).ToList();
            _historyProvider = historyProvider;

            var settings = options.Value;
            _flagThreshold = settings.FlagThreshold;
            _maxScore = settings.MaxScore;

            // The history query must cover both the velocity window and the amount baseline
            _velocityWindow = TimeSpan.FromMinutes(settings.VelocityRule.WindowMinutes);
            _lookbackWindow = TimeSpan.FromDays(settings.AmountDeviationRule.LookbackDays);
            if (_lookbackWindow < _velocityWindow)
                _lookbackWindow = _velocityWindow;
        }

        // Runs the transaction-only rules. Rules that need customer history run in EvaluateAsync.
        public FraudEventRecord Evaluate(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            var ruleResults = _rules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            return Build(kafkaEvent, ruleResults);
        }

        // Runs every rule, including the history-based rules.
        public async Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent)
        {
            if (kafkaEvent == null)
                throw new ArgumentNullException(nameof(kafkaEvent));

            var ruleResults = _rules.Select(rule => rule.Evaluate(kafkaEvent)).ToList();
            ruleResults.AddRange(await EvaluateHistoricalRulesAsync(kafkaEvent).ConfigureAwait(false));
            return Build(kafkaEvent, ruleResults);
        }

        private async Task<List<FraudRuleSetRecord>> EvaluateHistoricalRulesAsync(TransactionEvent kafkaEvent)
        {
            // Only query the database when at least one history rule is switched on
            CustomerHistory? history = null;
            if (_historyProvider != null
                && !string.IsNullOrWhiteSpace(kafkaEvent.CustomerId)
                && _historicalRules.Any(rule => rule.IsEnabled))
            {
                var transactionTime = kafkaEvent.TransactionTime;
                history = await _historyProvider.GetHistoryAsync(
                    kafkaEvent.CustomerId,
                    kafkaEvent.TransactionId,
                    transactionTime,
                    transactionTime - _velocityWindow,
                    transactionTime - _lookbackWindow).ConfigureAwait(false);
            }

            return _historicalRules.Select(rule => rule.Evaluate(kafkaEvent, history)).ToList();
        }

        private FraudEventRecord Build(TransactionEvent kafkaEvent, List<FraudRuleSetRecord> ruleResults)
        {
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
    }
}
