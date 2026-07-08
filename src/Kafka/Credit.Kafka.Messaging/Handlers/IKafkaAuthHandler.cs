using Confluent.Kafka;

namespace Credit.Kafka.Messaging.Handlers;

public interface IKafkaAuthHandler
{
    void OauthCallbackConsumer(IClient client, string cfg);

    void OauthCallbackProducer(IClient client, string cfg);

    void OauthCallbackConsumerWithRole(IClient client, string cfg, string? roleArn);

    void OauthCallbackProducerWithRole(IClient client, string cfg, string? roleArn);
}