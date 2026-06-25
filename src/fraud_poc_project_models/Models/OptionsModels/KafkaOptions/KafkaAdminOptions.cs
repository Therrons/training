using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions;

public record KafkaAdminOptions
{
    //public const string Section = "KafkaAdminOptions";

    [Required]
    public List<KafkaTopicOptions> TopicOptions { get; set; }
}