// ============================================================================
// CORRECTED CONNECTION POOLING IMPLEMENTATION
// ============================================================================
// This file shows the PROPER way to implement FraudRepository to leverage
// Npgsql's built-in connection pooling and handle concurrent requests safely.
// ============================================================================

using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;
using System.Data;

namespace fraud_poc_project_repo
{
    /// <summary>
    /// CORRECTED IMPLEMENTATION: Properly uses Npgsql connection pooling
    ///
    /// Key Differences from Current Implementation:
    /// 1. No persistent single connection - creates new connection each call
    /// 2. Npgsql automatically pools connections based on connection string settings
    /// 3. Each concurrent request gets its own connection from the pool
    /// 4. Thread-safe and scalable
    /// 5. Transactions are isolated per request
    /// </summary>
    public class FraudRepositoryCorrected : IFraudRepository
    {
        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<FraudRepositoryCorrected> _logger;

        public FraudRepositoryCorrected(
            IConfiguration configuration,
            ILogger<FraudRepositoryCorrected> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is not configured.");
            _schema = configuration["Database:DBSchema"] ?? "public";
            _logger = logger;
        }

        /// <summary>
        /// CORRECTED: Creates a NEW connection each call (Npgsql handles pooling)
        ///
        /// How This Works:
        /// 1. new NpgsqlConnection() gets connection from Npgsql's internal pool
        /// 2. await using ensures connection is properly disposed
        /// 3. When disposed, connection returns to pool (not actually closed)
        /// 4. Npgsql manages pool size, reuses connections for subsequent calls
        /// 5. Each request has isolated transaction scope
        ///
        /// Concurrency Safe:
        /// - Request 1: Gets connection from pool → Transaction A
        /// - Request 2: Gets DIFFERENT connection from pool → Transaction B
        /// - Both work independently, no conflicts
        /// </summary>
        public async Task<long> SaveFraudEvaluationAsync(FraudEventRecord result)
        {
            // CORRECTED: New connection each time (from Npgsql pool)
            await using var dbConnection = new NpgsqlConnection(_connectionString);
            await dbConnection.OpenAsync().ConfigureAwait(false);

            // Transaction is bound to this specific connection
            await using var tx = await dbConnection.BeginTransactionAsync().ConfigureAwait(false);

            try
            {
                long fraudEventId;

                await using (var cmd = new NpgsqlCommand($"CALL \"fr\".sp_insert_fraud_event(" +
                    "@p_kafka_topic, @p_transaction_id, @p_customer_id, @p_account_id, " +
                    "@p_amount, @p_currency, @p_merchant_name, @p_merchant_category, " +
                    "@p_transaction_type, @p_channel, @p_country_code, @p_transaction_time, " +
                    "@p_is_flagged, @p_fraud_score, @p_flagged_reason, @p_fraud_event_id)",
                    dbConnection, tx))
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
                        Direction = ParameterDirection.InputOutput,
                        Value = -1
                    };
                    cmd.Parameters.Add(outParam);

                    await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                    fraudEventId = (long)cmd.Parameters["p_fraud_event_id"].Value;
                }

