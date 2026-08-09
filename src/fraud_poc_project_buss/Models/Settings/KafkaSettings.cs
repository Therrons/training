using fraud_poc_project_buss.Models.Kafka;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Models.Settings
{
    public record KafkaSettings
    {
        [Required(ErrorMessage = "Broker Settings cannot be emptpy - check settings in your config")]
        public FraudKafkaBrokerSettings BrokerSettings { get; set; }

        [Required(ErrorMessage = "Fraud Kafka Producer Settings cannot be emptpy - check settings in your config")]
        public FraudKafkaProducerSettings ProducerSettings { get; set; }

        [Required(ErrorMessage = "Consumer Settings cannot be emptpy - check settings in your config")]
        public FraudKafkaConsumerSettings ConsumerSettings { get; set; }
    }
}
