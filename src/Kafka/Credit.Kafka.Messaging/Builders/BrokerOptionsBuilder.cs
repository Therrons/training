using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Helpers;

namespace Credit.Kafka.Messaging.Builders;
public class BrokerOptionsBuilder
{
    protected BrokerOptions BrokerOptions = new() { BootstrapServers = "" };

    /// <summary>
    /// Sets the broker address
    /// </summary>
    /// <param name="bootstrapServers"></param>
    /// <returns></returns>
    public BrokerOptionsBuilder WithBootstrapServers(string bootstrapServers)
    {
        BrokerOptions.BootstrapServers = bootstrapServers;
        return this;
    }

    /// <summary>
    /// Sets the security protocol used to communicate with the broker
    /// </summary>
    /// <param name="securityProtocol"></param>
    /// <returns></returns>
    public BrokerOptionsBuilder WithSecurityProtocol(SecurityProtocol securityProtocol)
    {
        BrokerOptions.SecurityProtocol = securityProtocol;
        return this;
    }

    /// <summary>
    /// Authentication mechanism with the broker, if not set, it will default to SaslMechanism.Plain
    /// </summary>
    /// <param name="saslMechanism"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <returns></returns>
    public BrokerOptionsBuilder WithSaslMechanism(SaslMechanism saslMechanism, string? username = null, string? password = null)
    {
        BrokerOptions.SaslMechanism = saslMechanism;
        switch (saslMechanism)
        {
            case SaslMechanism.ScramSha256:
            case SaslMechanism.ScramSha512:
                BrokerOptions.SaslUserName = username
                                             ?? throw new ArgumentNullException(nameof(username));
                BrokerOptions.SaslPassword = password
                                             ?? throw new ArgumentNullException(nameof(password));
                break;
            case SaslMechanism.Plain:
                BrokerOptions.SaslUserName = username;
                BrokerOptions.SaslPassword = password;
                break;
            case SaslMechanism.Gssapi:
            case SaslMechanism.OAuthBearer:
                BrokerOptions.SaslUserName = null;
                BrokerOptions.SaslPassword = null;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(saslMechanism), saslMechanism, null);
        }

        return this;
    }

    /// <summary>
    /// Sets the IAM role ARN to assume for authentication (used with OAuthBearer mechanism)
    /// </summary>
    /// <param name="roleArn"></param>
    /// <returns></returns>
    public BrokerOptionsBuilder WithIamRole(string roleArn)
    {
        BrokerOptions.IamRoleArn = roleArn;
        return this;
    }

    /// <summary>
    /// Creates a topic with the specified partitions and replication factor if it does not exist, it will not update the topic if it already exists
    /// </summary>
    /// <param name="topic"></param>
    /// <param name="partitions"></param>
    /// <param name="replicationFactor"></param>
    /// <param name="optionalConfigs"></param>
    /// <returns></returns>
    public BrokerOptionsBuilder CreateTopic(string topic, int partitions, short replicationFactor, Dictionary<string, string>? optionalConfigs = null)
    {
        BrokerOptions.TopicOptions.Add(new TopicOptions
        {
            Topic = topic,
            Partitions = partitions,
            ReplicationFactor = replicationFactor,
            OptionalConfigs = optionalConfigs
        });
        return this;
    }

    private void Validate()
    {
        OptionsValidator.ValidateOptions(BrokerOptions);
    }

    /// <summary>
    /// Builds and validates BrokerOptions
    /// </summary>
    /// <returns></returns>
    internal BrokerOptions Build()
    {
        Validate();
        return BrokerOptions;
    }
}