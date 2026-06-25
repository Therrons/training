using fraud_poc_project_models.Models;
using fraud_poc_project_models.Models.Fraud;
using fraud_poc_project_repo.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace fraud_poc_project_repo
{
    public class FraudRepository : IFraudRepository
    {
        private readonly string _connectionString;
        private readonly string _schema;
        private readonly ILogger<FraudRepository> _logger;

        public FraudRepository(IConfiguration configuration, ILogger<FraudRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("PostgreSQL")
                ?? throw new InvalidOperationException("ConnectionStrings:PostgreSQL is not configured.");
            _schema = configuration["Database:Schema"] ?? "public";
            _logger = logger;
        }

        public async Task<long> SaveFraudEvaluationAsync(FraudEvaluationResult result)
        {
            await using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                long fraudEventId;

                await using (var cmd = new NpgsqlCommand($"CALL \"{_schema}\".sp_insert_fraud_event(" +
                    "@kafka_topic, @kafka_partition, @kafka_offset, " +
                    "@transaction_id, @customer_id, @account_id, " +
                    "@amount, @currency, @merchant_name, @merchant_category, " +
                    "@transaction_type, @channel, @country_code, @transaction_time, " +
                    "@is_flagged, @fraud_score, @flagged_reason, @p_fraud_event_id)", conn, tx))
                {
                    var e = result.Event;
                    cmd.Parameters.AddWithValue("kafka_topic", e.KafkaTopic);
                    cmd.Parameters.AddWithValue("kafka_partition", e.KafkaPartition);
                    cmd.Parameters.AddWithValue("kafka_offset", e.KafkaOffset);
                    cmd.Parameters.AddWithValue("transaction_id", e.TransactionId);
                    cmd.Parameters.AddWithValue("customer_id", e.CustomerId);
                    cmd.Parameters.AddWithValue("account_id", e.AccountId);
                    cmd.Parameters.AddWithValue("amount", e.Amount);
                    cmd.Parameters.AddWithValue("currency", e.Currency);
                    cmd.Parameters.AddWithValue("merchant_name", (object?)e.MerchantName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("merchant_category", (object?)e.MerchantCategory ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("transaction_type", e.TransactionType);
                    cmd.Parameters.AddWithValue("channel", (object?)e.Channel ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("country_code", (object?)e.CountryCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("transaction_time", e.TransactionTime);
                    cmd.Parameters.AddWithValue("is_flagged", result.IsFlagged);
                    cmd.Parameters.AddWithValue("fraud_score", result.FraudScore);
                    cmd.Parameters.AddWithValue("flagged_reason", (object?)result.FlaggedReason ?? DBNull.Value);

                    var outParam = new NpgsqlParameter("p_fraud_event_id", NpgsqlTypes.NpgsqlDbType.Bigint)
                    {
                        Direction = System.Data.ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outParam);

                    await cmd.ExecuteNonQueryAsync();
                    fraudEventId = (long)outParam.Value!;
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
                await tx.RollbackAsync();
                throw;
            }
        }

        public async Task SavedltErrorAsync(string topic, string messageData, string error)
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

        public async Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryParameters query)
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

        public async Task<IEnumerable<FraudRuleResultRecord>> GetRuleResultsForEventAsync(long fraudEventId)
        {
            var results = new List<FraudRuleResultRecord>();

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

        private static FraudRuleResultRecord MapFraudRuleResultRecord(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            FraudEventId = r.GetInt64(r.GetOrdinal("fraud_event_id")),
            RuleCode = r.GetString(r.GetOrdinal("rule_code")),
            RuleDescription = r.IsDBNull(r.GetOrdinal("rule_description")) ? null : r.GetString(r.GetOrdinal("rule_description")),
            IsTriggered = r.GetBoolean(r.GetOrdinal("is_triggered")),
            ScoreContribution = r.GetDecimal(r.GetOrdinal("score_contribution")),
            EvaluatedAt = r.GetDateTime(r.GetOrdinal("evaluated_at"))
        };

        private static FraudEventRecord MapFraudEventRecord(NpgsqlDataReader r) => new()
        {
            Id = r.GetInt64(r.GetOrdinal("id")),
            KafkaTopic = r.GetString(r.GetOrdinal("kafka_topic")),
            KafkaPartition = r.GetInt32(r.GetOrdinal("kafka_partition")),
            KafkaOffset = r.GetInt64(r.GetOrdinal("kafka_offset")),
            ConsumedAt = r.GetDateTime(r.GetOrdinal("consumed_at")),
            TransactionId = r.GetGuid(r.GetOrdinal("transaction_id")),
            CustomerId = r.GetString(r.GetOrdinal("customer_id")),
            AccountId = r.GetString(r.GetOrdinal("account_id")),
            Amount = r.GetDecimal(r.GetOrdinal("amount")),
            Currency = r.GetString(r.GetOrdinal("currency")),
            MerchantName = r.IsDBNull(r.GetOrdinal("merchant_name")) ? null : r.GetString(r.GetOrdinal("merchant_name")),
            MerchantCategory = r.IsDBNull(r.GetOrdinal("merchant_category")) ? null : r.GetString(r.GetOrdinal("merchant_category")),
            TransactionType = r.GetString(r.GetOrdinal("transaction_type")),
            Channel = r.IsDBNull(r.GetOrdinal("channel")) ? null : r.GetString(r.GetOrdinal("channel")),
            CountryCode = r.IsDBNull(r.GetOrdinal("country_code")) ? null : r.GetString(r.GetOrdinal("country_code")),
            TransactionTime = r.GetDateTime(r.GetOrdinal("transaction_time")),
            IsFlagged = r.GetBoolean(r.GetOrdinal("is_flagged")),
            FraudScore = r.GetDecimal(r.GetOrdinal("fraud_score")),
            FlaggedReason = r.IsDBNull(r.GetOrdinal("flagged_reason")) ? null : r.GetString(r.GetOrdinal("flagged_reason")),
            TimeLogged = r.GetDateTime(r.GetOrdinal("time_logged"))
        };
    }
}
