using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions.ConsumerOptions;

public record IncomingOfferTrackingCommunicationConsumerOptions
{
    public const string Section = "IncomingOfferTrackingCommunicationConsumerOptions";

    [Required] public required string Topic { get; init; }
    [Required] public required string TopicDlt { get; init; }
}