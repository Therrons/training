namespace fraud_poc_project_buss.Dto
{
    // The filters someone can use when searching for fraud events through the API,
    // e.g. "show me flagged transactions for customer X between these two dates".
    public record FraudQueryDto
    {
        public DateTime DateFrom { get; set; }
        public DateTime DateTo { get; set; }
        public string? CustomerId { get; set; }
        public bool? IsFlaggedOnly { get; set; }
        public string? TransactionType { get; set; }
        public decimal? MinFraudScore { get; set; }
    }
}
