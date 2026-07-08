using Confluent.Kafka;

namespace Credit.Kafka.Messaging.Handlers;

public interface IMessageHandler
{
    Task HandleMessageAsync(ConsumeResult<byte[], byte[]> message, CancellationToken cancellationToken);
}