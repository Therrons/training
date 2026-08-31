using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions;

// The list of Kafka topics the app is allowed to automatically create on startup
public record KafkaAdminOptions
{
    [Required]
    public List<KafkaTopicOptions> TopicOptions { get; set; }
}