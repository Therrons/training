using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_repo.Kafka
{
    /// <summary>
    /// What to do with transaction events once they've been read from Kafka - run them
    /// through fraud evaluation and save the results.
    /// </summary>
    public interface ITransactionEventHandler
    {
        // Handle one transaction event on its own.
        Task HandleAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken);

        // Handle a whole batch of transaction events at once (used by the batching consumer).
        Task HandleBatchAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> batch, CancellationToken cancellationToken);
    }
}