namespace OptionsModels.KafkaOptions;

public record KafkaTopicOptions
{

    public string Topic { get; init; }
    public short ReplicationFactor { get; init; }
    public int Partitions { get; init; }
}