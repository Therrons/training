using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_repo.Kafka
{
    /// <summary>
    /// Sends transaction events to Kafka. Messages go to whichever topic is set on
    /// the message itself (message.KafkaTopic).
    /// </summary>
    public interface IFraudProducer : IDisposable
    {
        bool Produce<T>(T message) where T : TransactionEvent;

        Task<bool> ProduceAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        bool ProduceDlt<T>(T message) where T : TransactionEvent;

        Task<bool> ProduceDltAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        void Flush(TimeSpan? timeout = null);
    }
}