                foreach (var ruleResult in result.RuleResults)
                {
                    await using var ruleCmd = new NpgsqlCommand(
                        $"CALL \"{_schema}\".sp_insert_fraud_rule_result(" +
                        "@fraud_event_id, @rule_code, @rule_description, @is_triggered, @score_contribution)",
                        dbConnection, tx);
                    ruleCmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);
                    ruleCmd.Parameters.AddWithValue("rule_code", ruleResult.RuleCode);
                    ruleCmd.Parameters.AddWithValue("rule_description", (object?)ruleResult.RuleDescription ?? DBNull.Value);
                    ruleCmd.Parameters.AddWithValue("is_triggered", ruleResult.IsTriggered);
                    ruleCmd.Parameters.AddWithValue("score_contribution", ruleResult.ScoreContribution);
                    await ruleCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await tx.CommitAsync().ConfigureAwait(false);
                return fraudEventId;
            }
            catch
            {
                _logger.LogError("Correlation ID: {correlationID} - Failed to write Fraud data to Database for Model={model}",
                    result?.Event?.CorrelationId ?? Guid.NewGuid(),
                    JsonConvert.SerializeObject(result));
                await tx.RollbackAsync().ConfigureAwait(false);
                throw;
            }
            // CORRECTED: Connection is disposed here and returns to Npgsql's pool
            // It is NOT actually closed - just marked as available for reuse
        }

        /// <summary>
        /// CORRECTED: Simple, clean implementation with no state checks
        /// </summary>
        public async Task SavedltErrorAsync(string topic, string messageData, string error)
        {
            try
            {
                // CORRECTED: New connection each time
                await using var dbConnection = new NpgsqlConnection(_connectionString);
                await dbConnection.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"CALL \"{_schema}\".sp_insert_dlt_error(@topic_data, @topic_schema, @topic_name, @topic_dlt_name, @message_data, @error)",
                    dbConnection);
                cmd.Parameters.AddWithValue("topic_data", topic);
                cmd.Parameters.AddWithValue("topic_schema", _schema);
                cmd.Parameters.AddWithValue("topic_name", topic);
                cmd.Parameters.AddWithValue("topic_dlt_name", topic + ".dlt");
                cmd.Parameters.AddWithValue("message_data", messageData);
                cmd.Parameters.AddWithValue("error", error.Length > 2000 ? error[..2000] : error);
                await cmd.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
            catch
            {
                _logger.LogError("Failed to write Error to DLT for message={msg}", messageData);
                throw;
            }
        }

        /// <summary>
        /// CORRECTED: New connection per call, scales with concurrent requests
        /// </summary>
        public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                // CORRECTED: New connection each time
                await using var dbConnection = new NpgsqlConnection(_connectionString);
                await dbConnection.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    dbConnection);
                cmd.Parameters.AddWithValue("date_from", query.DateFrom.ToDateTimeFrom_yyyyMMddHHmmss());
                cmd.Parameters.AddWithValue("date_to", query.DateTo.ToDateTimeFrom_yyyyMMddHHmmss());
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
            catch
            {
                _logger.LogError("Customer ID: {customerid} - Failed to retrieve data for Query={msg}",
                    query.CustomerId,
                    JsonConvert.SerializeObject(query));
                throw;
            }
        }

        public async Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                await using var dbConnection = new NpgsqlConnection(_connectionString);
                await dbConnection.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    dbConnection);
                cmd.Parameters.AddWithValue("date_from", query.DateFrom.ToDateTimeFrom_yyyyMMddHHmmss());
                cmd.Parameters.AddWithValue("date_to", query.DateTo.ToDateTimeFrom_yyyyMMddHHmmss());
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only", true);
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudEventRecord(reader));
                }
                return results;
            }
            catch
            {
                _logger.LogError("Customer ID: {customerid} - Failed to retrieve only fraud data for Query={msg}",
                    query.CustomerId,
                    JsonConvert.SerializeObject(query));
                throw;
            }
        }

        public async Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId)
        {
            try
            {
                var results = new List<FraudRuleSetRecord>();

                await using var dbConnection = new NpgsqlConnection(_connectionString);
                await dbConnection.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_rule_results(@fraud_event_id)",
                    dbConnection);
                cmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudRuleResultRecord(reader));
                }
                return results;
            }
            catch
            {
                _logger.LogError("Failed to retrieve rule results for fraud event={fraudid}", fraudEventId);
                throw;
            }
        }

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
    }
}

// ============================================================================
// ALSO UPDATE: Connection String in appsettings.json/Configuration
// ============================================================================
/*
"ConnectionStrings": {
  "PostgreSQL": "Server=localhost;Port=5432;Database=fraud_poc;Username=postgres;Password=password;Maximum Pool Size=20;Minimum Pool Size=5;Connection Idle Lifetime=60;Connection Pruning Interval=60"
}

Key Connection String Parameters:
- Maximum Pool Size=20: Keep up to 20 connections pooled
- Minimum Pool Size=5: Always maintain at least 5 ready connections
- Connection Idle Lifetime=60: Recycle connections idle for 60 seconds
- Connection Pruning Interval=60: Check for idle connections every 60 seconds
*/
