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
    public static class KafkaConfiguration
    {
        private static KafkaSettings _kafkaSettings;
        private static AppSettings _appSettings;

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

            return services;
        }

        public static void Kafka_Setup_Topics(this IServiceCollection services)
        {
            // Build a temporary service provider to resolve the options
            var serviceProvider = services.BuildServiceProvider();

            // get options values populated from config
            var brokerSettings = serviceProvider.GetRequiredService<IOptions<FraudKafkaBrokerSettings>>().Value;
            var kafkaAdminSettings = serviceProvider.GetRequiredService<IOptions<KafkaAdminOptions>>().Value;

            if (brokerSettings != null && kafkaAdminSettings != null)
            {
                // validate that the options value met the minimum required values
                Model_Extensions_Helper.ValidateOptions(brokerSettings);
                Model_Extensions_Helper.ValidateOptions(kafkaAdminSettings);


                if (brokerSettings.AllowAutoCreateTopics == true && kafkaAdminSettings?.TopicOptions.Any() == true)
                {
                    AdminClientBuilder adminClientBuilder = new AdminClientBuilder(new AdminClientConfig
                    {
                        BootstrapServers = brokerSettings.BootstrapServers,
                        SaslUsername = brokerSettings.SaslUserName,
                        SaslPassword = brokerSettings.SaslPassword,
                        SaslMechanism = brokerSettings.SaslMechanism,
                        SecurityProtocol = brokerSettings.SecurityProtocol
                    });
                    CreateKafkaTopics(adminClientBuilder, kafkaAdminSettings);
                }
            }
        }

        private static void CreateKafkaTopics(AdminClientBuilder adminClientBuilder, KafkaAdminOptions kafkaAdminSettings)
        {
            using (IAdminClient adminClient = adminClientBuilder.Build())
            {
                // get list of existing topics in kafka
                List<string> topicsOnBroker = adminClient.GetMetadata(TimeSpan.FromSeconds(30.0)).Topics.Select((TopicMetadata a) => a.Topic).ToList();

                // only create topics which does not exist yet
                List<TopicSpecification> list = (from x in kafkaAdminSettings.TopicOptions
                                                 where !topicsOnBroker.Contains(x.Topic)
                                                 select new TopicSpecification
                                                 {
                                                     Name = x.Topic,
                                                     ReplicationFactor = x.ReplicationFactor,
                                                     NumPartitions = x.Partitions,
                                                 }).ToList();
                if (list.Count != 0)
                {
                    adminClient.CreateTopicsAsync(list).GetAwaiter().GetResult();
                }

            }
        }
    }
}
