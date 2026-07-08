using Credit.Kafka.Messaging.Contracts;

namespace Credit.Kafka.Messaging.Models;

public record ProduceDomainEventMessage<T> where T : DomainEvent
{
    public required T Message { get; init; }
    public required string Key { get; init; }
    public Dictionary<string, string>? AdditionalHeaders { get; init; }
}