using fraud_poc_project_buss;
using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project_repo.Optimizations
{
    /// <summary>
    /// In-memory cache for fraud rules with 1-hour TTL.
    /// </summary>
    public interface ICachedFraudRuleService
    {
        /// <summary>
        /// Gets rules from cache or loads and caches them.
        /// </summary>
        Task<IEnumerable<IFraudRule>> GetActiveRulesAsync();

        /// <summary>
        /// Clears the cache, forcing a reload on next access.
        /// </summary>
        Task InvalidateCacheAsync();
    }

    public class CachedFraudRuleService : ICachedFraudRuleService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CachedFraudRuleService> _logger;

        private const string CacheKey = "fraud_rules_active";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(1);
        private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(15);

        private static readonly SemaphoreSlim CacheLock = new(1, 1);

        public CachedFraudRuleService(
            IMemoryCache cache,
            ILogger<CachedFraudRuleService> logger)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Gets rules from cache, loading on first access.
        /// </summary>
        public async Task<IEnumerable<IFraudRule>> GetActiveRulesAsync()
        {
            // Fast path: cache hit
            if (_cache.TryGetValue(CacheKey, out var cachedRules))
            {
                _logger.LogDebug("Fraud rules retrieved from cache");
                return (IEnumerable<IFraudRule>)cachedRules;
            }

            // Slow path: acquire semaphore and load
            await CacheLock.WaitAsync();
            try
            {
                // Double-check: another thread may have populated cache while we waited
                if (_cache.TryGetValue(CacheKey, out cachedRules))
                {
                    return (IEnumerable<IFraudRule>)cachedRules;
                }

                _logger.LogInformationOnly("Loading fraud rules into cache");
                var rules = GetBuiltInRules().ToList();

                var cacheOptions = new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = CacheDuration,
                    SlidingExpiration = SlidingExpiration
                };

                _cache.Set(CacheKey, rules, cacheOptions);
                _logger.LogInformationOnly("Fraud rules cached. Count: {RuleCount}", rules.Count);
                return rules;
            }
            finally
            {
                CacheLock.Release();
            }
        }

        /// <summary>
        /// Clears the fraud rules from cache.
        /// Call this when rule configuration changes.
        /// </summary>
        public Task InvalidateCacheAsync()
        {
            _cache.Remove(CacheKey);
            _logger.LogInformationOnly("Fraud rules cache invalidated");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Returns the built-in fraud rules.
        /// In production, these could be loaded from database with their own cache.
        /// </summary>
        private IEnumerable<IFraudRule> GetBuiltInRules()
        {
            return new List<IFraudRule>
            {
                new HighAmountRule(50000m),
                new ForeignCnpRule("ZA"),
                new AtmWithdrawalLimitRule(5000m),
                new HighRiskMerchantCategoryRule(),
                new RoundAmountRule(1000m)
            };
        }
    }
}
