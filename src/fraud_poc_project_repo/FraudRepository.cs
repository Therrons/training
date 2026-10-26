using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_repo.Exceptions;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Npgsql;

namespace fraud_poc_project_repo
{
    public class FraudRepository : IFraudRepository
    {
        private const int DefaultPageSize = 50;
        private const int MaxPageSize = 500;

        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<FraudRepository> _logger;

        public FraudRepository(IConfiguration configuration,
            ILogger<FraudRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is not configured.");
            // Must match DatabaseConfiguration's default so reads and writes always target the same schema.
            _schema = configuration["Database:DBSchema"] ?? "fr";
            _logger = logger;
        }

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
                    await ruleCmd.ExecuteNonQueryAsync().ConfigureAwait(false);
                }

                await tx.CommitAsync().ConfigureAwait(false);
                return fraudEventId;
            }
            catch (Exception ex)
            {
                // Identifiers only: the payload contains customer, account and amount data and must not be logged.
                string errMsg = $"Correlation ID: {result?.Event?.CorrelationId} - Failed to write fraud event for TransactionId={result?.Event?.TransactionId}, error={ex.Message}";

                _logger.LogError(errMsg);
                await tx.RollbackAsync().ConfigureAwait(false);
                throw new FraudRepositoryException(errMsg);
            }
        }

        public async Task SaveDBDltErrorAsync(string topic, string messageData, string error)
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
                // The message body is deliberately not logged: it is the transaction payload.
                _logger.LogError(ex, "Failed to write error to DLT for topic {Topic}", topic);
                throw;
            }
        }

        // Queries fraud events based on the provided filter criteria. Every filter is optional.
        // Results are paged (default 50 per page, max 500).
        // When forceFlaggedOnly is true, only flagged transactions are returned (ignores query.IsFlaggedOnly).
        public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query, bool forceFlaggedOnly)
        {
            try
            {
                var results = new List<FraudEventRecord>();

                var pageSize = Math.Clamp(query.PageSize ?? DefaultPageSize, 1, MaxPageSize);
                var page = Math.Max(query.Page ?? 1, 1);
                var offset = (int)Math.Min((long)(page - 1) * pageSize, int.MaxValue);

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT id, kafka_topic, transaction_id, customer_id, account_id, amount, currency, " +
                    $"merchant_name, merchant_category, transaction_type, channel, country_code, transaction_time, " +
                    $"is_flagged, fraud_score, flagged_reason FROM \"{_schema}\".fn_select_fraud_events(" +
                    $"@date_from, @date_to, @customer_id, @is_flagged_only, @transaction_type, @min_fraud_score, @page_size, @page_offset)",
                    conn);
                cmd.Parameters.AddWithValue("date_from",
                    (object?)(query.DateFrom?.ToDateTimeFromYyyyMmDdHhMmSs()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("date_to",
                    (object?)(query.DateTo?.ToDateTimeFromYyyyMmDdHhMmSs()) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("customer_id", (object?)query.CustomerId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("is_flagged_only",
                    forceFlaggedOnly ? true : (object?)query.IsFlaggedOnly ?? DBNull.Value);
                cmd.Parameters.AddWithValue("transaction_type", (object?)query.TransactionType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("min_fraud_score", (object?)query.MinFraudScore ?? DBNull.Value);
                cmd.Parameters.AddWithValue("page_size", pageSize);
                cmd.Parameters.AddWithValue("page_offset", offset);

                await using var reader = await cmd.ExecuteReaderAsync().ConfigureAwait(false);
                while (await reader.ReadAsync().ConfigureAwait(false))
                {
                    results.Add(MapFraudEventRecord(reader));
                }
                return results;
            }
            catch (Exception ex)
            {
                // Filter values (customer ID, query JSON) are not logged; they can identify a customer.
                var logMessage = forceFlaggedOnly
                    ? "Failed to retrieve flagged fraud events"
                    : "Failed to retrieve fraud events";
                _logger.LogError(ex, "{LogMessage}", logMessage);
                throw;
            }
        }

        // Convenience method that returns only flagged fraud events (is_flagged_only = true).
        // Equivalent to calling QueryFraudEventsAsync(query, forceFlaggedOnly: true).
        public async Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query)
            => await QueryFraudEventsAsync(query, forceFlaggedOnly: true).ConfigureAwait(false);

        // Looks up every rule result that was recorded for one specific fraud event.
        public async Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId)
        {
            try
            {
                var results = new List<FraudRuleSetRecord>();

                await using var conn = new NpgsqlConnection(_connectionString);
                await conn.OpenAsync().ConfigureAwait(false);

                await using var cmd = new NpgsqlCommand(
                    $"SELECT id, fraud_event_id, rule_code, rule_description, is_triggered, score_contribution " +
                    $"FROM \"{_schema}\".fn_select_fraud_rule_results(@fraud_event_id)", conn);
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
                _logger.LogError(ex, "Failed to retrieve rule results for fraud event={FraudEventId}", fraudEventId);
                throw;
            }
        }

        // Turns one database row into a FraudRuleSetRecord object.
        private static FraudRuleSetRecord MapFraudRuleResultRecord(NpgsqlDataReader r) => new()
        {
            FraudEventId = r.GetInt64(r.GetOrdinal("fraud_event_id")),
            RuleCode = r.DBNullString("rule_code") ?? string.Empty,
            RuleDescription = r.DBNullString("rule_description") ?? string.Empty,
            IsTriggered = r.GetBoolean(r.GetOrdinal("is_triggered")),
            ScoreContribution = r.GetDecimal(r.GetOrdinal("score_contribution"))
        };

        // Turns one database row into a FraudEventRecord object (transaction + outcome).
        private static FraudEventRecord MapFraudEventRecord(NpgsqlDataReader r) => new()
        {
            Event = new TransactionEvent
            {
                Id = r.GetInt64(r.GetOrdinal("id")),
                KafkaTopic = r.DBNullString("kafka_topic") ?? string.Empty,
                TransactionId = r.GetGuid(r.GetOrdinal("transaction_id")),
                CustomerId = r.DBNullString("customer_id") ?? string.Empty,
                AccountId = r.DBNullString("account_id") ?? string.Empty,
                Amount = r.GetDecimal(r.GetOrdinal("amount")),
                Currency = r.DBNullString("currency") ?? string.Empty,
                MerchantName = r.DBNullString("merchant_name"),
                MerchantCategory = r.DBNullString("merchant_category"),
                TransactionType = r.DBNullString("transaction_type") ?? string.Empty,
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
