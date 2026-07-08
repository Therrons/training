using Confluent.Kafka;
using System.ComponentModel.DataAnnotations;

namespace Credit.Kafka.Messaging.Config;

public record BrokerOptions
{
    [Required]
    public required string BootstrapServers { get; set; }
    public string? SaslUserName { get; set; }
    public string? SaslPassword { get; set; }
    public SaslMechanism? SaslMechanism { get; set; }
    public SecurityProtocol? SecurityProtocol { get; set; }
    public List<TopicOptions> TopicOptions { get; set; } = [];

    /// <summary>
    /// IAM role to assume for authentication (used with OAuthBearer mechanism)
    /// </summary>
    public string? IamRoleArn { get; set; }
}