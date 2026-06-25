namespace OptionsModels.KafkaOptions.ConsumerOptions;

public record CommsScheduleNotificationOptions : ConsumerOptions
{
    public const string Section = "CommsScheduleNotificationOptions";
    public required string Topic { get; init; }
    public required string TopicDlt { get; init; }
}