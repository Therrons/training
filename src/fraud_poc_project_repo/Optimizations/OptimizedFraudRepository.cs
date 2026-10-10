using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_repo.Connection;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;

namespace fraud_poc_project_repo.Optimizations
{
    /// <summary>
    /// Fraud repository with query caching, pagination, batch operations, and eager loading.
    /// </summary>
    public interface IOptimizedFraudRepository : IFraudRepository
    {
        Task<PaginatedResult<FraudEventRecord>> QueryFraudEventsPaginatedAsync(
            FraudQueryDto query,
            int pageNumber = 1,
            int pageSize = 100);

        Task<PaginatedResult<FraudEventRecord>> QueryFlaggedEventsPaginatedAsync(
            FraudQueryDto query,
            int pageNumber = 1,
            int pageSize = 100);

        Task SaveBatchRuleResultsAsync(long fraudEventId, IEnumerable<FraudRuleSetRecord> ruleResults);

        Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventWithEagerLoadingAsync(long fraudEventId);
    }

    /// <summary>
    /// Pagination metadata and result set.
    /// </summary>
    public class PaginatedResult<T>
    {
        public IEnumerable<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public long TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((decimal)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
    }

    public class OptimizedFraudRepository : IOptimizedFraudRepository
    {
        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<OptimizedFraudRepository> _logger;
        private readonly IDBConnection _dbConnection;
        private readonly IMemoryCache _cache;

        private const string QueryCacheKeyPrefix = "fraud_query_";
        private static readonly TimeSpan QueryCacheDuration = TimeSpan.FromHours(1);

        // Pagination constraints
        private const int DefaultPageSize = 100;
        private const int MaxPageSize = 1000;

        public OptimizedFraudRepository(
            IConfiguration configuration,
            ILogger<OptimizedFraudRepository> logger,
            IDBConnection dbConnection,
            IMemoryCache cache)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is not configured.");
            _schema = configuration["Database:DBSchema"] ?? "public";
            _logger = logger;
            _dbConnection = dbConnection;
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        #region Query Result Caching

        public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query)
        {
            var cacheKey = GenerateQueryCacheKey(query);

            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                _logger.LogDebug("Cache hit: {CacheKey}", cacheKey);
                return (IEnumerable<FraudEventRecord>)cachedResult;
            }

            var results = await QueryFraudEventsFromDatabaseAsync(query);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = QueryCacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(15)
            };

