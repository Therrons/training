using Confluent.Kafka;
using fraud_poc_project.Services;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Exceptions;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo.Exceptions;
using fraud_poc_project_repo.Interfaces;
using fraud_poc_project_repo.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly.Registry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace fraud_poc_project.Kafka.Consumer
{
    public class FraudConsumerWorker : ITransactionEventHandler
    {
        private IConsumer<string, byte[]> _consumer;
        private readonly FraudKafkaBrokerSettings _brokerOptions;
        private readonly FraudKafkaConsumerSettings _consumerOptions;
        private readonly AppSettings _appSettings;
        private readonly ILogger<FraudConsumerWorker> _logger;
        private readonly IFraudRepository _fraudRepository;
        private readonly IServiceScopeFactory _serviceLocator;
        private readonly IFraudEvaluationService _evaluationService;
        private readonly ResiliencePipelineProvider<string> _resilienceProvider;
        private readonly IMetricsService _metricsService;

        private readonly List<ConsumeResult<string, byte[]>> _batch = [];

        private readonly bool _batchSequentialProcessing = false;
        private readonly string _topic = "";

        private readonly int _concurrency = 1;


        public FraudConsumerWorker(
            IOptions<FraudKafkaBrokerSettings> brokerOptions,
            IOptions<FraudKafkaConsumerSettings> consumerOptions,
            IOptions<AppSettings> appSettings,
            ResiliencePipelineProvider<string> resilienceProvider,
            IServiceScopeFactory serviceScopeFactory,
            IFraudRepository fraudRepository,
            IFraudEvaluationService evaluationService,
            IMetricsService metricsService,
            ILogger<FraudConsumerWorker> logger)
        {
            _brokerOptions = brokerOptions.Value;
            _consumerOptions = consumerOptions.Value;
            _appSettings = appSettings.Value;
            _resilienceProvider = resilienceProvider;
            _serviceLocator = serviceScopeFactory;
            _fraudRepository = fraudRepository;
            _evaluationService = evaluationService;
            _metricsService = metricsService;
            _logger = logger;

            _batchSequentialProcessing = _consumerOptions.SequentialProcessing;
            _topic = _consumerOptions.TransactionTopic;
            _concurrency = _consumerOptions.Concurrency;
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            // Translate our own settings objects into the config class the Kafka client library expects.
            var config = new ConsumerConfig
            {
                BootstrapServers = _brokerOptions.BootstrapServers,
                SaslUsername = _brokerOptions.SaslUserName,
                SaslPassword = _brokerOptions.SaslPassword,
                SaslMechanism = _brokerOptions.SaslMechanism,
                SecurityProtocol = _brokerOptions.SecurityProtocol,
                AutoOffsetReset = _consumerOptions.AutoOffsetReset,
                PartitionAssignmentStrategy = _consumerOptions.PartitionAssignmentStrategy,
                EnableAutoCommit = false,  // Manual offset management for at-least-once delivery
                MaxPollIntervalMs = _consumerOptions.MaxPollIntervalMs,
                SessionTimeoutMs = _consumerOptions.SessionTimeoutMs,
                AllowAutoCreateTopics = _brokerOptions.AllowAutoCreateTopics,
                EnablePartitionEof = true,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                FetchMinBytes = _consumerOptions.FetchMinBytes,
                FetchMaxBytes = _consumerOptions.FetchMaxBytes,
                GroupId = _appSettings.GroupId
            };

            _consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetLogHandler((_, message) => _logger.LogKafkaMessage(message))
                .SetErrorHandler((_, error) => _logger.LogKafkaError(error))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformationOnly("Partitions assigned: {Partitions}",
                        string.Join(", ", partitions));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformationOnly("Partitions revoked: {Partitions}",
                        string.Join(", ", partitions));
                })
                .SetPartitionsLostHandler((c, partitions) =>
                {
                    _logger.LogInformationOnly(
                           "ConsumerConfiguration group partitions lost: [{PartitionsLost}]",
                           string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")));
                    _batch.Clear();
                })
                .Build();

            await Task.Run(async () => await Consume(stoppingToken)).ConfigureAwait(false);

            _logger.LogInformationOnly(
                "FraudKafkaConsumer initialized for topic: {Topic}, batchSize: {BatchSize}, batchTimeout: {BatchTimeout}s",
                _consumerOptions.TransactionTopic,
                _consumerOptions.BatchSize,
                _consumerOptions.BatchProcessTimeout);
        }

        private async Task Consume(CancellationToken stoppingToken)
        {
            _consumer!.Subscribe(_topic);
            ConsumeResult<string, byte[]> lastProcessedResult = null;
            var startBatchTime = DateTime.UtcNow;
            var stopBatchTime = startBatchTime.AddSeconds(_consumerOptions.BatchProcessTimeout);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(_consumerOptions.ConsumeMessageIntervalMs));

                    if (consumeResult?.Message is not null)
                    {
                        _batch.Add(consumeResult);
                        lastProcessedResult = consumeResult;

                        _logger.LogTrace(
                            "Added message to batch from partition: {Partition}, offset: {Offset}, current batch size: {BatchSize}",
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value,
                            _batch.Count);
                    }

                    var timeExpired = DateTime.UtcNow >= stopBatchTime;
                    var shouldProcessBatch = _batch.Count >= _consumerOptions.BatchSize ||
                                            (_batch.Count > 0 && timeExpired) ||
                                            (consumeResult is not null && consumeResult.IsPartitionEOF && _batch.Count > 0);

                    if (shouldProcessBatch)
                    {
                        await ProcessBatchAsync(_batch, stoppingToken).ConfigureAwait(false);

                        string trigger = "unknown";
                        if (_batch.Count >= _consumerOptions.BatchSize)
                            trigger = "size";
                        else if (timeExpired)
                            trigger = "timeout";
                        else if (consumeResult.IsPartitionEOF)
                            trigger = "eof";

                        _logger.LogInformationOnly(
                            "Processing batch of {Count} messages (trigger: {Trigger})",
                            _batch.Count,
                            trigger);

                        if (lastProcessedResult != null)
                        {
                            _consumer.Commit(lastProcessedResult);
                            _logger.LogDebug("Committed offset: {Offset}", lastProcessedResult.Offset.Value);
                        }

                        _batch.Clear();
                        lastProcessedResult = null;
                        startBatchTime = DateTime.UtcNow;
                        stopBatchTime = startBatchTime.AddSeconds(_consumerOptions.BatchProcessTimeout);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);

                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal consume error, stopping consumer");
                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformationOnly("Consumer operation cancelled");

                    if (_batch.Count > 0)
                    {
                        _logger.LogInformationOnly("Processing remaining {Count} messages before shutdown", _batch.Count);
                        await ProcessBatchAsync(_batch, CancellationToken.None).ConfigureAwait(false);
                        if (lastProcessedResult != null)
                        {
                            _consumer.Commit(lastProcessedResult);
                            _logger.LogDebug("Committed remaining offset: {Offset}", lastProcessedResult.Offset.Value);
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in batch consumption loop");
                }
            }
        }

        private async Task ProcessBatchAsync(List<ConsumeResult<string, byte[]>> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0) return;

            var startTime = DateTime.UtcNow;
            var deserializedBatch = new List<(TransactionEvent Event, ConsumeResult<string, byte[]> ConsumeResult)>();

            // Step 1: turn each raw Kafka message into a TransactionEvent object.
            foreach (var consumeResult in batch)
            {
                try
                {
                    var eventObj = consumeResult.Message.Stream_Byte_Key_Value_To_String_Key_Value();
                    var transactionEvent = JsonConvert.DeserializeObject<TransactionEvent>(eventObj.Value);

                    if (transactionEvent == null)
                    {
                        _logger.LogWarning("Deserialization returned null for offset: {Offset}", consumeResult.Offset.Value);
                        continue;
                    }

                    deserializedBatch.Add((transactionEvent, consumeResult));
                }
                catch (Newtonsoft.Json.JsonException ex)
                {
                    _logger.LogError(ex, "JSON deserialization error for offset: {Offset}", consumeResult.Offset.Value);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected deserialization error for offset: {Offset}", consumeResult.Offset.Value);
                }
            }

            if (deserializedBatch.Count == 0) return;

            // Step 2: hand the whole batch over to be evaluated and saved.
            try
            {
                if (_batchSequentialProcessing)

                    await HandleBatchTransactionSequentialAsync(deserializedBatch, cancellationToken).ConfigureAwait(false);
                else
                    await HandleBatchTransactionNonSequentialAsync(deserializedBatch, cancellationToken).ConfigureAwait(false);

                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformationOnly(
                        "Successfully processed batch of {Count} messages in {ProcessingTime}ms ({Throughput} msg/sec)",
                        deserializedBatch.Count,
                        processingTime,
                        Math.Round(deserializedBatch.Count / (processingTime / 1000), 2));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Batch processing was cancelled for batch of {Count} messages", deserializedBatch.Count);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing batch of {Count} messages", deserializedBatch.Count);
                foreach (var itm in deserializedBatch)
                    await SendToDltAsync(JsonConvert.SerializeObject(itm.Event), $"Batch processing error: {ex.Message}").ConfigureAwait(false);
            }
        }

        // Evaluates one transaction for fraud and saves the result. If anything goes
        // wrong, the transaction is sent to the dead-letter topic instead of being lost.
        public async Task HandleTransactionAsync((TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits) consumeResult, CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (linkedToken.IsCancellationRequested) { return; }

                var model = JsonConvert.SerializeObject(consumeResult.transactionEvent);
                if (model != null)
                {
                    var evaluationStopwatch = Stopwatch.StartNew();
                    var result = _evaluationService.Evaluate(consumeResult.transactionEvent);
                    evaluationStopwatch.Stop();

                    if (result != null)
                    {
                        var dbStopwatch = Stopwatch.StartNew();
                        var pipeLineRetry = _resilienceProvider.GetPipeline("exception");
                        await pipeLineRetry.ExecuteAsync(async _ =>
                        {
                            await _fraudRepository.SaveFraudEvaluationAsync(result).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                        dbStopwatch.Stop();

                        // Record database operation metric (INSERT)
                        _metricsService.RecordDatabaseOperation("INSERT", dbStopwatch.ElapsedMilliseconds);

                        stopwatch.Stop();

                        // Record transaction metrics
                        _metricsService.RecordTransactionProcessed(
                            result.IsFlagged,
                            result.FraudScore,
                            stopwatch.ElapsedMilliseconds
                        );

                        // Record individual rule metrics
                        if (result.RuleResults != null && result.RuleResults.Count > 0)
                        {
                            // Distribute the evaluation time among all rules
                            long timePerRule = Math.Max(1, evaluationStopwatch.ElapsedMilliseconds / result.RuleResults.Count);

                            foreach (var ruleResult in result.RuleResults)
                            {
                                _metricsService.RecordRuleExecution(
                                    ruleResult.RuleCode,
                                    ruleResult.IsTriggered,
                                    timePerRule
                                );
                            }
                        }

                        _logger.LogInformationOnly("Processed transaction {TransactionId}: flagged={IsFlagged}, score={FraudScore}",
                            consumeResult.transactionEvent.TransactionId.ToString(), result.IsFlagged, result.FraudScore);
                        _logger.LogSensitiveData("Transaction data {TransactionId}: ", result);
                    }
                    else
                        _logger.LogInformationOnly("No Fraud Event Records found for Transaction Event={event}", model);
                }
            }
            catch (FraudEvaluationException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Fraud evaluation failed for transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                linkedCts.Cancel();
                await SendToDltAsync(JsonConvert.SerializeObject(consumeResult.transactionEvent), $"Fraud evaluation error: {ex.Message}").ConfigureAwait(false);
            }
            catch (FraudRepositoryException ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Database error processing transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                linkedCts.Cancel();
                await SendToDltAsync(JsonConvert.SerializeObject(consumeResult.transactionEvent), $"Database error: {ex.Message}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error processing transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                linkedCts.Cancel();
                await SendToDltAsync(JsonConvert.SerializeObject(consumeResult.transactionEvent), $"Unexpected error: {ex.Message}").ConfigureAwait(false);
            }
        }

        // Process batch of Transactions - the total items in the batch 
        // is defined in the ConsumerSettings
        public async Task HandleBatchTransactionSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages, CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

            using var scope = _serviceLocator.CreateScope();
            var batchFraudRepository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();
            int iTotal = messages?.Count ?? 0;

            if (iTotal > 0)
            {
                for (int i = 0; i < iTotal; i++)
                {
                    if (linkedToken.IsCancellationRequested) { return; }

                    if (messages[i].transactionEvent == null) continue;
                    var msg = messages[i];
                    try
                    {
                        await HandleTransactionAsync(msg, linkedToken).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        if (msg.transactionEvent != null)
                        {
                            _logger.LogError(ex, "Failed to consume the following Transaction data={data}", JsonConvert.SerializeObject(msg));
                            await SendToDltAsync(JsonConvert.SerializeObject(msg.transactionEvent), ex.Message).ConfigureAwait(false);
                        }
                        else
                            _logger.LogError(ex, "Failed to consume the following Transaction data - see error object details");

                        linkedCts.Cancel();
                        throw;
                    }
                }
            }
        }

        // Process batch of Transactions - the total items in the batch 
        // is defined in the ConsumerSettings
        public async Task HandleBatchTransactionNonSequentialAsync(
            List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages,
            CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

            int iTotal = messages?.Count ?? 0;
            if (iTotal == 0) return;

            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = _concurrency,
                CancellationToken = linkedToken
            };

            await Parallel.ForEachAsync(messages, parallelOptions, async (msg, ct) =>
            {
                if (ct.IsCancellationRequested) return;

                using var scope = _serviceLocator.CreateScope();
                var fraudRepository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();

                try
                {
                    if (msg.transactionEvent == null) return;
                    await HandleTransactionAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (msg.transactionEvent != null)
                    {
                        _logger.LogError(ex, "Failed to consume the following Transaction data={data}", JsonConvert.SerializeObject(msg));
                        await SendToDltAsync(JsonConvert.SerializeObject(msg.transactionEvent), ex.Message).ConfigureAwait(false);
                    }
                    else
                        _logger.LogError(ex, "Failed to consume the following Transaction data - see error object details");

                    linkedCts.Cancel();
                    throw;
                }
            }).ConfigureAwait(false);
        }

        private async Task SendToDltAsync(string messageData, string error)
        {
            try
            {
                var pipeLineRetry = _resilienceProvider.GetPipeline("exception");
                await pipeLineRetry.ExecuteAsync(async _ =>
                {
                    await _fraudRepository.SavedltErrorAsync(_topic, messageData, error).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write message to DLT for topic {Topic}", _topic);
            }
        }

    }
}
