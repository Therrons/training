using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;

namespace KafkaSetup
{
    public class KafkaConsumer : ConsumerConfig
    {
        public KafkaConsumer(FraudKafkaBrokerSettings brokerSettings, FraudKafkaConsumerSettings consumerSettings)
        {
            BootstrapServers = brokerSettings.BootstrapServers;
            SaslUsername = brokerSettings.SaslUserName;
            SaslPassword = brokerSettings.SaslPassword;
            SaslMechanism = brokerSettings.SaslMechanism;
            SecurityProtocol = brokerSettings.SecurityProtocol;
            AllowAutoCreateTopics = brokerSettings.AllowAutoCreateTopics;

            AutoOffsetReset = consumerSettings.AutoOffsetReset;
            PartitionAssignmentStrategy = consumerSettings.PartitionAssignmentStrategy;
            MaxPollIntervalMs = consumerSettings.MaxPollIntervalMs;
            SessionTimeoutMs = consumerSettings.SessionTimeoutMs;
            EnableAutoCommit = consumerSettings.EnableAutoCommit;
            FetchMinBytes = consumerSettings.FetchMinBytes;
            FetchMaxBytes = consumerSettings.FetchMaxBytes;
        }
    }
}
