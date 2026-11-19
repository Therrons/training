using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;

namespace fraud_poc_project_buss
{
    // A rule that needs the customer's history as well as the transaction itself.
    public interface IHistoricalFraudRule
    {
        string RuleCode { get; }

        string RuleDescription { get; }

        bool IsEnabled { get; }

        // history is null when it could not be loaded. Rules then do not trigger.
        FraudRuleSetRecord Evaluate(TransactionEvent transaction, CustomerHistory? history);
    }


    // Flags a customer who makes too many transactions in a short window.
    // Limit and window are loaded from configuration (appsettings.json).
    public class VelocityRule : IHistoricalFraudRule
    {
        private readonly int _maxTransactions;
        private readonly int _windowMinutes;
        private readonly decimal _scoreContribution;

        public VelocityRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.VelocityRule;
            _maxTransactions = settings.MaxTransactions;
            _windowMinutes = settings.WindowMinutes;
            _scoreContribution = settings.ScoreContribution;
            IsEnabled = settings.Enabled;
        }

        public string RuleCode => "VELOCITY";
        public string RuleDescription =>
            $"More than {_maxTransactions} transactions for this customer within {_windowMinutes} minute(s)";

        public bool IsEnabled { get; }

        public FraudRuleSetRecord Evaluate(TransactionEvent tx, CustomerHistory? history)
        {
            if (!IsEnabled)
                return Disabled();

            // RecentCount excludes the current transaction, so add it back before comparing
            var triggered = history != null && history.RecentCount + 1 > _maxTransactions;

            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }

        private FraudRuleSetRecord Disabled() => new()
        {
            RuleCode = RuleCode,
            RuleDescription = RuleDescription,
            IsTriggered = false,
            ScoreContribution = 0m
        };
    }


    // Flags a transaction whose amount is far above the customer's own average.
    // Multiplier, lookback and minimum history are loaded from configuration (appsettings.json).
    public class AmountDeviationRule : IHistoricalFraudRule
    {
        private readonly decimal _multiplier;
        private readonly int _lookbackDays;
        private readonly int _minimumBaselineCount;
        private readonly decimal _scoreContribution;

        public AmountDeviationRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.AmountDeviationRule;
            _multiplier = settings.Multiplier;
            _lookbackDays = settings.LookbackDays;
            _minimumBaselineCount = settings.MinimumBaselineCount;
            _scoreContribution = settings.ScoreContribution;
            IsEnabled = settings.Enabled;
        }

        public string RuleCode => "AMOUNT_DEVIATION";
        public string RuleDescription =>
            $"Amount is more than {_multiplier:N1} times the customer's average over {_lookbackDays} day(s)";

        public bool IsEnabled { get; }

        public FraudRuleSetRecord Evaluate(TransactionEvent tx, CustomerHistory? history)
        {
            if (!IsEnabled)
                return Disabled();

            // Without enough history the average means little, so the rule stays quiet for new customers
            var triggered = history != null
                            && history.BaselineCount >= _minimumBaselineCount
                            && history.AverageAmount > 0m
                            && Math.Abs(tx.Amount) > history.AverageAmount * _multiplier;

            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }

        private FraudRuleSetRecord Disabled() => new()
        {
            RuleCode = RuleCode,
            RuleDescription = RuleDescription,
            IsTriggered = false,
            ScoreContribution = 0m
        };
    }
}
