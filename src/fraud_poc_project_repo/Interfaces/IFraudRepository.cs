using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Models.Kafka;

namespace fraud_poc_project_repo.Interfaces
{
    public interface IFraudRepository
    {
        Task<long> SaveFraudEvaluationAsync(FraudEventRecord result);
        Task SavedltErrorAsync(string topic, string messageData, string error);
        Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query);
        Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query);
        Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId);
        Task<bool> CaptureErrorAsync(string correlationID, DLT_Kafka model);
    }
}
