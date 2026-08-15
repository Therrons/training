namespace OptionsModels.KafkaOptions;

// Describes one Kafka topic to create: its name, how many copies of the data to keep
// (ReplicationFactor), and how many partitions to split it into.
public record KafkaTopicOptions
{
    public string Topic { get; init; }
    public short ReplicationFactor { get; init; }
    public int Partitions { get; init; }
}