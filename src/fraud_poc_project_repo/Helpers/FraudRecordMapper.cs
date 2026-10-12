using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using Npgsql;

namespace fraud_poc_project_repo.Helpers
{
    /// <summary>
    /// Shared utility class for mapping database records to domain objects.
    /// Consolidates duplicate mapping logic from repository implementations.
    /// </summary>
    public static class FraudRecordMapper
    {
        /// <summary>
        /// Maps a database record to a FraudRuleSetRecord domain object.
        /// </summary>
        public static FraudRuleSetRecord MapFraudRuleResultRecord(NpgsqlDataReader r) => new()
        {
            FraudEventId = r.GetInt64(r.GetOrdinal("fraud_event_id")),
            RuleCode = r.DBNullString("rule_code"),
            RuleDescription = r.DBNullString("rule_description"),
            IsTriggered = r.GetBoolean(r.GetOrdinal("is_triggered")),
            ScoreContribution = r.GetDecimal(r.GetOrdinal("score_contribution"))
        };

        /// <summary>
        /// Maps a database record to a FraudEventRecord domain object.
        /// </summary>
        public static FraudEventRecord MapFraudEventRecord(NpgsqlDataReader r) => new()
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
