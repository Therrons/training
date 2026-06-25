using Confluent.Kafka;

namespace OptionsModels.KafkaOptions.ConsumerOptions;

public record ConsumerOptions : KafkaClientOptions
{
    public string? GroupId { get; init; }
    public AutoOffsetReset AutoOffsetReset { get; set; }
    public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; }
    public int RetryIntervalMs { get; set; }
    public int MaxPollIntervalMs { get; set; }
    public int SessionTimeoutMs { get; set; }
    public int ConsumerConcurrency { get; set; } = 1; // default 
    public int BatchSize { get; set; } = 15; // default

}