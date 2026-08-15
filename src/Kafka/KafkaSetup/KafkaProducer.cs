using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;

namespace KafkaSetup
{
    // Translates our own broker + producer settings into the ProducerConfig class that
    // the Confluent Kafka library actually understands.
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
