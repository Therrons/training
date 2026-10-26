using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{

    public class FraudRulesSettings
    {
        public const string SectionName = "FraudRules";

        // Settings for the HighAmountRule - flags transactions exceeding this amount
        [Required]
        public HighAmountRuleSettings HighAmount { get; set; } = new();


        // Settings for the ForeignCnpRule - flags card-not-present transactions from foreign countries
        [Required]
        public ForeignCnpRuleSettings ForeignCnp { get; set; } = new();


        // Settings for the AtmWithdrawalLimitRule - flags ATM withdrawals exceeding this limit
        [Required]
        public AtmWithdrawalLimitRuleSettings AtmWithdrawalLimit { get; set; } = new();


        // Settings for the RoundAmountRule - flags large round-number transactions
        [Required]
        public RoundAmountRuleSettings RoundAmount { get; set; } = new();


        // Settings for the HighRiskMerchantCategoryRule - flags high-risk merchant categories
        [Required]
        public HighRiskMerchantCategoryRuleSettings HighRiskMerchantCategory { get; set; } = new();


        // Overall fraud scoring threshold - transactions with score >= this are flagged
        [Range(0, 100)]
        public decimal FlagThreshold { get; set; } = 40m;


        // Maximum possible fraud score (capped at this value)
        [Range(1, 1000)]
        public decimal MaxScore { get; set; } = 100m;
    }


    // High Amount Rule Configuration
    public class HighAmountRuleSettings
    {

        // Transaction amount threshold in base currency
        [Range(0, 999999999.99)]
        public decimal Threshold { get; set; } = 50000m;


        // Score contribution when rule is triggered
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 40m;


        // Whether this rule is enabled
        public bool Enabled { get; set; } = true;
    }


    // Foreign CNP Rule Configuration
    public class ForeignCnpRuleSettings
    {

        // Home country code (transactions from other countries are flagged)
        [Required]
        [StringLength(10)]
        public string HomeCountry { get; set; } = "ZA";


        // Score contribution when rule is triggered

        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 35m;


        // Whether this rule is enabled
        public bool Enabled { get; set; } = true;
    }


    // ATM Withdrawal Limit Rule Configuration
    public class AtmWithdrawalLimitRuleSettings
    {

        // Single transaction ATM withdrawal limit
        [Range(0, 999999999.99)]
        public decimal Limit { get; set; } = 5000m;


        // Score contribution when rule is triggered
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 30m;


        // Whether this rule is enabled
        public bool Enabled { get; set; } = true;
    }

    // Round Amount Rule Configuration
    public class RoundAmountRuleSettings
    {

        // Minimum transaction amount to check for round numbers
        [Range(0, 999999999.99)]
        public decimal MinimumAmount { get; set; } = 1000m;


        // Amount must be multiple of this value to trigger (e.g., 1000 for thousands)
        [Range(1, 999999999.99)]
        public decimal RoundnessMultiple { get; set; } = 1000m;


        // Score contribution when rule is triggered
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 10m;


        // Whether this rule is enabled
        public bool Enabled { get; set; } = true;
    }


    // High Risk Merchant Category Rule Configuration
    public class HighRiskMerchantCategoryRuleSettings
    {

        // Comma-separated list of high-risk merchant categories
        [Required]
        public string HighRiskCategories { get; set; } = "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer";


        // Score contribution when rule is triggered
        [Range(0, 100)]
        public decimal ScoreContribution { get; set; } = 25m;


        // Whether this rule is enabled
        public bool Enabled { get; set; } = true;


        // Parse the comma-separated categories into a HashSet
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
