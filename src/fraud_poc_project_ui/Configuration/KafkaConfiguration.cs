using Confluent.Kafka;
using Credit.Kafka.Messaging.Builders;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Consumers;
using Credit.Kafka.Messaging.DependencyInjection;
using fraud_poc_project.Kafka.Consumer;
using fraud_poc_project_models.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace fraud_poc_project.Configuration
{
    public static class KafkaConfiguration
    {
        private static IConfiguration _configuration;

        public static IServiceCollection ConfigureKafka(this IServiceCollection services, IConfiguration configuration)
        {
            var provider = services.BuildServiceProvider();
            var appSettings = provider.GetRequiredService<AppSettings>();
            var consumerOpts = appSettings.IncomingFraudConsumerOptions;
            _configuration = configuration;

            SetupBroker(services, appSettings, consumerOpts);
            SetupProducer(services, appSettings);
            SetupFraudConsumer(services, appSettings, consumerOpts);

            return services;
        }

        private static void SetupBroker(
            IServiceCollection services,
            AppSettings appSettings,
            OptionsModels.KafkaOptions.ConsumerOptions.IncomingFraudConsumerOptions consumerOpts)
        {
            var brokerBuilder = new BrokerOptionsBuilder()
                .WithBootstrapServers(consumerOpts.BootstrapServers ?? "")
                .WithSaslMechanism(
                    consumerOpts.SaslMechanism,
                    _configuration[consumerOpts.SaslUserName],
                    _configuration[consumerOpts.SaslPassword])
                .WithSecurityProtocol(consumerOpts.SecurityProtocol);

            //var brokerBuilder = new BrokerOptionsBuilder()
            //    .WithBootstrapServers(consumerOpts.BootstrapServers ?? "")
            //    .WithSaslMechanism(
            //        consumerOpts.SaslMechanism,
            //        "kafka-user",
            //        "kafka-user-pass")
            //    .WithSecurityProtocol(consumerOpts.SecurityProtocol);

            foreach (var topic in appSettings.KafkaAdminOptions.TopicOptions)
                brokerBuilder.CreateTopic(topic.Topic, topic.Partitions, topic.ReplicationFactor);

            services.RegisterKafkaBroker(brokerBuilder);
        }

        private static void SetupProducer(IServiceCollection services, AppSettings appSettings)
        {
            var producerBuilder = new ProducerOptionsBuilder()
                .DefaultProducer()
                .WithCompressionType(appSettings.CompressionType)
                .WithApplicationName(appSettings.ApplicationName ?? "fraud_poc");

            services.RegisterKafkaProducer(producerBuilder);
        }

        private static void SetupFraudConsumer(
            IServiceCollection services,
            AppSettings appSettings,
            OptionsModels.KafkaOptions.ConsumerOptions.IncomingFraudConsumerOptions consumerOpts)
        {
            var batchConsumerBuilder = new BatchConsumerOptionsBuilder()
                .WithGroupId(consumerOpts.GroupId ?? "fraud-detection-group")
                .WithAutoOffsetReset(consumerOpts.AutoOffsetReset)
                .WithPartitionAssignmentStrategy(consumerOpts.PartitionAssignmentStrategy)
                .WithMaxPollIntervalMs(consumerOpts.MaxPollIntervalMs)
                .WithSessionTimeoutMs(consumerOpts.SessionTimeoutMs)
                .WithConcurrency(consumerOpts.ConsumerConcurrency)
                .WithBatchSize(consumerOpts.BatchSize)
                .WithTopicHandler(consumerOpts.FraudTopic, typeof(FraudBatchConsumerWorker));

            services.RegisterKafkaBatchMultiConsumers<KafkaBatchMultiConsumers<BatchConsumerOptions>,
                BatchConsumerOptionsBuilder>(batchConsumerBuilder);
        }
    }
}
