using Amazon;
using Amazon.Runtime;
using Amazon.SecurityToken;
using Amazon.SecurityToken.Model;
using AWS.MSK.Auth;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Handlers;

/// <summary>
/// Implement IAM authentication to AWS MSK.
/// Simplified for single broker usage since MSK topic replication handles multi-broker scenarios.
/// 
/// IMPORTANT: This handler properly isolates AWS credentials to prevent interference with other AWS services.
/// When assuming a role for Kafka access, it creates isolated credentials that only affect Kafka operations,
/// preserving the original application credentials for other services like SQS.
/// 
/// Usage Scenarios:
/// 1. Default credentials: Pod uses its default role for both SQS and Kafka
/// 2. Cross-account Kafka: Pod keeps original role for SQS, assumes different role for Kafka
/// </summary>
public class KafkaAuthHandler : IKafkaAuthHandler
{
    private readonly ILogger<KafkaAuthHandler> _logger;
    private readonly AWSMSKAuthTokenGenerator _tokenGenerator = new();

    public KafkaAuthHandler() { }

    public KafkaAuthHandler(ILogger<KafkaAuthHandler> logger)
    {
        _logger = logger;
    }

    public void OauthCallbackConsumer(IClient client, string cfg)
    {
        InternalGetToken(client, "Consumer", null);
    }

    public void OauthCallbackProducer(IClient client, string cfg)
    {
        InternalGetToken(client, "Producer", null);
    }

    public void OauthCallbackConsumerWithRole(IClient client, string cfg, string? roleArn)
    {
        InternalGetToken(client, "Consumer", roleArn);
    }

    public void OauthCallbackProducerWithRole(IClient client, string cfg, string? roleArn)
    {
        InternalGetToken(client, "Producer", roleArn);
    }

    private void InternalGetToken(IClient client, string type, string? roleArn)
    {
        AmazonSecurityTokenServiceClient? stsClient = null;
        AmazonSecurityTokenServiceClient? kafkaStsClient = null;
        AWSMSKAuthTokenGenerator? generator = null;

        try
        {
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("Generating a token for IAM authentication to MSK ({type}) with role: {roleArn}", type, roleArn ?? "default");

            if (!string.IsNullOrEmpty(roleArn))
            {
                // CROSS-ACCOUNT SCENARIO: Assume role for Kafka access without affecting global credentials
                // This preserves the original pod credentials for other AWS services (e.g., SQS)
                stsClient = new AmazonSecurityTokenServiceClient();

                var assumeRoleRequest = new AssumeRoleRequest
                {
                    RoleArn = roleArn,
                    RoleSessionName = $"kafka-{type.ToLower()}-{DateTime.UtcNow:yyyyMMddHHmmss}"
                };

                var assumeRoleResponse = stsClient.AssumeRoleAsync(assumeRoleRequest)
                    .ConfigureAwait(false).GetAwaiter().GetResult();

                // Create isolated AWS credentials for Kafka operations only
                // These credentials are NOT set globally and do NOT affect other AWS service calls
                var kafkaCredentials = new SessionAWSCredentials(
                    assumeRoleResponse.Credentials.AccessKeyId,
                    assumeRoleResponse.Credentials.SecretAccessKey,
                    assumeRoleResponse.Credentials.SessionToken);

                // Create STS client with the isolated Kafka credentials
                kafkaStsClient = new AmazonSecurityTokenServiceClient(kafkaCredentials);

                // Use the isolated STS client for token generation
                generator = new AWSMSKAuthTokenGenerator(kafkaStsClient);
            }
            else
            {
                generator = _tokenGenerator;
            }

            (string token, long expiryMs) = generator.GenerateAuthTokenAsync(RegionEndpoint.AFSouth1, true)
                .ConfigureAwait(false).GetAwaiter().GetResult();

            client.OAuthBearerSetToken(token, expiryMs, "");

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("MSK IAM token expires at {expiryMs}", DateTimeOffset.FromUnixTimeMilliseconds(expiryMs));
        }
        catch (Exception e)
        {
            _logger.LogError(
                e, "Failed to generate a token for IAM authentication");

            client.OAuthBearerSetTokenFailure(e.ToString());
        }
        finally
        {
            stsClient?.Dispose();
            kafkaStsClient?.Dispose();
        }
    }
}