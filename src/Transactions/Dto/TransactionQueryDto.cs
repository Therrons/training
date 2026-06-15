using System;
using docke_web_Api.Transactions.Models;

namespace docke_web_Api.Transactions.Dto
{
    public class TransactionQueryDto
    {
        public string? CustomerId { get; set; }
        public TransactionCategory? Category { get; set; }
        public TransactionSourceType? Source { get; set; }
        public DateTime? FromPostedAt { get; set; }
        public DateTime? ToPostedAt { get; set; }
        public decimal? MinimumAmount { get; set; }
        public decimal? MaximumAmount { get; set; }
    }
}