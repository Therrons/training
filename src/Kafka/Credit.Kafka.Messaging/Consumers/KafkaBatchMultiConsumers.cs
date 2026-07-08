using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Extensions;
using Credit.Kafka.Messaging.Handlers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Credit.Kafka.Messaging.Consumers
{
    public class KafkaBatchMultiConsumers<T> : BackgroundService, IBatchMultiConsumer_Start_Stop
    {
        private readonly ILogger<KafkaBatchMultiConsumers<T>> _logger;
        private readonly ILoggerFactory _loggerFactory;
        private readonly BatchConsumerOptions _batchConsumerOptions;
        private readonly BrokerOptions _brokerOptions;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IKafkaAuthHandler? _kafkaAuthHandler;

        private static readonly List<Thread> _workerThreads = [];

        private readonly static ConcurrentBag<KafkaBatchMultiConsumersWorker> _workerInstances = [];

        private static List<string> KafkaTopicNames { get; set; }

        public KafkaBatchMultiConsumers(
            ILogger<KafkaBatchMultiConsumers<T>> logger,
            ILoggerFactory loggerFactory,
            IOptions<BatchConsumerOptions> batchConsumerOptions,
            IOptions<BrokerOptions> brokerOptions,
            IServiceScopeFactory serviceScopeFactory,
            IKafkaAuthHandler kafkaAuthHandler)
        {
            _logger = logger;
            _loggerFactory = loggerFactory;
            _batchConsumerOptions = batchConsumerOptions?.Value as BatchConsumerOptions
                ?? throw new ArgumentException("consumerOptions must be of type BatchConsumerOptions", nameof(batchConsumerOptions));
            _brokerOptions = brokerOptions.Value;
            _serviceScopeFactory = serviceScopeFactory;
            _kafkaAuthHandler = kafkaAuthHandler;

            // get a list of all the topics that this consumer services subscribed to
            KafkaTopicNames = _batchConsumerOptions.MessageHandlers?
                .Select(kv => kv.Key?.Trim().ToLower() ?? string.Empty)
                .ToList() ?? [];

            _batchConsumerOptions.Topics = KafkaTopicNames; // ensure topics are set in the options
        }

        // Keep the original parameterless API for IBatchMultiConsumer_Start_Stop compatibility.
        public async Task Service_Enable_Toggle()
        {
            var newConfig = Startup_AWS_ConfigSettings.Get_Startup_AWS_ConfigSettings_To_Dictionary();
            await Service_Enable_Toggle(newConfig).ConfigureAwait(false);
        }

        // New overload: accept a per-topic dictionary (topic -> enabled)
        private async Task Service_Enable_Toggle(Dictionary<string, bool>? newConfig)
        {

            if (!_workerInstances.IsEmpty)
            {
                var parallelOptions = new ParallelOptions { MaxDegreeOfParallelism = _batchConsumerOptions.Concurrency };
                await Parallel.ForEachAsync(_workerInstances, parallelOptions, async (worker, cancellationToken) =>
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogInformation($"Updating worker {GetCurrentThreadId()} to status (apply toggles) for topics: {Newtonsoft.Json.JsonConvert.SerializeObject(newConfig)}");
                    }

                    if (newConfig != null && newConfig.Any())
                    {
                        try
                        {
                            // worker now accepts a per-topic dictionary and applies it to its instance topics
                            worker.PauseConsumer(newConfig);
                        }
                        catch (MissingMethodException)
                        {
                            // fallback to toggle behavior if worker doesn't support the dictionary overload
                            _logger.LogWarning("Worker does not support per-topic pause; falling back to toggle.");
                            worker.PauseConsumer();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error applying per-topic toggles to worker {WorkerId}", GetCurrentThreadId());
                        }
                    }
                    else
                    {
                        // toggle instance-level pause for backwards compatibility
                        worker.PauseConsumer();
                    }
                }).ConfigureAwait(false);
                await Task.WhenAll();
            }
        }
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            for (int i = 1; i <= _batchConsumerOptions.Concurrency; i++)
            {
                var workerLogger = _loggerFactory.CreateLogger<KafkaBatchMultiConsumersWorker>();
                var worker = new KafkaBatchMultiConsumersWorker("worker-" + i, workerLogger, _batchConsumerOptions, _brokerOptions, _serviceScopeFactory, _kafkaAuthHandler, _batchConsumerOptions.GroupId);
                _workerInstances.Add(worker);
                Service_Enable_Toggle().ConfigureAwait(false);

                var thread = new Thread(() => MonitorWorkerSync(worker, stoppingToken))
                {
                    IsBackground = true,
                    Name = GetCurrentThreadId().ToString()
                };

                _workerThreads.Add(thread);
                thread.Start();
            }

            // Return a completed task because threads run independently.
            return Task.CompletedTask;
        }

        private void MonitorWorkerSync(KafkaBatchMultiConsumersWorker worker, CancellationToken stoppingToken)
        {
            using (_logger.BeginScope(new Dictionary<string, object> { ["WorkerId"] = GetCurrentThreadId() }))
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    try
                    {
                        worker.StartAsync(stoppingToken).GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Worker {WorkerId} encountered an error and will be restarted.", GetCurrentThreadId());
                        Thread.Sleep(TimeSpan.FromSeconds(5));
                    }
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            // stoppingToken passed to MonitorWorkerSync is provided by framework,
            // the framework will request cancellation; here we can wait for threads to finish:
            foreach (var t in _workerThreads)
            {
                if (t.IsAlive)
                {
                    t.Join(TimeSpan.FromSeconds(5)); // wait, but don't block forever
                }
            }

            return base.StopAsync(cancellationToken);
        }

        private static int GetCurrentThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}