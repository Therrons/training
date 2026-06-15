using docke_web_Api.Transactions.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace docke_web_Api.Transactions.Services
{
    public interface ITransactionAggregator
    {
        IEnumerable<TransactionRecord> GetAllTransactions();
        IEnumerable<TransactionRecord> GetTransactions(TransactionQueryParameters query);
        IEnumerable<TransactionCategorySummary> GetCategorySummaries(TransactionQueryParameters? query = null);
        IEnumerable<TransactionCustomerSummary> GetCustomerSummaries(TransactionQueryParameters? query = null);
        IEnumerable<TransactionSourceSummary> GetSourceSummaries(TransactionQueryParameters? query = null);
        IEnumerable<TransactionDailySummary> GetDailySummaries(TransactionQueryParameters? query = null);
    }

    public class TransactionAggregator : ITransactionAggregator
    {
        private readonly List<TransactionRecord> _allTransactions;

        public TransactionAggregator(IEnumerable<ITransactionDataSource> dataSources)
        {
            _allTransactions = (dataSources ?? Array.Empty<ITransactionDataSource>())
                .SelectMany(source => source.GetTransactions())
                .ToList();
        }

        public IEnumerable<TransactionRecord> GetAllTransactions()
        {
            return _allTransactions.OrderByDescending(t => t.PostedAt);
        }

        public IEnumerable<TransactionRecord> GetTransactions(TransactionQueryParameters query)
        {
            if (query == null)
                throw new ArgumentNullException(nameof(query));

            return GetAllTransactions()
                .Where(t => string.IsNullOrWhiteSpace(query.CustomerId) || t.CustomerId.Equals(query.CustomerId, StringComparison.OrdinalIgnoreCase))
                .Where(t => !query.Category.HasValue || t.Category == query.Category.Value)
                .Where(t => !query.Source.HasValue || t.Source == query.Source.Value)
                .Where(t => !query.FromPostedAt.HasValue || t.PostedAt >= query.FromPostedAt.Value)
                .Where(t => !query.ToPostedAt.HasValue || t.PostedAt <= query.ToPostedAt.Value)
                .Where(t => !query.MinimumAmount.HasValue || t.Amount >= query.MinimumAmount.Value)
                .Where(t => !query.MaximumAmount.HasValue || t.Amount <= query.MaximumAmount.Value);
        }

        public IEnumerable<TransactionCategorySummary> GetCategorySummaries(TransactionQueryParameters? query = null)
        {
            var filtered = query == null ? GetAllTransactions() : GetTransactions(query);
            return filtered
                .GroupBy(t => t.Category)
                .Select(group => new TransactionCategorySummary
                {
                    Category = group.Key,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(t => t.Amount)
                })
                .OrderByDescending(summary => summary.TotalAmount);
        }

        public IEnumerable<TransactionCustomerSummary> GetCustomerSummaries(TransactionQueryParameters? query = null)
        {
            var filtered = query == null ? GetAllTransactions() : GetTransactions(query);
            return filtered
                .GroupBy(t => t.CustomerId)
                .Select(group => new TransactionCustomerSummary
                {
                    CustomerId = group.Key,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(t => t.Amount)
                })
                .OrderByDescending(summary => summary.TotalAmount);
        }

        public IEnumerable<TransactionSourceSummary> GetSourceSummaries(TransactionQueryParameters? query = null)
        {
            var filtered = query == null ? GetAllTransactions() : GetTransactions(query);
            return filtered
                .GroupBy(t => t.Source)
                .Select(group => new TransactionSourceSummary
                {
                    Source = group.Key,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(t => t.Amount)
                })
                .OrderByDescending(summary => summary.TotalAmount);
        }

        public IEnumerable<TransactionDailySummary> GetDailySummaries(TransactionQueryParameters? query = null)
        {
            var filtered = query == null ? GetAllTransactions() : GetTransactions(query);
            return filtered
                .GroupBy(t => t.PostedAt.Date)
                .Select(group => new TransactionDailySummary
                {
                    Day = group.Key,
                    TransactionCount = group.Count(),
                    TotalAmount = group.Sum(t => t.Amount)
                })
                .OrderByDescending(summary => summary.Day);
        }
    }
}
