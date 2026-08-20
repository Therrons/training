using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Models.Kafka;

namespace fraud_poc_project_repo.Interfaces
{
    // Everything the rest of the app can do with the fraud data in the database:
    // save results, look them up again, and record errors.
    public interface IFraudRepository
    {
        // Save the result of evaluating one transaction (and each rule's result) to the database.
        Task<long> SaveFraudEvaluationAsync(FraudEventRecord result);

        // Record that a Kafka message could not be processed (a "dead letter").
        Task SavedltErrorAsync(string topic, string messageData, string error);

        // Look up fraud-evaluated transactions matching the given filters.
        Task<IEnumerable<FraudEventRecord>> QueryFraudEventsAsync(FraudQueryDto query);

        // Same as above, but only returns transactions that were flagged as fraud.
        Task<IEnumerable<FraudEventRecord>> QueryFlaggedOnlyFraudEventsAsync(FraudQueryDto query);

        // Look up which rules fired (and why) for one specific fraud event.
        Task<IEnumerable<FraudRuleSetRecord>> GetRuleResultsForEventAsync(long fraudEventId);
    }
}
