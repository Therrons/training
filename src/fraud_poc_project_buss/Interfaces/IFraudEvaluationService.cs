using fraud_poc_project_buss.Dto;

namespace fraud_poc_project_buss.Interfaces
{
    public interface IFraudEvaluationService
    {
        FraudEventRecord Evaluate(TransactionEvent kafkaEvent);

        Task<FraudEventRecord> EvaluateAsync(TransactionEvent kafkaEvent);
    }
}
