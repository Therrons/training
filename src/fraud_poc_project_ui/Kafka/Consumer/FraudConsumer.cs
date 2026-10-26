using fraud_poc_project.Interfaces;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_repo.Interfaces;
using fraud_poc_project_repo.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OptionsModels.KafkaOptions;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Kafka.Consumer
{
    // Runs in the background for as long as the app is running. It continuously reads
    // transaction messages from Kafka, groups them into small batches, and passes each
    // batch off to be evaluated for fraud and saved to the database.
    public class FraudConsumer : BackgroundService
    {

        private readonly IOptions<FraudKafkaBrokerSettings> _brokerOptions;
        private readonly IOptions<FraudKafkaConsumerSettings> _consumerOptions;
        private readonly KafkaAdminOptions _kafkaAdminOptionsSettings;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IOptions<AppSettings> _appSettings;
        private readonly ResiliencePipelineProvider<string> _resilienceProvider;
        private readonly IFraudEvaluationService _evaluationService;
        private readonly ILogger<FraudConsumer> _logger;

        private readonly IMetricsService _metricsService;

        private readonly string consumerTopic = "";
        private readonly int consumerCount = 0;

        private readonly IFraudProducer _producer;

        public FraudConsumer(
                    IOptions<FraudKafkaBrokerSettings> brokerOptions,
                    IOptions<FraudKafkaConsumerSettings> consumerOptions,
                    IOptions<KafkaAdminOptions> kafkaAdminOptionsSettings,
                    ILoggerFactory loggerFactory,
                    IServiceScopeFactory serviceScopeFactory,
                    IOptions<AppSettings> appSettings,
                    ResiliencePipelineProvider<string> resilienceProvider,
                    IFraudEvaluationService evaluationService,
                    ILogger<FraudConsumer> logger,
                    IMetricsService metricsService,
                    IFraudProducer producer)
        {
            _brokerOptions = brokerOptions;
            _consumerOptions = consumerOptions;
            _kafkaAdminOptionsSettings = kafkaAdminOptionsSettings.Value;
            _loggerFactory = loggerFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _appSettings = appSettings;
            _resilienceProvider = resilienceProvider;
            _evaluationService = evaluationService;
            _logger = logger;
            _metricsService = metricsService;
            _producer = producer;
            consumerTopic = _consumerOptions.Value.TransactionTopic.Trim();

            var consumerInstances = _kafkaAdminOptionsSettings.TopicOptions.FirstOrDefault(t => t.Topic.ToLower().Trim() == consumerTopic.ToLower()) ?? new KafkaTopicOptions();
            consumerCount = consumerInstances?.Partitions ?? 0;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformationOnly("Starting Kafka consumer for topic: {Topic} with batch processing",
                               consumerTopic);

                List<FraudConsumerWorker> workers = new List<FraudConsumerWorker>();
                foreach (var item in Enumerable.Range(0, consumerCount))
                {
                    var workerLogger = _loggerFactory.CreateLogger<FraudConsumerWorker>();
                    var itm = new FraudConsumerWorker(_brokerOptions, _consumerOptions, _appSettings, _resilienceProvider, _serviceScopeFactory, _evaluationService, _metricsService, _producer, workerLogger);
                    workers.Add(itm);
                }
                ;
                // One source for all workers: if any worker gives up, the whole consumer stops.
                using var consumerCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                var tasks = workers.Select(w => RunWorkerWithRetryAsync(w, consumerCts));
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            finally
            {
                _logger.LogInformationOnly("Kafka consumer closed");
            }
        }

        private async Task RunWorkerWithRetryAsync(FraudConsumerWorker worker, CancellationTokenSource consumerCts)
        {
            int maxRetries = _consumerOptions.Value.StartWorkerRetry;
            int failures = 0;
            var stoppingToken = consumerCts.Token;

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // StartAsync only returns normally on shutdown; any failure is an exception.
                    await worker.StartAsync(stoppingToken).ConfigureAwait(false);
                    return;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformationOnly("Consumer operation cancelled");
                    return;
                }
                catch (Exception ex)
                {
                    failures++;

                    if (failures > maxRetries)
                    {
                        // Stop this worker only. The API keeps running; restart the service to resume consuming.
                        _logger.LogCritical(ex, "Consumer stopped after {Attempts} attempts", failures);
                        consumerCts.Cancel();
                        return;
                    }

                    _logger.LogError(ex, "Worker failed. Retry {Retry}/{Max}", failures, maxRetries);

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        return;
                    }
                }
            }
        }

        public override void Dispose()
        {
            try
            {
                _logger.LogInformationOnly("FraudKafkaConsumer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing FraudKafkaConsumer");
            }
            base.Dispose();
        }

    }
}