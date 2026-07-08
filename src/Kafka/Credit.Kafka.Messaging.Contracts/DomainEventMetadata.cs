using System;
using System.Collections.Generic;

namespace Credit.Kafka.Messaging.Contracts;

public record DomainEventMetadata
{
    /// <summary>The date that this message was published.</summary>
    public required DateTime PublishedAt { get; init; }

    /// <summary>The reference ID representing a chain of events.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>
    /// The list of other CorrelationIds that started/affected the current chain.
    /// </summary>
    public List<Guid>? AssociatedCorrelationIds { get; set; }

    /// <summary>
    /// The list of external to the domain IDs that started/affected the current chain.
    /// </summary>
    public List<string>? ExternalCorrelationIds { get; set; }

    /// <summary>Implement to return the message type.</summary>
    /// <returns></returns>
    public required string Type { get; init; }

    /// <summary>
    /// Implement the version of the event.
    /// </summary>
    public required string Version { get; init; }
}