using fraud_poc_project.Services;

namespace fraud_poc_project.Interfaces
{
    public interface IMetricsService
    {
        void RecordTransactionProcessed(bool isFlagged, decimal fraudScore, long elapsedMilliseconds);
        void RecordRuleExecution(string ruleCode, bool triggered, long elapsedMilliseconds);
        void RecordAuthenticationAttempt(bool successful);
        void RecordDatabaseOperation(string operationType, long elapsedMilliseconds);
        void RecordKafkaEvent(string eventType, long elapsedMilliseconds);

        MetricsSnapshot GetSnapshot();
        void Reset();
    }
}
