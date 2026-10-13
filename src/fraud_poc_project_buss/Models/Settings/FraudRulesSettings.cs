using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    /// <summary>
    /// Configuration settings for fraud detection rules.
    /// These values are loaded from appsettings and can be modified without code changes.
    /// </summary>
    public class FraudRulesSettings
    {
        public const string SectionName = "FraudRules";

        /// <summary>
        /// Settings for the HighAmountRule - flags transactions exceeding this amount
        /// </summary>
        [Required]
        public HighAmountRuleSettings HighAmount { get; set; } = new();

        /// <summary>
        /// Settings for the ForeignCnpRule - flags card-not-present transactions from foreign countries
        /// </summary>
        [Required]
        public ForeignCnpRuleSettings ForeignCnp { get; set; } = new();

        /// <summary>
        /// Settings for the AtmWithdrawalLimitRule - flags ATM withdrawals exceeding this limit
        /// </summary>
        [Required]
        public AtmWithdrawalLimitRuleSettings AtmWithdrawalLimit { get; set; } = new();

        /// <summary>
        /// Settings for the RoundAmountRule - flags large round-number transactions
        /// </summary>
        [Required]
        public RoundAmountRuleSettings RoundAmount { get; set; } = new();

        /// <summary>
        /// Settings for the HighRiskMerchantCategoryRule - flags high-risk merchant categories
        /// </summary>
        [Required]
        public HighRiskMerchantCategoryRuleSettings HighRiskMerchantCategory { get; set; } = new();

        /// <summary>
        /// Overall fraud scoring threshold - transactions with score >= this are flagged
        /// </summary>
        [Range(0, 100)]
        public decimal FlagThreshold { get; set; } = 40m;

        /// <summary>
        /// Maximum possible fraud score (capped at this value)
        /// </summary>
        [Range(1, 1000)]
        public decimal MaxScore { get; set; } = 100m;
    }

    /// <summary>
    /// High Amount Rule Configuration
    /// </summary>
    public class HighAmountRuleSettings
    {
        /// <summary>
        /// Transaction amount threshold in base currency
        /// </summary>
        [Range(0, 999999999.99)]
        public decimal Threshold { get; set; } = 50000m;

        /// <summary>
        /// Score contribution when rule is triggered
        /// </summary>
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 40m;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Foreign CNP Rule Configuration
    /// </summary>
    public class ForeignCnpRuleSettings
    {
        /// <summary>
        /// Home country code (transactions from other countries are flagged)
        /// </summary>
        [Required]
        [StringLength(10)]
        public string HomeCountry { get; set; } = "ZA";

        /// <summary>
        /// Score contribution when rule is triggered
        /// </summary>
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 35m;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// ATM Withdrawal Limit Rule Configuration
    /// </summary>
    public class AtmWithdrawalLimitRuleSettings
    {
        /// <summary>
        /// Single transaction ATM withdrawal limit
        /// </summary>
        [Range(0, 999999999.99)]
        public decimal Limit { get; set; } = 5000m;

        /// <summary>
        /// Score contribution when rule is triggered
        /// </summary>
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 30m;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// Round Amount Rule Configuration
    /// </summary>
    public class RoundAmountRuleSettings
    {
        /// <summary>
        /// Minimum transaction amount to check for round numbers
        /// </summary>
        [Range(0, 999999999.99)]
        public decimal MinimumAmount { get; set; } = 1000m;

        /// <summary>
        /// Amount must be multiple of this value to trigger (e.g., 1000 for thousands)
        /// </summary>
        [Range(1, 999999999.99)]
        public decimal RoundnessMultiple { get; set; } = 1000m;

        /// <summary>
        /// Score contribution when rule is triggered
        /// </summary>
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 10m;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;
    }

    /// <summary>
    /// High Risk Merchant Category Rule Configuration
    /// </summary>
    public class HighRiskMerchantCategoryRuleSettings
    {
        /// <summary>
        /// Comma-separated list of high-risk merchant categories
        /// </summary>
        [Required]
        public string HighRiskCategories { get; set; } = "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer";

        /// <summary>
        /// Score contribution when rule is triggered
        /// </summary>
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 25m;

        /// <summary>
        /// Whether this rule is enabled
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Parse the comma-separated categories into a HashSet
        /// </summary>
        public HashSet<string> GetCategories()
        {
            return new HashSet<string>(
                HighRiskCategories.Split(',')
                    .Select(c => c.Trim().ToLowerInvariant())
                    .Where(c => !string.IsNullOrEmpty(c)),
                StringComparer.OrdinalIgnoreCase);
        }
    }
}
