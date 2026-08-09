using fraud_poc_project_buss.Models.Fraud;

namespace fraud_poc_project_repo.Kafka
{
    /// <summary>
    /// Simplified Kafka producer for fraud detection system
    /// </summary>
    public interface IFraudProducer : IDisposable
    {
        /// <summary>
        /// Generic produce method for custom topics
        /// </summary>
        bool Produce<T>(T message) where T : TransactionEvent;

        /// <summary>
        /// Generic produce method for custom topics
        /// </summary>
        Task<bool> ProduceAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        /// <summary>
        /// Generic produce method for custom topics
        /// </summary>
        bool ProduceDlt<T>(T message) where T : TransactionEvent;

        /// <summary>
        /// Generic produce method for custom topics
        /// </summary>
        Task<bool> ProduceDltAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent;

        /// <summary>
        /// Flush any pending messages
        /// </summary>
        void Flush(TimeSpan? timeout = null);
    }
}