namespace OptionsModels.KafkaOptions.ConsumerOptions
{
    public record IncomingAwarenessUpdateEventsConsumerOptions : ConsumerOptions
    {
        public string OfferUpdatedTopic { get; set; }
        public string OfferUpdatedTopicDLQ { get; set; }
    }

}
