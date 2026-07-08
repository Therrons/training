using System.ComponentModel.DataAnnotations;

namespace Credit.Kafka.Messaging.Config;

public record AdminOptions
{
    public const string Section = "KafkaAdminOptions";

    [Required]
    public required IReadOnlyList<TopicOptions> TopicOptions { get; init; }
}