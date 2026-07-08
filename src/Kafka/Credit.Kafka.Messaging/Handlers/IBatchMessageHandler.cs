using Confluent.Kafka;

namespace Credit.Kafka.Messaging.Handlers;

public interface IBatchMessageHandler
{
    /// <summary>
    /// Handles a batch of messages from a consumer for a specific topic. The list of messages is in the order they were consumed from the consumer
    /// </summary>
    /// <param name="messages">List of Kafka Messages in the order they were consumed from the consumer</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task HandleBatchMessagesAsync(List<ConsumeResult<byte[], byte[]>> messages, CancellationToken cancellationToken);
}