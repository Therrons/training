using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions.ConsumerOptions;

public record IncomingOfferOnboardingCommunicationConsumerOptions
{
    public const string Section = "IncomingOfferOnboardingCommunicationConsumerOptions";

    [Required] public required string Topic { get; init; }
    [Required] public required string TopicDlt { get; init; }
}