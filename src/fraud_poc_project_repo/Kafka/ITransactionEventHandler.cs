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
        Task HandleTransactionAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken);

        // Handle a whole batch of transaction events at once (used by the batching consumer), order of items is important.
        // This method may be slower than HandleBatchTransactionNonSequentialAsync because it may need to process items in order,
        // but it may be necessary for certain use cases where order matters.
        Task HandleBatchTransactionSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages, CancellationToken cancellationToken);

        // Handle a whole batch of transaction events at once (used by the batching consumer), order of items is not important.
        // Depending on the batch this method is generally faster than the HandleBatchTransactionSequentialAsync
        Task HandleBatchTransactionNonSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages, CancellationToken cancellationToken);
    }
}