namespace Credit.Kafka.Messaging.Config;

public class BatchConsumerOptions : ConsumerOptions
{
    public int BatchSize { get; set; } = 50;
    public int? MaxPollIntervalMs { get; set; } = 300000;
    public int? SessionTimeoutMs { get; set; } = 45000;
}