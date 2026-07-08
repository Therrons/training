using System.ComponentModel.DataAnnotations;

namespace Credit.Kafka.Messaging.Config;

public record TopicOptions
{
    internal TopicOptions() { }

    [Required]
    public string? Topic { get; set; }
    [Required]
    public short ReplicationFactor { get; set; }
    [Required]
    public int Partitions { get; set; }
    public Dictionary<string, string>? OptionalConfigs { get; set; }
}