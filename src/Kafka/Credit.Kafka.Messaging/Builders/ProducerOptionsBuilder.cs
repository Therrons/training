using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Exceptions;

namespace Credit.Kafka.Messaging.Builders;

public class ProducerOptionsBuilder
{
    private ProducerOptions _producerOptions = new() { ProducingApplicationName = "" };

    /// <summary>
    /// Gives a default producer configuration with Snappy compression and no auto topic creation
    /// </summary>
    /// <returns></returns>
    public ProducerOptionsBuilder DefaultProducer()
    {
        _producerOptions = new ProducerOptions
        {
            ProducingApplicationName = "",
            CompressionType = CompressionType.Snappy,
            AllowAutoCreateTopics = false
        };
        return this;
    }

    /// <summary>
    /// Sets the compression type for the producer
    /// </summary>
    /// <param name="compressionType"></param>
    /// <returns></returns>
    public ProducerOptionsBuilder WithCompressionType(CompressionType compressionType)
    {
        _producerOptions.CompressionType = compressionType;
        return this;
    }

    /// <summary>
    /// Set the name of the application producing the messages, this is used for tracking the source of messages.
    /// Will be added as a message header
    /// </summary>
    /// <param name="applicationName"></param>
    /// <returns></returns>
    public ProducerOptionsBuilder WithApplicationName(string applicationName)
    {
        _producerOptions.ProducingApplicationName = applicationName;
        return this;
    }

    /// <summary>
    /// Local message timeout. This value is only enforced locally and limits the time a produced message waits for successful delivery.
    /// A time of 0 is infinite. This is the maximum time librdkafka may use to deliver a message (including retries).
    /// </summary>
    /// <param name="messageTimeoutMs"></param>
    /// <returns></returns>
    public ProducerOptionsBuilder WithMessageTimeoutMs(int messageTimeoutMs)
    {
        _producerOptions.MessageTimeoutMs = messageTimeoutMs;
        return this;
    }

    /// <summary>
    /// Delay in milliseconds to wait for messages in the producer queue to accumulate before constructing message batches (MessageSets) to transmit to brokers.
    /// A higher value allows larger and more effective (less overhead, improved compression) batches of messages to accumulate at the expense of increased message delivery latency.
    /// default: 5 importance: high
    /// </summary>
    /// <param name="lingerMs"></param>
    /// <returns></returns>
    public ProducerOptionsBuilder WithLingerMs(double lingerMs)
    {
        _producerOptions.LingerMs = lingerMs;
        return this;
    }

    private void Validate()
    {
        if (_producerOptions.ProducingApplicationName == null)
        {
            throw new ConfigurationException("Application name is required for the producer");
        }
    }

    /// <summary>
    /// Builds the producer options
    /// </summary>
    /// <returns></returns>
    internal ProducerOptions Build()
    {
        Validate();
        return _producerOptions;
    }
}