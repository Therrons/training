using docke_web_Api.Transactions.Dto;
using docke_web_Api.Transactions.Models;
using System.Collections.Generic;
using System.Linq;

namespace docke_web_Api.Transactions.Mapping
{
    public static class TransactionMappingExtensions
    {
        public static TransactionDto ToDto(this TransactionRecord record)
        {
            return new TransactionDto
            {
                Id = record.Id,
                CustomerId = record.CustomerId,
                Source = record.Source,
                PostedAt = record.PostedAt,
                Amount = record.Amount,
                Currency = record.Currency,
                Merchant = record.Merchant,
                Description = record.Description,
                Category = record.Category,
                RawType = record.RawType
            };
        }

        public static TransactionQueryParameters ToModel(this TransactionQueryDto query)
        {
            return new TransactionQueryParameters
            {
                CustomerId = query.CustomerId,
                Category = query.Category,
                Source = query.Source,
                FromPostedAt = query.FromPostedAt,
                ToPostedAt = query.ToPostedAt,
                MinimumAmount = query.MinimumAmount,
                MaximumAmount = query.MaximumAmount
            };
        }

        public static TransactionCategorySummaryDto ToDto(this TransactionCategorySummary summary)
        {
            return new TransactionCategorySummaryDto
            {
                Category = summary.Category,
                TransactionCount = summary.TransactionCount,
                TotalAmount = summary.TotalAmount
            };
        }

        public static TransactionCustomerSummaryDto ToDto(this TransactionCustomerSummary summary)
        {
            return new TransactionCustomerSummaryDto
            {
                CustomerId = summary.CustomerId,
                TransactionCount = summary.TransactionCount,
                TotalAmount = summary.TotalAmount
            };
        }

        public static TransactionSourceSummaryDto ToDto(this TransactionSourceSummary summary)
        {
            return new TransactionSourceSummaryDto
            {
                Source = summary.Source,
                TransactionCount = summary.TransactionCount,
                TotalAmount = summary.TotalAmount
            };
        }

        public static TransactionDailySummaryDto ToDto(this TransactionDailySummary summary)
        {
            return new TransactionDailySummaryDto
            {
                Day = summary.Day,
                TransactionCount = summary.TransactionCount,
                TotalAmount = summary.TotalAmount
            };
        }

        public static IEnumerable<TransactionDto> ToDto(this IEnumerable<TransactionRecord> records)
        {
            return records.Select(r => r.ToDto());
        }

        public static IEnumerable<TransactionCategorySummaryDto> ToDto(this IEnumerable<TransactionCategorySummary> summaries)
        {
            return summaries.Select(s => s.ToDto());
        }

        public static IEnumerable<TransactionCustomerSummaryDto> ToDto(this IEnumerable<TransactionCustomerSummary> summaries)
        {
            return summaries.Select(s => s.ToDto());
        }

        public static IEnumerable<TransactionSourceSummaryDto> ToDto(this IEnumerable<TransactionSourceSummary> summaries)
        {
            return summaries.Select(s => s.ToDto());
        }

        public static IEnumerable<TransactionDailySummaryDto> ToDto(this IEnumerable<TransactionDailySummary> summaries)
        {
            return summaries.Select(s => s.ToDto());
        }
    }
}