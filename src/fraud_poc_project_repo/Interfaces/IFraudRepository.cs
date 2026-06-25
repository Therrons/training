using fraud_poc_project_models.Models.Fraud;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fraud_poc_project_repo.Interfaces
{
    public interface IFraudRepository
    {
        Task<long> SaveFraudEvaluationAsync(FraudEvaluationResult result);
        Task SavedltErrorAsync(string topic, string messageData, string error);
        Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryParameters query);
        Task<IEnumerable<FraudRuleResultRecord>> GetRuleResultsForEventAsync(long fraudEventId);
    }
}
