using System;
using docke_web_Api.Transactions.Models;

namespace docke_web_Api.Transactions.Dto
{
    public class TransactionCategorySummaryDto
    {
        public TransactionCategory Category { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionCustomerSummaryDto
    {
        public string CustomerId { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionSourceSummaryDto
    {
        public TransactionSourceType Source { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionDailySummaryDto
    {
        public DateTime Day { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}