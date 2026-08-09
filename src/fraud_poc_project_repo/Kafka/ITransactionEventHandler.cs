using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_repo.Kafka
{
    /// <summary>
    /// Handler for processing consumed transaction events
    /// </summary>
    public interface ITransactionEventHandler
    {

        Task HandleAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken);

        Task HandleBatchAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> batch, CancellationToken cancellationToken);
    }
}