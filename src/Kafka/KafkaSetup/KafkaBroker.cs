using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;

namespace KafkaSetup
{
    // Translates our own FraudKafkaBrokerSettings into the ClientConfig class that the
    // Confluent Kafka library actually understands.
    public class KafkaBroker : ClientConfig
    {
        public KafkaBroker(FraudKafkaBrokerSettings settings)
        {
            BootstrapServers = settings.BootstrapServers;
            SaslUsername = settings.SaslUserName;
            SaslPassword = settings.SaslPassword;
            SaslMechanism = settings.SaslMechanism;
            SecurityProtocol = settings.SecurityProtocol;
            AllowAutoCreateTopics = settings.AllowAutoCreateTopics;
        }
    }
}
