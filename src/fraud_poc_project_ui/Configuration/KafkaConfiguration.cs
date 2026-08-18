using Confluent.Kafka;
using Confluent.Kafka.Admin;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OptionsModels.KafkaOptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fraud_poc_project.Configuration
{



    // Loads all the Kafka-related settings from config, and can create any Kafka
    // topics that don't exist yet (only when auto-create is turned on).
    // Setup Consumer events
    public static class KafkaConfiguration
    {
        public static IServiceCollection AddKafkaConfigurations(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            var brokerSettings = services.AddOptions<FraudKafkaBrokerSettings>()
                .BindConfiguration("KafkaSettings:BrokerSettings")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<FraudKafkaProducerSettings>()
                .BindConfiguration("KafkaSettings:ProducerSettings")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<FraudKafkaConsumerSettings>()
                .BindConfiguration("KafkaSettings:ConsumerSettings")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddOptions<KafkaAdminOptions>()
                .BindConfiguration("KafkaAdminOptions")
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<KafkaBrokerHealthCheck>();

            return services;
        }

        // If auto-create-topics is turned on, this checks which Kafka topics we need
        // and creates any that don't already exist on the broker.
        public static void KafkaSetupTopics(this IServiceCollection services)
        {
            // Build a temporary service provider just so we can read the settings we
            // registered above - a normal DI container isn't available this early in startup.
            var serviceProvider = services.BuildServiceProvider();

            var brokerSettings = serviceProvider.GetRequiredService<IOptions<FraudKafkaBrokerSettings>>().Value;
            var kafkaAdminSettings = serviceProvider.GetRequiredService<IOptions<KafkaAdminOptions>>().Value;

            if (brokerSettings == null || kafkaAdminSettings == null)
                return;

            // Make sure the required settings were actually filled in before we try to use them.
            Model_Extensions_Helper.ValidateOptions(brokerSettings);
            Model_Extensions_Helper.ValidateOptions(kafkaAdminSettings);

            var noTopicsConfigured = kafkaAdminSettings.TopicOptions == null || !kafkaAdminSettings.TopicOptions.Any();
            if (!brokerSettings.AllowAutoCreateTopics || noTopicsConfigured)
                return;

            var adminClientBuilder = new AdminClientBuilder(new AdminClientConfig
            {
                BootstrapServers = brokerSettings.BootstrapServers,
                SaslUsername = brokerSettings.SaslUserName,
                SaslPassword = brokerSettings.SaslPassword,
                SaslMechanism = brokerSettings.SaslMechanism,
                SecurityProtocol = brokerSettings.SecurityProtocol
            });
            CreateKafkaTopics(adminClientBuilder, kafkaAdminSettings);
        }

        // Compares the topics we need (from config) against the topics that already
        // exist on the Kafka broker, and creates only the ones that are missing.
        private static void CreateKafkaTopics(AdminClientBuilder adminClientBuilder, KafkaAdminOptions kafkaAdminSettings)
        {
            using var adminClient = adminClientBuilder.Build();

            var existingTopics = adminClient.GetMetadata(TimeSpan.FromSeconds(30))
                .Topics
                .Select(topic => topic.Topic)
                .ToList();

            var topicsToCreate = new List<TopicSpecification>();
            foreach (var topicOption in kafkaAdminSettings.TopicOptions)
            {
                if (existingTopics.Contains(topicOption.Topic))
                    continue; // already exists - nothing to do

                topicsToCreate.Add(new TopicSpecification
                {
                    Name = topicOption.Topic,
                    ReplicationFactor = topicOption.ReplicationFactor,
                    NumPartitions = topicOption.Partitions
                });
            }

            if (topicsToCreate.Count > 0)
            {
                adminClient.CreateTopicsAsync(topicsToCreate).GetAwaiter().GetResult();
            }
        }
    }
}
