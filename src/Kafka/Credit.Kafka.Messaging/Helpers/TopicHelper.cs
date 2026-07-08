using AWS.MSK.Auth;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Credit.Kafka.Messaging.Config;

namespace Credit.Kafka.Messaging.Helpers;

public static class TopicHelper
{
    internal static void CreateTopics(List<TopicOptions> topicOptionsList, BrokerOptions brokerOptions)
    {
        topicOptionsList.ForEach(OptionsValidator.ValidateOptions);

        var builder = new AdminClientBuilder(new AdminClientConfig
        {
            BootstrapServers = brokerOptions.BootstrapServers,
            SaslUsername = brokerOptions.SaslUserName,
            SaslPassword = brokerOptions.SaslPassword,
            SaslMechanism = brokerOptions.SaslMechanism,
            SecurityProtocol = brokerOptions.SecurityProtocol,
        });

        if (brokerOptions.SaslMechanism == SaslMechanism.OAuthBearer)
        {
            AWSMSKAuthTokenGenerator mskAuthTokenGenerator = new();

            //Callback to handle OAuth bearer token refresh. It fetches the OAUTH Token from the AWSMSKAuthTokenGenerator class. 
            void OauthCallback(IClient client, string cfg)
            {
                try
                {
                    var (token, expiryMs) = mskAuthTokenGenerator.GenerateAuthTokenAsync(Amazon.RegionEndpoint.AFSouth1)
                        .ConfigureAwait(false).GetAwaiter().GetResult();
                    client.OAuthBearerSetToken(token, expiryMs, "AdminClient");
                }
                catch (Exception e)
                {
                    client.OAuthBearerSetTokenFailure(e.ToString());
                }
            }

            builder.SetOAuthBearerTokenRefreshHandler(OauthCallback);
        }

        using var adminClient = builder.Build();

        try
        {
            var topicsOnBroker = adminClient.GetMetadata(TimeSpan.FromSeconds(30))
             .Topics.Select(a => a.Topic)
             .ToList();

            var topicsToBeCreated = topicOptionsList.Where(x => !topicsOnBroker.Contains(x.Topic!));

            var topicSpecification = topicsToBeCreated.Select(x => new TopicSpecification
            {
                Name = x.Topic,
                ReplicationFactor = x.ReplicationFactor,
                NumPartitions = x.Partitions,
                Configs = x.OptionalConfigs
            }).ToList();

            if (topicSpecification.Count != 0)
                adminClient.CreateTopicsAsync(topicSpecification).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {

            throw;
        }



    }
}