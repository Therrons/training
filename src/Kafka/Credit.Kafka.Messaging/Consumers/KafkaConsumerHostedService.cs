using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credit.Kafka.Messaging.Consumers;

public class KafkaConsumerHostedService : BackgroundService
{
    private readonly ILogger<KafkaConsumerHostedService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ConsumerOptions _consumerOptions;
    private readonly BrokerOptions _brokerOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IKafkaAuthHandler _kafkaAuthHandler;
    private readonly List<Task> _consumerTasks = new();

    public KafkaConsumerHostedService(ILogger<KafkaConsumerHostedService> logger,
        ILoggerFactory loggerFactory,
        IOptions<ConsumerOptions> consumerOptions,
        IOptions<BrokerOptions> brokerOptions,
        IServiceScopeFactory serviceScopeFactory,
        IKafkaAuthHandler kafkaAuthHandler)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _consumerOptions = consumerOptions.Value;
        _brokerOptions = brokerOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaAuthHandler = kafkaAuthHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int i = 1; i <= _consumerOptions.Concurrency; i++)
        {
            var workerLogger = _loggerFactory.CreateLogger<KafkaSingleConsumerWorker>();
            var worker = new KafkaSingleConsumerWorker($"worker-{i}", workerLogger, _consumerOptions, _brokerOptions, _serviceScopeFactory, _kafkaAuthHandler);
            _consumerTasks.Add(MonitorWorkerAsync(worker, stoppingToken));
        }

        await Task.WhenAll(_consumerTasks);
    }

    private async Task MonitorWorkerAsync(KafkaSingleConsumerWorker worker, CancellationToken stoppingToken)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["WorkerId"] = worker.Id }))
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await worker.StartAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Worker {WorkerId} encountered an error and will be restarted.", worker.Id);
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken); // Delay before restart
                }
            }
        }
    }
}
