using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_repo.Kafka
{
    /// <summary>
    /// Sends transaction events to Kafka. Messages go to whichever topic is set on
    /// the message itself (message.KafkaTopic).
    /// </summary>
    public interface IFraudProducer : IDisposable
    {
        /// <summary>
        /// Sends a message and returns immediately (fire-and-forget), without waiting
        /// to confirm Kafka received it.
        /// </summary>
        bool Produce<T>(T message) where T : TransactionEvent;

        /// <summary>
        /// Sends a message and waits until Kafka confirms it was received.
        /// </summary>
        Task<bool> ProduceAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        /// <summary>
        /// Sends a message to the dead-letter topic without waiting for confirmation.
        /// </summary>
        bool ProduceDlt<T>(T message) where T : TransactionEvent;

        /// <summary>
        /// Sends a message to the dead-letter topic and waits for confirmation.
        /// </summary>
        Task<bool> ProduceDltAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        /// <summary>
        /// Waits for any messages still being sent in the background to finish sending.
        /// </summary>
        void Flush(TimeSpan? timeout = null);
    }
}