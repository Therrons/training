using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;

namespace KafkaSetup
{
    public class KafkaProducer : ProducerConfig
    {
        public KafkaProducer(FraudKafkaBrokerSettings brokerSettings, FraudKafkaProducerSettings producerSettings)
        {
            BootstrapServers = brokerSettings.BootstrapServers;
            SaslUsername = brokerSettings.SaslUserName;
            SaslPassword = brokerSettings.SaslPassword;
            SaslMechanism = brokerSettings.SaslMechanism;
            SecurityProtocol = brokerSettings.SecurityProtocol;
            AllowAutoCreateTopics = brokerSettings.AllowAutoCreateTopics;

            CompressionType = producerSettings.CompressionType;
            MessageTimeoutMs = producerSettings.MessageTimeoutMs;
            LingerMs = producerSettings.LingerMs;
            EnableIdempotence = producerSettings.EnableIdempotence;
        }
    }
}
