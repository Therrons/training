using Confluent.Kafka;

namespace OptionsModels.KafkaOptions;

public record KafkaClientOptions
{
    public string? BootstrapServers { get; set; }
    public string? Topic { get; set; }
    public string? SaslUserName { get; set; }
    public string? SaslPassword { get; set; }
    public SaslMechanism SaslMechanism { get; set; }
    public SecurityProtocol SecurityProtocol { get; set; }
    public bool AllowAutoCreateTopics { get; set; }
    public string? Debug { get; set; }
    public string? ApplicationName { get; set; }
}