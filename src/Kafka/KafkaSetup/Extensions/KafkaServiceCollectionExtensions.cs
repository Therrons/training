using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KafkaSetup.Extensions
{
    public static class KafkaServiceCollectionExtensions
    {
        public static IServiceCollection AddKafka(
            this IServiceCollection services,
            IConfiguration configuration,
            string sectionName = "KafkaSettings")
        {
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
