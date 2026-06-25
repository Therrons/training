using fraud_poc_project_models.Dto;
using fraud_poc_project_models.Models;
using fraud_poc_project_models.Models.Fraud;

namespace fraud_poc_project_models.Mapping
{
    public static class FraudMappingExtensions
    {
        public static FraudEventDto ToDto(this FraudEventRecord record)
        {
            return new FraudEventDto
            {
                Id = record.Id,
                KafkaTopic = record.KafkaTopic,
                KafkaPartition = record.KafkaPartition,
                KafkaOffset = record.KafkaOffset,
                ConsumedAt = record.ConsumedAt,
                TransactionId = record.TransactionId,
                CustomerId = record.CustomerId,
                AccountId = record.AccountId,
                Amount = record.Amount,
                Currency = record.Currency,
                MerchantName = record.MerchantName,
                MerchantCategory = record.MerchantCategory,
                TransactionType = record.TransactionType,
                Channel = record.Channel,
                CountryCode = record.CountryCode,
                TransactionTime = record.TransactionTime,
                IsFlagged = record.IsFlagged,
                FraudScore = record.FraudScore,
                FlaggedReason = record.FlaggedReason,
                TimeLogged = record.TimeLogged,
                RuleResults = record.RuleResults.Select(r => r.ToDto()).ToList()
            };
        }

        public static FraudRuleResultDto ToDto(this FraudRuleResultRecord record)
        {
            return new FraudRuleResultDto
            {
                Id = record.Id,
                FraudEventId = record.FraudEventId,
                RuleCode = record.RuleCode,
                RuleDescription = record.RuleDescription,
                IsTriggered = record.IsTriggered,
                ScoreContribution = record.ScoreContribution,
                EvaluatedAt = record.EvaluatedAt
            };
        }

        public static FraudQueryParameters ToModel(this FraudQueryDto dto)
        {
            return new FraudQueryParameters
            {
                DateFrom = dto.DateFrom,
                DateTo = dto.DateTo,
                CustomerId = dto.CustomerId,
                IsFlaggedOnly = dto.IsFlaggedOnly,
                TransactionType = dto.TransactionType,
                MinFraudScore = dto.MinFraudScore
            };
        }

        public static IEnumerable<FraudEventDto> ToDto(this IEnumerable<FraudEventRecord> records)
            => records.Select(r => r.ToDto());

        public static IEnumerable<FraudRuleResultDto> ToDto(this IEnumerable<FraudRuleResultRecord> records)
            => records.Select(r => r.ToDto());
    }
}
