namespace fraud_poc_project_buss.Models.Fraud
{
    /// <summary>
    /// Extra tracking info attached to every Kafka event, so we can tell when a message
    /// was published and match related messages together using the CorrelationId.
    /// </summary>
    public record EventMetadata
    {
        public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
        public string Version { get; set; } = "1.0";
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
    }

    /// <summary>
    /// A single card/account transaction, read from Kafka, before or after fraud checks
    /// have been run against it.
    /// </summary>
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
    /// The outcome of running one fraud rule (see FraudRules.cs) against one transaction:
    /// did it trigger, and how many points does it add to the fraud score?
    /// </summary>
    public record FraudRuleSetRecord
    {
        public long FraudEventId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string RuleDescription { get; set; } = string.Empty;
        public bool IsTriggered { get; set; }
        public decimal ScoreContribution { get; set; }
    }

    /// <summary>
    /// The full result of evaluating one transaction: the original transaction, every
    /// rule's result, and the final flagged/score decision. This is what gets saved to
    /// the database and returned by the API.
    /// </summary>
    public record FraudEventRecord
    {
        public TransactionEvent Event { get; set; } = new();
        public List<FraudRuleSetRecord> RuleResults { get; set; } = new();
        public bool IsFlagged { get; set; }
        public decimal FraudScore { get; set; }
        public string? FlaggedReason { get; set; }
    }
}
