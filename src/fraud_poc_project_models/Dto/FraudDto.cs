using System;
using System.Collections.Generic;

namespace fraud_poc_project_models.Dto
{
    public class FraudEventDto
    {
        public long Id { get; set; }
        public string KafkaTopic { get; set; } = string.Empty;
        public int KafkaPartition { get; set; }
        public long KafkaOffset { get; set; }
        public DateTime ConsumedAt { get; set; }
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
        public bool IsFlagged { get; set; }
        public decimal FraudScore { get; set; }
        public string? FlaggedReason { get; set; }
        public DateTime TimeLogged { get; set; }
        public List<FraudRuleResultDto> RuleResults { get; set; } = new();
    }

    public class FraudRuleResultDto
    {
        public long Id { get; set; }
        public long FraudEventId { get; set; }
        public string RuleCode { get; set; } = string.Empty;
        public string? RuleDescription { get; set; }
        public bool IsTriggered { get; set; }
        public decimal ScoreContribution { get; set; }
        public DateTime EvaluatedAt { get; set; }
    }

    public class FraudQueryDto
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string? CustomerId { get; set; }
        public bool? IsFlaggedOnly { get; set; }
        public string? TransactionType { get; set; }
        public decimal? MinFraudScore { get; set; }
    }
}
