using Credit.Kafka.Messaging.Builders;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Consumers;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;
using Credit.Kafka.Messaging.Producers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credit.Kafka.Messaging.DependencyInjection;

public static class KafkaConfiguration
{
    private static readonly List<string> _consumerGroups = []; // this is to ensure that we have unique consumer groups

    /// <summary>
    /// Registers Kafka broker configuration
    /// </summary>
    /// <param name="services"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterKafkaBroker(this IServiceCollection services,
        BrokerOptionsBuilder builder)
    {
        var options = builder.Build();

        OptionsValidator.ValidateOptions(options);

        // Register the broker options
        services.Configure<BrokerOptions>(brokerOptions =>
        {
            brokerOptions.BootstrapServers = options.BootstrapServers;
            brokerOptions.SaslUserName = options.SaslUserName;
            brokerOptions.SaslPassword = options.SaslPassword;
            brokerOptions.SaslMechanism = options.SaslMechanism;
            brokerOptions.SecurityProtocol = options.SecurityProtocol;
            brokerOptions.TopicOptions = options.TopicOptions;
            brokerOptions.IamRoleArn = options.IamRoleArn;
        });

        if (options.TopicOptions.Count != 0)
            TopicHelper.CreateTopics(options.TopicOptions, options);

        // Register auth handler if not already registered
        if (services.All(x => x.ServiceType != typeof(IKafkaAuthHandler)))
        {
            services.AddSingleton<IKafkaAuthHandler, KafkaAuthHandler>();
        }
        return services;
    }

    public static IServiceCollection RegisterKafkaBatchMultiConsumers<TService, TOptions>(
            this IServiceCollection services, BatchConsumerOptionsBuilder builder)
            where TService : class, IHostedService
            where TOptions : BatchConsumerOptionsBuilder, new()
    {
        var options = builder.Build();

        OptionsValidator.ValidateOptions(options);

        // Use the GroupId as the unique identifier for multiple consumer groups
        var consumerGroupId = options.GroupId;

        _consumerGroups.Add(consumerGroupId);  // if the consumer group already exists this will throw an error - which is what we want

        // Register consumer options with named options using GroupId
        services.Configure<BatchConsumerOptions>(consumerGroupId, consumerOptions =>
        {
            consumerOptions.Topics = options.MessageHandlers.Keys
                   .Where(t => !string.IsNullOrWhiteSpace(t))
                   .Select(t => t)
                   .Distinct(StringComparer.OrdinalIgnoreCase)
                   .ToList();

            consumerOptions.GroupId = options.GroupId;
            consumerOptions.AutoOffsetReset = options.AutoOffsetReset;
            consumerOptions.PartitionAssignmentStrategy = options.PartitionAssignmentStrategy;
            consumerOptions.AllowAutoCreateTopics = options.AllowAutoCreateTopics;
            consumerOptions.ConsumeDomainEvents = options.ConsumeDomainEvents;
            consumerOptions.Concurrency = options.Concurrency;

            // Copy message handlers
            foreach (var handler in options.MessageHandlers)
            {
                consumerOptions.MessageHandlers[handler.Key] = handler.Value;
            }
        });

        foreach (var type in options.MessageHandlers.Values)
        {
            services.AddTransient(type);
        }

        services.AddSingleton<IHostedService>(sp =>
        {
            using var scope = sp.CreateScope();
            var optionsSnapshot = scope.ServiceProvider.GetRequiredService<IOptionsSnapshot<BatchConsumerOptions>>();
            var batchOptions = optionsSnapshot.Get(consumerGroupId); // get specific options as each consumer will have different options

            IOptions<BatchConsumerOptions> batchConsumerOptions = Options.Create(batchOptions);

            var serviceLogger = sp.GetRequiredService<ILogger<KafkaBatchMultiConsumers<TService>>>();
            var brokerOptions = sp.GetRequiredService<IOptions<BrokerOptions>>();
            var loggerFactory = sp.GetRequiredService<ILoggerFactory>();
            var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
            var kafkaAuthHandler = sp.GetRequiredService<IKafkaAuthHandler>();

            KafkaBatchMultiConsumers<TService> instance = new(serviceLogger, loggerFactory, batchConsumerOptions, brokerOptions, serviceScopeFactory, kafkaAuthHandler);
            return instance;
        }
        !);
        return services;
    }

    /// <summary>
    /// Registers a Kafka producer with the provided options
    /// </summary>
    /// <param name="services"></param>
    /// <param name="builder"></param>
    /// <returns></returns>
    public static IServiceCollection RegisterKafkaProducer(this IServiceCollection services,
        ProducerOptionsBuilder builder)
    {
        var options = builder.Build();

        OptionsValidator.ValidateOptions(options);

        // Register producer options
        services.Configure<ProducerOptions>(producerOptions =>
        {
            producerOptions.CompressionType = options.CompressionType;
            producerOptions.AllowAutoCreateTopics = options.AllowAutoCreateTopics;
            producerOptions.TransactionId = options.TransactionId;
            producerOptions.DefaultTransactionTimeoutInSeconds = options.DefaultTransactionTimeoutInSeconds;
            producerOptions.ProducingApplicationName = options.ProducingApplicationName;
            producerOptions.MessageTimeoutMs = options.MessageTimeoutMs;
            producerOptions.LingerMs = options.LingerMs;
        });

        // Register producer service
        services.AddSingleton<IDomainProducer, DomainProducer>();

        return services;
    }

    public static List<string> Valid_GroupIDs() // check if we have duplicate group id - as this is not to be allowed
    {
        return (_consumerGroups
            .Select(s => s.Trim().ToLowerInvariant())
            .GroupBy(s => s)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key!).ToList()) ?? [];
    }
}