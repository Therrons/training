using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions;

// The list of Kafka topics the app is allowed to automatically create on startup
// (only used when AllowAutoCreateTopics is turned on).
public record KafkaAdminOptions
{
    [Required]
    public List<KafkaTopicOptions> TopicOptions { get; set; }
}