using Confluent.Kafka;

namespace Credit.Kafka.Messaging.Config;

public class ConsumerOptions
{
    public List<string> Topics { get; set; } = [];
    public Dictionary<string, Type> MessageHandlers { get; } = [];

    public string? GroupId { get; set; }
    public AutoOffsetReset AutoOffsetReset { get; set; } = AutoOffsetReset.Earliest;
    public PartitionAssignmentStrategy PartitionAssignmentStrategy { get; set; } = PartitionAssignmentStrategy.RoundRobin;
    public bool AllowAutoCreateTopics { get; set; }
    public bool ConsumeDomainEvents { get; set; } = true;
    public int Concurrency { get; set; } = 1;
}