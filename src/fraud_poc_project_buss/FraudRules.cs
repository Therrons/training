using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Options;

namespace fraud_poc_project_buss
{
    public interface IFraudRule
    {
        string RuleCode { get; }

        string RuleDescription { get; }

        FraudRuleSetRecord Evaluate(TransactionEvent transaction);
    }

    /// <summary>
    /// Flags transactions whose absolute amount exceeds the configured threshold.
    /// Threshold is loaded from configuration (appsettings.json).
    /// </summary>
    public class HighAmountRule : IFraudRule
    {
        private readonly decimal _threshold;
        private readonly decimal _scoreContribution;
        private readonly bool _enabled;

        public HighAmountRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.HighAmount;
            _threshold = settings.Threshold;
            _scoreContribution = settings.ScoreContribution;
            _enabled = settings.Enabled;
        }

        public string RuleCode => "HIGH_AMOUNT";
        public string RuleDescription => $"Transaction amount exceeds {_threshold:N2}";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            if (!_enabled)
                return new FraudRuleSetRecord
                {
                    RuleCode = RuleCode,
                    RuleDescription = "Rule disabled",
                    IsTriggered = false,
                    ScoreContribution = 0m
                };

            var triggered = Math.Abs(tx.Amount) > _threshold;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }
    }

    /// <summary>
    /// Flags card-not-present (CNP) transactions originating from outside the home country.
    /// Home country is loaded from configuration (appsettings.json).
    /// </summary>
    public class ForeignCnpRule : IFraudRule
    {
        private readonly string _homeCountry;
        private readonly decimal _scoreContribution;
        private readonly bool _enabled;

        public ForeignCnpRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.ForeignCnp;
            _homeCountry = settings.HomeCountry;
            _scoreContribution = settings.ScoreContribution;
            _enabled = settings.Enabled;
        }

        public string RuleCode => "FOREIGN_CNP";
        public string RuleDescription => "Card-not-present transaction from a foreign country";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            if (!_enabled)
                return new FraudRuleSetRecord
                {
                    RuleCode = RuleCode,
                    RuleDescription = "Rule disabled",
                    IsTriggered = false,
                    ScoreContribution = 0m
                };

            var isCnp = string.Equals(tx.TransactionType, "CNP", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tx.Channel, "Online", StringComparison.OrdinalIgnoreCase);

            var isForeign = !string.IsNullOrWhiteSpace(tx.CountryCode)
                            && !string.Equals(tx.CountryCode, _homeCountry, StringComparison.OrdinalIgnoreCase);

            var triggered = isCnp && isForeign;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }
    }

    /// <summary>
    /// Flags ATM cash withdrawals that exceed the configured single-transaction limit.
    /// Limit is loaded from configuration (appsettings.json).
    /// </summary>
    public class AtmWithdrawalLimitRule : IFraudRule
    {
        private readonly decimal _limit;
        private readonly decimal _scoreContribution;
        private readonly bool _enabled;

        public AtmWithdrawalLimitRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.AtmWithdrawalLimit;
            _limit = settings.Limit;
            _scoreContribution = settings.ScoreContribution;
            _enabled = settings.Enabled;
        }

        public string RuleCode => "ATM_WITHDRAWAL_LIMIT";
        public string RuleDescription => $"ATM withdrawal exceeds single-transaction limit of {_limit:N2}";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            if (!_enabled)
                return new FraudRuleSetRecord
                {
                    RuleCode = RuleCode,
                    RuleDescription = "Rule disabled",
                    IsTriggered = false,
                    ScoreContribution = 0m
                };

            var isAtm = string.Equals(tx.TransactionType, "ATM", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tx.Channel, "ATM", StringComparison.OrdinalIgnoreCase);

            var triggered = isAtm && Math.Abs(tx.Amount) > _limit;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }
    }

    /// <summary>
    /// Flags transactions in merchant categories associated with high-risk activity.
    /// Categories are loaded from configuration (appsettings.json).
    /// </summary>
    public class HighRiskMerchantCategoryRule : IFraudRule
    {
        private readonly HashSet<string> _highRiskCategories;
        private readonly decimal _scoreContribution;
        private readonly bool _enabled;

        public HighRiskMerchantCategoryRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.HighRiskMerchantCategory;
            _highRiskCategories = settings.GetCategories();
            _scoreContribution = settings.ScoreContribution;
            _enabled = settings.Enabled;
        }

        public string RuleCode => "HIGH_RISK_MERCHANT";
        public string RuleDescription => "Transaction in a high-risk merchant category";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            if (!_enabled)
                return new FraudRuleSetRecord
                {
                    RuleCode = RuleCode,
                    RuleDescription = "Rule disabled",
                    IsTriggered = false,
                    ScoreContribution = 0m
                };

            var triggered = !string.IsNullOrWhiteSpace(tx.MerchantCategory)
                            && _highRiskCategories.Contains(tx.MerchantCategory);
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }
    }

    /// <summary>
    /// Flags transactions with a round-number amount, which is a common indicator of fraud.
    /// Minimum amount threshold and roundness multiple are loaded from configuration (appsettings.json).
    /// </summary>
    public class RoundAmountRule : IFraudRule
    {
        private readonly decimal _minimumAmount;
        private readonly decimal _roundnessMultiple;
        private readonly decimal _scoreContribution;
        private readonly bool _enabled;

        public RoundAmountRule(IOptions<FraudRulesSettings> options)
        {
            var settings = options.Value.RoundAmount;
            _minimumAmount = settings.MinimumAmount;
            _roundnessMultiple = settings.RoundnessMultiple;
            _scoreContribution = settings.ScoreContribution;
            _enabled = settings.Enabled;
        }

        public string RuleCode => "ROUND_AMOUNT";
        public string RuleDescription => "Large round-number transaction amount";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            if (!_enabled)
                return new FraudRuleSetRecord
                {
                    RuleCode = RuleCode,
                    RuleDescription = "Rule disabled",
                    IsTriggered = false,
                    ScoreContribution = 0m
                };

            var abs = Math.Abs(tx.Amount);

            // Triggered when the amount is >= minimum AND is a multiple of roundness multiple
            // (e.g. 5000.00 with multiple of 1000) - fraudsters often pick round numbers.
            var triggered = abs >= _minimumAmount && abs % _roundnessMultiple == 0;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? _scoreContribution : 0m
            };
        }
    }
}
