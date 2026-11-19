using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Interfaces
{
    public interface IFraudQueryService
    {
        Task<IEnumerable<FraudEventRecord>> QueryEventsAsync(FraudQueryDto query);

        // Returns null when no fraud event has the given id
        Task<IReadOnlyList<FraudRuleSetRecord>?> GetRuleResultsAsync(long fraudEventId);
    }
}
