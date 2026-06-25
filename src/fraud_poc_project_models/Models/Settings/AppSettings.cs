
using Azure.Core;
using Confluent.Kafka;
using OptionsModels.KafkaOptions;
using OptionsModels.KafkaOptions.ConsumerOptions;

namespace fraud_poc_project_models.Models.Settings
{
    public class AppSettings
    {
        public string ApplicationName { get; set; }
        public CompressionType CompressionType { get; init; }
        public IncomingFraudConsumerOptions IncomingFraudConsumerOptions { get; set; }
        public KafkaAdminOptions KafkaAdminOptions { get; set; }
    }
}

