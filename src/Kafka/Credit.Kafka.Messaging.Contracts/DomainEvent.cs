using System;

namespace Credit.Kafka.Messaging.Contracts
{
    public abstract record DomainEvent
    {
        /// <summary>Message ID</summary>
        public Guid EventId { get; init; } = Guid.NewGuid();

        /// <summary>Metadata for the event.</summary>
        public abstract DomainEventMetadata Metadata { get; set; }
    }
}