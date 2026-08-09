namespace fraud_poc_project_buss.Dto
{
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
