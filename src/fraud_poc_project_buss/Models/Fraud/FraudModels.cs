namespace fraud_poc_project_buss.Models.Fraud
{
    /// <summary>
    /// these models are for kafka events
    /// </summary>
    public record EventMetadata
    {
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    public record TransactionEvent : EventMetadata
    {
        public long Id { get; set; }
        public string KafkaTopic { get; set; }
        public Guid TransactionId { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public string AccountId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string? MerchantName { get; set; }
        public string? MerchantCategory { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string? Channel { get; set; }
        public string? CountryCode { get; set; }
        public DateTime TransactionTime { get; set; }
    }

    /// <summary>
    /// these models are for database operations
    /// </summary>
    public record FraudRuleSetRecord
    {
        public long FraudEventId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string RuleDescription { get; set; } = string.Empty;
        public bool IsTriggered { get; set; }
        public decimal ScoreContribution { get; set; }
    }

    public record FraudEventRecord
    {
        public TransactionEvent Event { get; set; } = new();
        public List<FraudRuleSetRecord> RuleResults { get; set; } = new();
        public bool IsFlagged { get; set; }
        public decimal FraudScore { get; set; }
        public string? FlaggedReason { get; set; }
    }
}
