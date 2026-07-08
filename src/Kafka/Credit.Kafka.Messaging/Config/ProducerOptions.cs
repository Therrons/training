using Confluent.Kafka;

namespace Credit.Kafka.Messaging.Config;

public record ProducerOptions
{
    public CompressionType CompressionType { get; set; }
    public bool AllowAutoCreateTopics { get; set; }
    public string? TransactionId { get; set; }
    public int? DefaultTransactionTimeoutInSeconds { get; set; }
    public required string ProducingApplicationName { get; set; }
    public int? MessageTimeoutMs { get; set; }
    public double? LingerMs { get; set; }
}