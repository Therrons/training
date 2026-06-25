namespace OptionsModels.KafkaOptions.ConsumerOptions;

public record IncomingFraudConsumerOptions : ConsumerOptions
{
    public string FraudTopic { get; set; }
    public string FraudTopicDLT { get; set; }
}