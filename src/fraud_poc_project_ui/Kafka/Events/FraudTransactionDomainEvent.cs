using Credit.Kafka.Messaging.Contracts;
using System;

namespace fraud_poc_project.Kafka.Events
{
    public record FraudTransactionDomainEvent : DomainEvent
    {
        public const string EventType = "FraudTransaction";
        public const string EventVersion = "1.0";

        public override DomainEventMetadata Metadata { get; set; } = new DomainEventMetadata
        {
            PublishedAt = DateTime.UtcNow,
            Type = EventType,
            Version = EventVersion,
            CorrelationId = Guid.NewGuid()
        };

        public Guid TransactionId { get; init; } = Guid.NewGuid();
        public string CustomerId { get; init; } = string.Empty;
        public string AccountId { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string Currency { get; init; } = "ZAR";
        public string? MerchantName { get; init; }
        public string? MerchantCategory { get; init; }
        public string TransactionType { get; init; } = string.Empty;
        public string? Channel { get; init; }
        public string? CountryCode { get; init; }
        public DateTime TransactionTime { get; init; } = DateTime.UtcNow;
    }
}
