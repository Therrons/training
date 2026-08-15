using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaSetup.Extensions
{
    // A one-call helper for wiring up all the Kafka-related pieces (broker, producer,
    // consumer config) into the app's dependency injection container.
    public static class KafkaServiceCollectionExtensions
    {
        public static IServiceCollection AddKafka(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "KafkaSettings")
        {
            // Load and validate the Kafka settings from config (e.g. appsettings.json).
            services.AddOptions<KafkaSettings>()
              .BindConfiguration(sectionName)
              .ValidateDataAnnotations()
              .ValidateOnStart();

            services.AddSingleton<KafkaBroker>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>().Value;
                return new KafkaBroker(settings.BrokerSettings);
            });

            services.AddSingleton<KafkaProducer>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>().Value;
                return new KafkaProducer(settings.BrokerSettings, settings.ProducerSettings);
            });

            services.AddSingleton<KafkaConsumer>(sp =>
            {
                var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<KafkaSettings>>().Value;
                return new KafkaConsumer(settings.BrokerSettings, settings.ConsumerSettings);
            });

            return services;
        }
    }
}
