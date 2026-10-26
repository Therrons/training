using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_repo.Interfaces
{
    public interface IFraudRepository
    {
        Task<long> SaveFraudEvaluationAsync(FraudEventRecord result);

        Task SaveDBDltErrorAsync(string topic, string messageData, string error);

        Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query, bool forceFlaggedOnly);

        // Same as above, but only returns transactions that were flagged as fraud.
        Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query);

        Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId);
    }
}
