using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_repo.Connection;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;

namespace fraud_poc_project_repo
{
    // Talks directly to the PostgreSQL database for everything fraud-related: saving
    // evaluation results, recording errors, and answering search queries from the API.
    // Each method opens its own database connection, runs one SQL stored procedure or
    // function, and closes the connection again.
    public class FraudRepository : IFraudRepository
    {
        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<FraudRepository> _logger;
        private readonly IDBConnection _dbConnection;

        public FraudRepository(IConfiguration configuration,
            ILogger<FraudRepository> logger,
            IDBConnection dbConnection)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is not configured.");
            _schema = configuration["Database:DBSchema"] ?? "public";
            _logger = logger;
            _dbConnection = dbConnection;
        }

        // Saves one fraud evaluation result to the database:
        //   1. Insert the transaction + its overall score/flag into fraud_event.
        //   2. Insert one row per rule result into fraud_rule_result, linked to that event.
        // Both steps happen in a single transaction, so if either one fails, nothing is saved.
        public async Task<long> SaveFraudEvaluationAsync(FraudEventRecord result)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                long fraudEventId;

                await using (var cmd = new NpgsqlCommand($"CALL \"fr\".sp_insert_fraud_event(" +
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

                    await cmd.ExecuteNonQueryAsync();
                    fraudEventId = (long)cmd.Parameters["p_fraud_event_id"].Value;
                }

                foreach (var ruleResult in result.RuleResults)
                {
                    await using var ruleCmd = new NpgsqlCommand(
                        $"CALL \"{_schema}\".sp_insert_fraud_rule_result(" +
                        "@fraud_event_id, @rule_code, @rule_description, @is_triggered, @score_contribution)",
                        conn, tx);
                    ruleCmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);
                    ruleCmd.Parameters.AddWithValue("rule_code", ruleResult.RuleCode);
                    ruleCmd.Parameters.AddWithValue("rule_description", (object?)ruleResult.RuleDescription ?? DBNull.Value);
                    ruleCmd.Parameters.AddWithValue("is_triggered", ruleResult.IsTriggered);
                    ruleCmd.Parameters.AddWithValue("score_contribution", ruleResult.ScoreContribution);
                    await ruleCmd.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();
                return fraudEventId;
            }
            catch
            {
                _logger.LogError("Correlation ID: {correlationID} - Failed to write Fraud data to Database for Model={model}", result?.Event?.CorrelationId ?? Guid.NewGuid(), JsonConvert.SerializeObject(result));
                await tx.RollbackAsync();
                throw;
            }
        }

        // A simpler way to record a dead-letter error, used when we only have the raw
        // topic name, message text, and error message (no full DLT_Kafka model).
        public async Task SavedltErrorAsync(string topic, string messageData, string error)
        {
            try
            {
                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();
                await using var cmd = new NpgsqlCommand(
                    $"CALL \"{_schema}\".sp_insert_dlt_error(@topic_data, @topic_schema, @topic_name, @topic_dlt_name, @message_data, @error)",
                    conn);
                cmd.Parameters.AddWithValue("topic_data", topic);
                cmd.Parameters.AddWithValue("topic_schema", _schema);
                cmd.Parameters.AddWithValue("topic_name", topic);
                cmd.Parameters.AddWithValue("topic_dlt_name", topic + ".dlt");
                cmd.Parameters.AddWithValue("message_data", messageData);
                cmd.Parameters.AddWithValue("error", error.Length > 2000 ? error[..2000] : error);
                await cmd.ExecuteNonQueryAsync();
            }
            catch
            {
                _logger.LogError("Failed to write Error to DLT for message={msg}", messageData);
                throw;
            }
        }

        // Looks up transactions matching the given filters (date range, customer, etc).
        public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    conn);
                cmd.Parameters.AddWithValue("date_from", query.DateFrom);
                cmd.Parameters.AddWithValue("date_to", query.DateTo);
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only", (object?)query.IsFlaggedOnly ?? DBNull.Value);
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(MapFraudEventRecord(reader));
                }
                return results;
            }
            catch
            {
                _logger.LogError("Customer ID: {customerid} - Failed to retrieve data for Query={msg}", query.CustomerId, JsonConvert.SerializeObject(query));
                throw;
            }
        }

        // Same as QueryFraudEventsAsync, but always forces is_flagged_only = true,
        // so only the transactions that were flagged as fraud come back.
        public async Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_events(" +
                    "@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score)",
                    conn);
                cmd.Parameters.AddWithValue("date_from", query.DateFrom);
                cmd.Parameters.AddWithValue("date_to", query.DateTo);
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only", true);
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    results.Add(MapFraudEventRecord(reader));
                }
                return results;
            }
            catch
            {
                _logger.LogError("Customer ID: {customerid} - Failed to retrieve only fraud data for Query={msg}", query.CustomerId, JsonConvert.SerializeObject(query));
                throw;
            }
        }

        // Looks up every rule result that was recorded for one specific fraud event.
        public async Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId)
        {
            try
            {
                var results = new List<FraudRuleSetRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync();

                await using var cmd = new NpgsqlCommand(
                    $"SELECT * FROM \"{_schema}\".fn_select_fraud_rule_results(@fraud_event_id)", conn);
                cmd.Parameters.AddWithValue("fraud_event_id", fraudEventId);

                await using var reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
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

        // Turns one database row into a FraudRuleSetRecord object.
        private static FraudRuleSetRecord MapFraudRuleResultRecord(NpgsqlDataReader r) => new()
        {
            FraudEventId = r.GetInt64(r.GetOrdinal("fraud_event_id")),
            RuleCode = r.DBNullString("rule_code"),
            RuleDescription = r.DBNullString("rule_description"),
            IsTriggered = r.GetBoolean(r.GetOrdinal("is_triggered")),
            ScoreContribution = r.GetDecimal(r.GetOrdinal("score_contribution"))
        };

        // Turns one database row into a FraudEventRecord object (transaction + outcome).
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
