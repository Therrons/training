using fraud_poc_project_buss.Dto;

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
    /// </summary>
    public class HighAmountRule : IFraudRule
    {
        private readonly decimal _threshold;

        public HighAmountRule(decimal threshold = 50000m)
        {
            _threshold = threshold;
        }

        public string RuleCode => "HIGH_AMOUNT";
        public string RuleDescription => $"Transaction amount exceeds {_threshold:N2}";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            var triggered = Math.Abs(tx.Amount) > _threshold;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? 40m : 0m
            };
        }
    }

    /// <summary>
    /// Flags card-not-present (CNP) transactions originating from outside the home country.
    /// </summary>
    public class ForeignCnpRule : IFraudRule
    {
        private readonly string _homeCountry;

        public ForeignCnpRule(string homeCountry = "ZA")
        {
            _homeCountry = homeCountry;
        }

        public string RuleCode => "FOREIGN_CNP";
        public string RuleDescription => "Card-not-present transaction from a foreign country";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
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
                ScoreContribution = triggered ? 35m : 0m
            };
        }
    }

    /// <summary>
    /// Flags ATM cash withdrawals that exceed the configured single-transaction limit.
    /// </summary>
    public class AtmWithdrawalLimitRule : IFraudRule
    {
        private readonly decimal _limit;

        public AtmWithdrawalLimitRule(decimal limit = 5000m)
        {
            _limit = limit;
        }

        public string RuleCode => "ATM_WITHDRAWAL_LIMIT";
        public string RuleDescription => $"ATM withdrawal exceeds single-transaction limit of {_limit:N2}";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            var isAtm = string.Equals(tx.TransactionType, "ATM", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tx.Channel, "ATM", StringComparison.OrdinalIgnoreCase);

            var triggered = isAtm && Math.Abs(tx.Amount) > _limit;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? 30m : 0m
            };
        }
    }

    /// <summary>
    /// Flags transactions in merchant categories associated with high-risk activity.
    /// </summary>
    public class HighRiskMerchantCategoryRule : IFraudRule
    {
        private static readonly HashSet<string> HighRiskCategories = new(StringComparer.OrdinalIgnoreCase)
        {
            "gambling", "casino", "crypto", "cryptocurrency", "money_transfer", "wire_transfer"
        };

        public string RuleCode => "HIGH_RISK_MERCHANT";
        public string RuleDescription => "Transaction in a high-risk merchant category";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            var triggered = !string.IsNullOrWhiteSpace(tx.MerchantCategory)
                            && HighRiskCategories.Contains(tx.MerchantCategory);
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? 25m : 0m
            };
        }
    }

    /// <summary>
    /// Flags transactions with a round-number amount, which is a common indicator of fraud.
    /// </summary>
    public class RoundAmountRule : IFraudRule
    {
        private readonly decimal _minimumAmount;

        public RoundAmountRule(decimal minimumAmount = 1000m)
        {
            _minimumAmount = minimumAmount;
        }

        public string RuleCode => "ROUND_AMOUNT";
        public string RuleDescription => "Large round-number transaction amount";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            var abs = Math.Abs(tx.Amount);

            // Triggered when the amount is big AND lands exactly on a multiple of 1000
            // (e.g. 5000.00) - fraudsters often pick round numbers.
            var triggered = abs >= _minimumAmount && abs % 1000 == 0;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? 10m : 0m
            };
        }
    }
}