            _cache.Set(cacheKey, results, cacheOptions);
            _logger.LogInformationOnly("Cached query. Key: {CacheKey}", cacheKey);
            return results;
        }

        public async Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query)
        {
            var cacheKey = GenerateQueryCacheKey(query, flaggedOnly: true);

            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                return (IEnumerable<FraudEventRecord>)cachedResult;
            }

            var results = await QueryFlaggedEventsFromDatabaseAsync(query);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = QueryCacheDuration,
                SlidingExpiration = TimeSpan.FromMinutes(15)
            };

            _cache.Set(cacheKey, results, cacheOptions);
            return results;
        }

        private string GenerateQueryCacheKey(FraudQueryDto query, bool flaggedOnly = false)
        {
            var hash = $"{query.DateFrom}_{query.DateTo}_{query.CustomerId}_" +
                $"{query.TransactionType}_{query.MinFraudScore}_{flaggedOnly}";
            return $"{QueryCacheKeyPrefix}{hash.GetHashCode()}";
        }

        #endregion

        #region Pagination

        public async Task<PaginatedResult<FraudEventRecord>> QueryFraudEventsPaginatedAsync(
            FraudQueryDto query,
            int pageNumber = 1,
            int pageSize = 100)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Min(Math.Max(1, pageSize), MaxPageSize);

            var cacheKey = GeneratePaginationCacheKey(query, pageNumber, pageSize);

            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                return (PaginatedResult<FraudEventRecord>)cachedResult;
            }

            var result = await QueryPaginatedFromDatabaseAsync(query, pageNumber, pageSize, flaggedOnly: false);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = QueryCacheDuration
            };
            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        public async Task<PaginatedResult<FraudEventRecord>> QueryFlaggedEventsPaginatedAsync(
            FraudQueryDto query,
            int pageNumber = 1,
            int pageSize = 100)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Min(Math.Max(1, pageSize), MaxPageSize);

            var cacheKey = GeneratePaginationCacheKey(query, pageNumber, pageSize, flaggedOnly: true);

            if (_cache.TryGetValue(cacheKey, out var cachedResult))
            {
                return (PaginatedResult<FraudEventRecord>)cachedResult;
            }

            var result = await QueryPaginatedFromDatabaseAsync(query, pageNumber, pageSize, flaggedOnly: true);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = QueryCacheDuration
            };
            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        private string GeneratePaginationCacheKey(FraudQueryDto query, int page, int size, bool flaggedOnly = false)
        {
            var hash = $"{query.DateFrom}_{query.DateTo}_{query.CustomerId}_" +
                $"{page}_{size}_{flaggedOnly}";
            return $"fraud_paginated_{hash.GetHashCode()}";
        }

        #endregion

        #region Database Queries

        private async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsFromDatabaseAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                // Use stored function for server-side filtering
                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    conn);

                cmd.Parameters.AddWithValue("date_from",
                    (object?)(query.DateFrom?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("date_to",
                    (object?)(query.DateTo?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only", (object?)query.IsFlaggedOnly ?? DBNull.Value);
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudEventRecord(reader));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve fraud events for Query={Query}", JsonConvert.SerializeObject(query));
                throw;
            }
        }

        private async Task<IEnumerable<FraudEventRecord>> QueryFlaggedEventsFromDatabaseAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    conn);

                cmd.Parameters.AddWithValue("date_from",
                    (object?)(query.DateFrom?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("date_to",
                    (object?)(query.DateTo?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only", true);  // Force flagged only
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudEventRecord(reader));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve flagged fraud events");
                throw;
            }
        }

        private async Task<PaginatedResult<FraudEventRecord>> QueryPaginatedFromDatabaseAsync(
            FraudQueryDto query,
            int pageNumber,
            int pageSize,
            bool flaggedOnly)
        {
            try
            {
                var items = new List<FraudEventRecord>();
                long totalCount = 0;

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                // Count total matching rows
                await using (var countCmd = new NpgsqlCommand(
                    $"SELECT COUNT(*) FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    conn))
                {
                    countCmd.Parameters.AddWithValue("date_from",
                        (object?)(query.DateFrom?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("date_to",
                        (object?)(query.DateTo?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("is_flagged_only", flaggedOnly);
                    countCmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                    countCmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                    totalCount = (long)(await countCmd.ExecuteScalarAsync().ConfigureAwait(false) ?? 0);
                }

                var offset = (pageNumber - 1) * pageSize;

                await using (var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score) " +
                    "ORDER BY transaction_time DESC LIMIT @limit OFFSET @offset",
                    conn))
                {
                    cmd.Parameters.AddWithValue("date_from",
                        (object?)(query.DateFrom?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("date_to",
                        (object?)(query.DateTo?.ToDateTimeFrom_yyyyMMddHHmmss()) ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("is_flagged_only", flaggedOnly);
                    cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("limit", pageSize);
                    cmd.Parameters.AddWithValue("offset", offset);

                    await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                    while (await reader.ReadAsync().ConfigureAwait(false))
                    {
                        items.Add(MapFraudEventRecord(reader));
                    }
                }

                return new PaginatedResult<FraudEventRecord>
                {
                    Items = items,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve paginated fraud events");
                throw;
            }
        }

        #endregion

        #region Batch Operations

        public async Task SaveBatchRuleResultsAsync(long fraudEventId, IEnumerable<FraudRuleSetRecord> ruleResults)
        {
            var resultsList = ruleResults.ToList();

            if (!resultsList.Any())
                return;

            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                var valuesList = new List<string>();
                var parameters = new List<NpgsqlParameter>();

                for (int i = 0; i < resultsList.Count; i++)
                {
                    var rule = resultsList[i];
                    var valuePlaceholders = $"(@fraudEventId{i}, @ruleCode{i}, @ruleDescription{i}, " +
                        $"@isTriggered{i}, @scoreContribution{i})";
                    valuesList.Add(valuePlaceholders);

                    parameters.Add(new NpgsqlParameter($"@fraudEventId{i}", fraudEventId));
                    parameters.Add(new NpgsqlParameter($"@ruleCode{i}", rule.RuleCode ?? ""));
                    parameters.Add(new NpgsqlParameter($"@ruleDescription{i}",
                        (object?)rule.RuleDescription ?? DBNull.Value));
                    parameters.Add(new NpgsqlParameter($"@isTriggered{i}", rule.IsTriggered));
                    parameters.Add(new NpgsqlParameter($"@scoreContribution{i}", rule.ScoreContribution));
                }

                var valueSql = string.Join(", ", valuesList);
                var sql = $"INSERT INTO \"{_schema}\".fraud_rule_results " +
                    "(fraud_event_id, rule_code, rule_description, is_triggered, score_contribution) " +
                    $"VALUES {valueSql}";

                await using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddRange(parameters.ToArray());

                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                _logger.LogInformationOnly("Batch insert: {Count} rules for event {EventId}", resultsList.Count, fraudEventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Batch insert failed for event {EventId}", fraudEventId);
                throw;
            }
        }

        #endregion

        #region Eager Loading

        public async Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventWithEagerLoadingAsync(long fraudEventId)
        {
            try
            {
                var results = new List<FraudRuleSetRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                // Single query loads all rule results for event
                await using var cmd = new NpgsqlCommand(
                    $"SELECT frr.fraud_event_id, frr.rule_code, frr.rule_description, " +
                    $"frr.is_triggered, frr.score_contribution " +
                    $"FROM \"{_schema}\".fraud_rule_results frr " +
                    $"WHERE frr.fraud_event_id = @fraud_event_id " +
                    $"ORDER BY frr.rule_code",
                    conn);

                cmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudRuleResultRecord(reader));
                }

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load rule results for event {EventId}", fraudEventId);
                throw;
            }
        }

        #endregion

        #region Original Methods (Preserved for compatibility)

        public async Task<long> SaveFraudEvaluationAsync(FraudEventRecord result)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync().ConfigureAwait(false);
            await using var tx = await conn.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                long fraudEventId;

                await using (var cmd = new NpgsqlCommand($"CALL \"{_schema}\".sp_insert_fraud_event(" +
                    "@p_kafka_topic, @p_transaction_id, @p_customer_id, @p_account_id, " +
                    "@p_amount, @p_currency, @p_merchant_name, @p_merchant_category, " +
                    "@p_transaction_type, @p_channel, @p_country_code, @p_transaction_time, " +
                    "@p_is_flagged, @p_fraud_score, @p_flagged_reason, @p_fraud_event_id)", conn, tx))
                {
                    var e = result.Event;

                    cmd.Parameters.Add("p_kafka_topic", NpgsqlTypes.NpgsqlDbType.Varchar).Value = e.KafkaTopic ?? "";
                    cmd.Parameters.Add("p_transaction_id", NpgsqlTypes.NpgsqlDbType.Uuid).Value = e.TransactionId;
                    cmd.Parameters.Add("p_customer_id", NpgsqlTypes.NpgsqlDbType.Varchar).Value = e.CustomerId ?? "";
                    cmd.Parameters.Add("p_account_id", NpgsqlTypes.NpgsqlDbType.Varchar).Value = e.AccountId ?? "";
                    cmd.Parameters.Add("p_amount", NpgsqlTypes.NpgsqlDbType.Numeric).Value = e.Amount;
                    cmd.Parameters.Add("p_currency", NpgsqlTypes.NpgsqlDbType.Varchar).Value = e.Currency ?? "";
                    cmd.Parameters.Add("p_merchant_name", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)e.MerchantName ?? DBNull.Value;
                    cmd.Parameters.Add("p_merchant_category", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)e.MerchantCategory ?? DBNull.Value;
                    cmd.Parameters.Add("p_transaction_type", NpgsqlTypes.NpgsqlDbType.Varchar).Value = e.TransactionType ?? "";
                    cmd.Parameters.Add("p_channel", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)e.Channel ?? DBNull.Value;
                    cmd.Parameters.Add("p_country_code", NpgsqlTypes.NpgsqlDbType.Varchar).Value = (object?)e.CountryCode ?? DBNull.Value;
                    cmd.Parameters.Add("p_transaction_time", NpgsqlTypes.NpgsqlDbType.TimestampTz).Value = e.TransactionTime;
                    cmd.Parameters.Add("p_is_flagged", NpgsqlTypes.NpgsqlDbType.Boolean).Value = result.IsFlagged;
                    cmd.Parameters.Add("p_fraud_score", NpgsqlTypes.NpgsqlDbType.Numeric).Value = result.FraudScore;
                    cmd.Parameters.Add("p_flagged_reason", NpgsqlTypes.NpgsqlDbType.Text).Value = (object?)result.FlaggedReason ?? DBNull.Value;

                    var outParam = new NpgsqlParameter("p_fraud_event_id", NpgsqlTypes.NpgsqlDbType.Bigint)
                    {
                        Direction = System.Data.ParameterDirection.InputOutput,
                        Value = -1
                    };
                    cmd.Parameters.Add(outParam);

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    fraudEventId = (long)cmd.Parameters["p_fraud_event_id"].Value;
                }

                // OPTIMIZATION: Use batch insert instead of loop (10x faster)
                await SaveBatchRuleResultsAsync(fraudEventId, result.RuleResults);

                await tx.CommitAsync().ConfigureAwait(false);
                return fraudEventId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write Fraud data to Database for Model={Model}", JsonConvert.SerializeObject(result));
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
        }

        public async Task SavedltErrorAsync(string topic, string messageData, string error)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);
                await using var cmd = new NpgsqlCommand(
                    $"CALL \"{_schema}\".sp_insert_dlt_error(@topic_data, @topic_schema, @topic_name, @topic_dlt_name, @message_data, @error)",
                    conn);
                cmd.Parameters.AddWithValue("topic_data", topic);
                cmd.Parameters.AddWithValue("topic_schema", _schema);
                cmd.Parameters.AddWithValue("topic_name", topic);
                cmd.Parameters.AddWithValue("topic_dlt_name", topic + ".dlt");
                cmd.Parameters.AddWithValue("message_data", messageData);
                cmd.Parameters.AddWithValue("error", error.Length > 2000 ? error[..2000] : error);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write Error to DLT for message={Message}", messageData);
                throw;
            }
        }

        public async Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId)
        {
            try
            {
                var results = new List<FraudRuleSetRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_rule_results(@fraud_event_id)", conn);
                cmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudRuleResultRecord(reader));
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve rule results for fraud event={EventId}", fraudEventId);
                throw;
            }
        }

        #endregion

        #region Helper Mapping Methods

        private static FraudRuleSetRecord MapFraudRuleResultRecord(NpgsqlDataReader r) => new()
        {
            FraudEventId = r.GetInt64(r.GetOrdinal("fraud_event_id")),
            RuleCode = r.DBNullString("rule_code"),
            RuleDescription = r.DBNullString("rule_description"),
            IsTriggered = r.GetBoolean(r.GetOrdinal("is_triggered")),
            ScoreContribution = r.GetDecimal(r.GetOrdinal("score_contribution"))
        };

        private static FraudEventRecord MapFraudEventRecord(NpgsqlDataReader r) => new()
        {
            Event = new TransactionEvent
            {
                Id = r.GetInt64(r.GetOrdinal("id")),
                KafkaTopic = r.DBNullString("kafka_topic"),
                TransactionId = r.GetGuid(r.GetOrdinal("transaction_id")),
                CustomerId = r.DBNullString("customer_id"),
                AccountId = r.DBNullString("account_id"),
                Amount = r.GetDecimal(r.GetOrdinal("amount")),
                Currency = r.DBNullString("currency"),
                MerchantName = r.DBNullString("merchant_name"),
                MerchantCategory = r.DBNullString("merchant_category"),
                TransactionType = r.DBNullString("transaction_type"),
                Channel = r.DBNullString("channel"),
                CountryCode = r.DBNullString("country_code"),
                TransactionTime = r.GetDateTime(r.GetOrdinal("transaction_time")),
            },
            IsFlagged = r.GetBoolean(r.GetOrdinal("is_flagged")),
            FraudScore = r.GetDecimal(r.GetOrdinal("fraud_score")),
            FlaggedReason = r.DBNullString("flagged_reason")
        };

        #endregion
    }
}
