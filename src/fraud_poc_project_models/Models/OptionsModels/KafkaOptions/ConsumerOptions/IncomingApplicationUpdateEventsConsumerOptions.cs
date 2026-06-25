namespace OptionsModels.KafkaOptions.ConsumerOptions
{

    public record IncomingApplicationUpdateEventsConsumerOptions : ConsumerOptions
    {
        public string ApplicationUpdatedTopic { get; set; }
        public string ApplicationUpdatedTopicDLQ { get; set; }
    }
}
