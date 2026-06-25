using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions.ProducerOptions;

public record AuditProducerOptions
{
    public const string Section = "AuditProducerOptions";

    [Required] public required string Topic { get; init; }
}