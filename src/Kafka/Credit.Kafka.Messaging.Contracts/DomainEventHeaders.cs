namespace Credit.Kafka.Messaging.Contracts;

public static class DomainEventHeaders
{
    public const string Id = "id";

    public const string EventType = "event-type";

    public const string CorrelationId = "correlation-id";

    public const string PublishedAt = "published-at";

    public const string PublishedBy = "published-by";

    public const string Version = "version";

    public const string ContentType = "content-type";
}