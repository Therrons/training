using System;
using docke_web_Api.Transactions.Models;

namespace docke_web_Api.Transactions.Dto
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        public string CustomerId { get; set; } = string.Empty;
        public TransactionSourceType Source { get; set; }
        public DateTime PostedAt { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "ZAR";
        public string Merchant { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TransactionCategory Category { get; set; }
        public string RawType { get; set; } = string.Empty;
    }
}