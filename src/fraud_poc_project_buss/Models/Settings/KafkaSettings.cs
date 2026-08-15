using fraud_poc_project_buss.Models.Kafka;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    // Groups together all the Kafka-related settings loaded from the "KafkaSettings"
    // section of appsettings.json: how to connect (BrokerSettings), how to send
    // messages (ProducerSettings), and how to read messages (ConsumerSettings).
    public record KafkaSettings
    {
        [Required(ErrorMessage = "BrokerSettings cannot be empty - check settings in your config")]
        public FraudKafkaBrokerSettings BrokerSettings { get; set; }

        [Required(ErrorMessage = "ProducerSettings cannot be empty - check settings in your config")]
        public FraudKafkaProducerSettings ProducerSettings { get; set; }

        [Required(ErrorMessage = "ConsumerSettings cannot be empty - check settings in your config")]
        public FraudKafkaConsumerSettings ConsumerSettings { get; set; }
    }
}
