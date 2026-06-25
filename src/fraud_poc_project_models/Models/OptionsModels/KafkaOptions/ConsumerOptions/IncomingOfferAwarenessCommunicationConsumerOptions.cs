using System.ComponentModel.DataAnnotations;

namespace OptionsModels.KafkaOptions.ConsumerOptions
{
    public record IncomingOfferAwarenessCommunicationConsumerOptions
    {
        public const string Section = "IncomingOfferAwarenessCommunicationConsumerOptions";

        [Required] public required string Topic { get; init; }
        [Required] public required string TopicDlt { get; init; }
    }
}