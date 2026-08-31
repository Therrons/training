using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;

namespace fraud_poc_project_repo.Kafka.Helpers
{
    /// <summary>
    /// Factory for creating pre-configured Kafka AdminClient instances.
    /// Eliminates duplicate AdminClientConfig setup across the application.
    /// </summary>
    public static class KafkaAdminClientFactory
    {
        /// <summary>
        /// Creates an AdminClientConfig with settings from FraudKafkaBrokerSettings.
        /// </summary>
        public static AdminClientConfig CreateAdminClientConfig(FraudKafkaBrokerSettings brokerSettings)
        {
            return new AdminClientConfig
            {
                BootstrapServers = brokerSettings.BootstrapServers,
                SaslUsername = brokerSettings.SaslUserName,
                SaslPassword = brokerSettings.SaslPassword,
                SaslMechanism = brokerSettings.SaslMechanism,
                SecurityProtocol = brokerSettings.SecurityProtocol,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                AllowAutoCreateTopics = brokerSettings.AllowAutoCreateTopics
            };
        }
    }
}
