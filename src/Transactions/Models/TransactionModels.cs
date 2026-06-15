using System;
using System.Collections.Generic;

namespace docke_web_Api.Transactions.Models
{
    public enum TransactionCategory
    {
        Income,
        Groceries,
        Utilities,
        Housing,
        Dining,
        Travel,
        Healthcare,
        Entertainment,
        Uncategorized
    }

    public enum TransactionSourceType
    {
        Bank,
        CreditCard,
        DigitalWallet
    }

    public class TransactionRecord
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

    public class TransactionQueryParameters
    {
        public string? CustomerId { get; set; }
        public TransactionCategory? Category { get; set; }
        public TransactionSourceType? Source { get; set; }
        public DateTime? FromPostedAt { get; set; }
        public DateTime? ToPostedAt { get; set; }
        public decimal? MinimumAmount { get; set; }
        public decimal? MaximumAmount { get; set; }
    }

    public class TransactionCategorySummary
    {
        public TransactionCategory Category { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionCustomerSummary
    {
        public string CustomerId { get; set; } = string.Empty;
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionSourceSummary
    {
        public TransactionSourceType Source { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class TransactionDailySummary
    {
        public DateTime Day { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
