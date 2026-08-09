using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions;

public record KafkaAdminOptions
{
    [Required]
    public List<KafkaTopicOptions> TopicOptions { get; set; }
}