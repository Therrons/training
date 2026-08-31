using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo.Interfaces;
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
        private readonly IFraudRepository _fraudRepository;
        private readonly IFraudEvaluationService _evaluationService;
        private readonly ILogger<FraudConsumer> _logger;

        private readonly string consumerTopic = "";
        private readonly int consumerCount = 0;

        public FraudConsumer(
                    IOptions<FraudKafkaBrokerSettings> brokerOptions,
                    IOptions<FraudKafkaConsumerSettings> consumerOptions,
                    IOptions<KafkaAdminOptions> kafkaAdminOptionsSettings,
                    ILoggerFactory loggerFactory,
                    IServiceScopeFactory serviceScopeFactory,
                    IOptions<AppSettings> appSettings,
                    ResiliencePipelineProvider<string> resilienceProvider,
                    IFraudRepository fraudRepository,
                    IFraudEvaluationService evaluationService,
                    ILogger<FraudConsumer> logger)
        {
            _brokerOptions = brokerOptions;
            _consumerOptions = consumerOptions;
            _kafkaAdminOptionsSettings = kafkaAdminOptionsSettings.Value;
            _loggerFactory = loggerFactory;
            _serviceScopeFactory = serviceScopeFactory;
            _appSettings = appSettings;
            _resilienceProvider = resilienceProvider;
            _fraudRepository = fraudRepository;
            _evaluationService = evaluationService;
            _logger = logger;

            consumerTopic = _consumerOptions.Value.TransactionTopic.Trim();

            var consumerInstances = _kafkaAdminOptionsSettings.TopicOptions.FirstOrDefault(t => t.Topic.ToLower().Trim() == consumerTopic.ToLower()) ?? new KafkaTopicOptions();
            consumerCount = consumerInstances?.Partitions ?? 0;
        }

        // This runs automatically when the app starts, and keeps running until the app
        // shuts down. (We override ExecuteAsync, which BackgroundService calls for us,
        // instead of StartAsync.)
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
                    var itm = new FraudConsumerWorker(_brokerOptions, _consumerOptions, _appSettings, _resilienceProvider, _serviceScopeFactory, _fraudRepository, _evaluationService, workerLogger);
                    workers.Add(itm);
                }
                ;
                var tasks = workers.Select(w => RunWorkerWithRetryAsync(w, stoppingToken));
                await Task.WhenAll(tasks);
            }
            finally
            {
                _logger.LogInformationOnly("Kafka consumer closed");
            }
        }

        private async Task RunWorkerWithRetryAsync(FraudConsumerWorker worker, CancellationToken stoppingToken)
        {
            const int maxRetries = 5;
            int retryCount = 0;

            while (retryCount < maxRetries && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await worker.StartAsync(stoppingToken);
                    retryCount = 0;
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    retryCount++;
                    _logger.LogError(ex, "Worker failed. Retry {Retry}/{Max}", retryCount, maxRetries);
                    if (retryCount < maxRetries)
                        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
            }
        }
        private static int GetCurrentThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
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

        ~FraudConsumer()
        {
            _logger.LogInformationOnly("Stopping FraudConsumer...");
            base.StopAsync(CancellationToken.None);
        }
    }
}