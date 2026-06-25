namespace OptionsModels.KafkaOptions.ConsumerOptions
{
    public record IncomingVMAXDeclineReasonConsumerOptions : ConsumerOptions
    {
        public string DeclineReasonsTopic { get; set; }
        public string DeclineReasonsTopicDLQ { get; set; }
    }
}





