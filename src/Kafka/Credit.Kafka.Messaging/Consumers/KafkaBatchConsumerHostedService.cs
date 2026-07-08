using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Credit.Kafka.Messaging.Consumers;

public class KafkaBatchConsumerHostedService : BackgroundService
{
    private readonly ILogger<KafkaBatchConsumerHostedService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly BatchConsumerOptions _batchConsumerOptions;
    private readonly BrokerOptions _brokerOptions;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IKafkaAuthHandler _kafkaAuthHandler;
    private readonly List<Task> _consumerTasks = new();

    public KafkaBatchConsumerHostedService(ILogger<KafkaBatchConsumerHostedService> logger,
        ILoggerFactory loggerFactory,
        IOptions<BatchConsumerOptions> consumerOptions,
        IOptions<BrokerOptions> brokerOptions,
        IServiceScopeFactory serviceScopeFactory,
        IKafkaAuthHandler kafkaAuthHandler)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _batchConsumerOptions = consumerOptions.Value;
        _brokerOptions = brokerOptions.Value;
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaAuthHandler = kafkaAuthHandler;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int i = 1; i <= _batchConsumerOptions.Concurrency; i++)
        {
            var workerLogger = _loggerFactory.CreateLogger<KafkaBatchConsumerWorker>();
            var worker = new KafkaBatchConsumerWorker($"worker-{i}", workerLogger, _batchConsumerOptions, _brokerOptions, _serviceScopeFactory, _kafkaAuthHandler);
            _consumerTasks.Add(MonitorWorkerAsync(worker, stoppingToken));
        }

        await Task.WhenAll(_consumerTasks);
    }

    private async Task MonitorWorkerAsync(KafkaBatchConsumerWorker worker, CancellationToken stoppingToken)
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
