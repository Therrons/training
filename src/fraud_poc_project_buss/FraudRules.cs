using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_buss
{
    // This file holds all the "fraud rules". Each rule looks at one transaction
    // and answers a simple question: "does this look suspicious?"
    // If it does, the rule adds some points ("ScoreContribution") to the transaction's
    // overall fraud score. FraudEvaluationService (in the Service folder) adds up the
    // points from every rule to decide if a transaction should be flagged.

    public interface IFraudRule
    {
        // A short code that identifies this rule, e.g. "HIGH_AMOUNT".
        string RuleCode { get; }

        // A human-readable sentence explaining what this rule checks for.
        string RuleDescription { get; }

        // Looks at one transaction and returns whether this rule was triggered.
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
            // Triggered when the transaction's amount (ignoring +/-) is bigger than our limit.
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
            // Card-not-present means the physical card wasn't swiped/tapped (e.g. an online purchase).
            var isCnp = string.Equals(tx.TransactionType, "CNP", StringComparison.OrdinalIgnoreCase)
                        || string.Equals(tx.Channel, "Online", StringComparison.OrdinalIgnoreCase);

            // "Foreign" means the transaction's country isn't our home country.
            var isForeign = !string.IsNullOrWhiteSpace(tx.CountryCode)
                            && !string.Equals(tx.CountryCode, _homeCountry, StringComparison.OrdinalIgnoreCase);

            // Triggered only when both conditions are true at once.
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

            // Triggered only for ATM withdrawals over the limit - not other transaction types.
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
            // Triggered when the merchant's category is one of the risky ones in our list above.
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
    /// Flags transactions that occur at unusual hours (midnight to 4 AM local time).
    /// </summary>
    public class UnusualHoursRule : IFraudRule
    {
        private readonly int _startHour;
        private readonly int _endHour;

        public UnusualHoursRule(int startHour = 0, int endHour = 4)
        {
            _startHour = startHour;
            _endHour = endHour;
        }

        public string RuleCode => "UNUSUAL_HOURS";
        public string RuleDescription => $"Transaction occurred between {_startHour:D2}:00 and {_endHour:D2}:00";

        public FraudRuleSetRecord Evaluate(TransactionEvent tx)
        {
            // Look at just the hour (0-23) the transaction happened at.
            var hour = tx.TransactionTime.Hour;
            var triggered = hour >= _startHour && hour < _endHour;
            return new FraudRuleSetRecord
            {
                RuleCode = RuleCode,
                RuleDescription = RuleDescription,
                IsTriggered = triggered,
                ScoreContribution = triggered ? 15m : 0m
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
