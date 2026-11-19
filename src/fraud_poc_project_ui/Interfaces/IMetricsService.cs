using fraud_poc_project.Services;
using fraud_poc_project_buss.Interfaces;

namespace fraud_poc_project.Interfaces
{
    public interface IMetricsService : IAuthenticationMetrics
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
