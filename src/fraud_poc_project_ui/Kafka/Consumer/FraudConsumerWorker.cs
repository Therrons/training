using Confluent.Kafka;
using fraud_poc_project.Interfaces;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Exceptions;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
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
using System.Text;
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
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IFraudEvaluationService _evaluationService;
        private readonly ResiliencePipelineProvider<string> _resilienceProvider;
        private readonly IMetricsService _metricsService;
        private readonly List<ConsumeResult<string, byte[]>> _batch = [];
        private readonly bool _batchSequentialProcessing = false;
        private readonly string _topic = "";
        private readonly int _concurrency = 1;

        private readonly IFraudProducer _producer;

        public FraudConsumerWorker(
            IOptions<FraudKafkaBrokerSettings> brokerOptions,
            IOptions<FraudKafkaConsumerSettings> consumerOptions,
            IOptions<AppSettings> appSettings,
            ResiliencePipelineProvider<string> resilienceProvider,
            IServiceScopeFactory serviceScopeFactory,
            IFraudEvaluationService evaluationService,
            IMetricsService metricsService,
            IFraudProducer producer,
            ILogger<FraudConsumerWorker> logger)
        {
            _brokerOptions = brokerOptions.Value;
            _consumerOptions = consumerOptions.Value;
            _appSettings = appSettings.Value;
            _resilienceProvider = resilienceProvider;
            _serviceScopeFactory = serviceScopeFactory;
            _evaluationService = evaluationService;
            _metricsService = metricsService;
            _producer = producer;
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

            // A restart must not reuse messages from the failed attempt; Kafka redelivers them from the last commit.
            _batch.Clear();

            try
            {
                await Task.Run(async () => await Consume(stoppingToken)).ConfigureAwait(false);
            }
            finally
            {
                CloseConsumer();
            }

            _logger.LogInformationOnly("FraudKafkaConsumer stopped for topic: {Topic}", _consumerOptions.TransactionTopic);
        }

        // Leaves the consumer group promptly. Offsets are committed manually, so Close does not commit.
        private void CloseConsumer()
        {
            if (_consumer == null) return;

            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error closing Kafka consumer");
            }
            finally
            {
                _consumer.Dispose();
                _consumer = null;
            }
        }

        private async Task Consume(CancellationToken stoppingToken)
        {
            _consumer!.Subscribe(_topic);
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

                        CommitBatch(_batch);
                        _batch.Clear();
                        startBatchTime = DateTime.UtcNow;
                        stopBatchTime = startBatchTime.AddSeconds(_consumerOptions.BatchProcessTimeout);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Consume error: {Error}", ex.Error.Reason);

                    if (ex.Error.IsFatal)
                    {
                        // Rethrown so the consumer retries this worker, then stops it.
                        _logger.LogCritical("Fatal consume error, failing this worker");
                        throw;
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformationOnly("Consumer operation cancelled");

                    if (_batch.Count > 0)
                    {
                        _logger.LogInformationOnly("Processing remaining {Count} messages before shutdown", _batch.Count);
                        await ProcessBatchAsync(_batch, CancellationToken.None).ConfigureAwait(false);
                        CommitBatch(_batch);
                        _batch.Clear();
                    }
                    break;
                }
                catch (Exception ex)
                {
                    // Not retried in place. The worker fails and the consumer restarts it from the last committed offset.
                    _logger.LogError(ex, "Batch consumption failed; failing this worker");
                    throw;
                }
            }
        }

        // Commits the highest processed offset per partition. Kafka's committed offset is the NEXT
        // message to read, so the committed value is last processed + 1. Messages dead-lettered during
        // the batch count as processed. Offsets are per partition, so a batch spanning several partitions
        // commits every one of them, not just the partition of the last message.
        private void CommitBatch(IReadOnlyCollection<ConsumeResult<string, byte[]>> processed)
        {
            if (processed.Count == 0) return;

            var offsets = processed
                .GroupBy(r => r.TopicPartition)
                .Select(g => new TopicPartitionOffset(g.Key, new Offset(g.Max(r => r.Offset.Value) + 1)))
                .ToList();

            _consumer.Commit(offsets);
            _logger.LogDebug("Committed offsets for {PartitionCount} partition(s)", offsets.Count);
        }

        private async Task ProcessBatchAsync(List<ConsumeResult<string, byte[]>> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0) return;

            var startTime = DateTime.UtcNow;
            var deserializedBatch = new List<(TransactionEvent Event, ConsumeResult<string, byte[]> ConsumeResult)>();

            // Step 1: turn each raw Kafka message into a TransactionEvent object, then validate it.
            foreach (var consumeResult in batch)
            {
                // Malformed payloads go to the DLT as raw text. Retrying them would fail the batch forever and block the partition.
                var raw = consumeResult.Message.Value is null ? string.Empty : Encoding.UTF8.GetString(consumeResult.Message.Value);
                TransactionEvent transactionEvent;
                try
                {
                    transactionEvent = JsonConvert.DeserializeObject<TransactionEvent>(raw);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "JSON deserialization error for offset: {Offset}", consumeResult.Offset.Value);
                    await SendToDltTableAsync(raw, ex.Message).ConfigureAwait(false);
                    continue;
                }

                if (transactionEvent == null)
                {
                    _logger.LogWarning("Deserialization returned null for offset: {Offset}", consumeResult.Offset.Value);
                    await SendToDltTableAsync(raw, "Deserialization returned null").ConfigureAwait(false);
                    continue;
                }

                // Structurally invalid events are dead-lettered, never scored or persisted.
                TransactionEventValidator.NormalizeTransactionTime(transactionEvent);
                var validationErrors = TransactionEventValidator.Validate(transactionEvent);
                if (validationErrors.Count > 0)
                {
                    _logger.LogWarning("Validation failed for TransactionId={TransactionId}: {Errors}",
                        transactionEvent.TransactionId, string.Join("; ", validationErrors));
                    await SendToDltTableAsync(raw, "Validation failed: " + string.Join("; ", validationErrors)).ConfigureAwait(false);
                    continue;
                }

                deserializedBatch.Add((transactionEvent, consumeResult));
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
                _logger.LogError(ex, "Error processing batch of {Count} messages; failing this worker", deserializedBatch.Count);
                throw;
            }
        }

        // Runs a database operation on a fresh DI scope. The consumer is a singleton, so it must not hold
        // a scoped IFraudRepository itself.
        private async Task WithRepositoryAsync(Func<IFraudRepository, Task> operation)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<IFraudRepository>();
            await operation(repository).ConfigureAwait(false);
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

                var evaluationStopwatch = Stopwatch.StartNew();
                var result = _evaluationService.Evaluate(consumeResult.transactionEvent);
                evaluationStopwatch.Stop();

                if (result != null)
                {
                    var dbStopwatch = Stopwatch.StartNew();
                    var pipeLineRetry = _resilienceProvider.GetPipeline("exception");
                    await pipeLineRetry.ExecuteAsync(async _ =>
                    {
                        await WithRepositoryAsync(repository => repository.SaveFraudEvaluationAsync(result)).ConfigureAwait(false);
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
                {
                    throw new FraudEvaluationException(
                        $"No fraud event records produced for TransactionId={consumeResult.transactionEvent.TransactionId}");
                }
            }
            catch (FraudEvaluationException ex)
            {
                stopwatch.Stop();
                linkedCts.Cancel();
                _logger.LogError(ex, "Fraud evaluation failed for TransactionId={TransactionId}", consumeResult.transactionEvent.TransactionId);
                await SendToDltAsync(consumeResult.transactionEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (FraudRepositoryException ex)
            {
                stopwatch.Stop();
                linkedCts.Cancel();
                await SendToDltTableAsync(JsonConvert.SerializeObject(consumeResult.transactionEvent), $"Database error: {ex.Message}").ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "Unexpected error processing transaction {TransactionId}", consumeResult.transactionEvent.TransactionId);
                linkedCts.Cancel();
                await SendToDltTableAsync(JsonConvert.SerializeObject(consumeResult.transactionEvent), $"Unexpected error: {ex.Message}").ConfigureAwait(false);
            }
        }

        // Process batch of Transactions - the total items in the batch
        // is defined in the ConsumerSettings
        public async Task HandleBatchTransactionSequentialAsync(List<(TransactionEvent transactionEvent, ConsumeResult<string, byte[]> transactionEventAsBits)> messages, CancellationToken cancellationToken)
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var linkedToken = linkedCts.Token;

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
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        // Dead-letter only this message. Do not cancel or rethrow, so the rest of the batch is processed.
                        _logger.LogError(ex, "Failed to process TransactionId={TransactionId}", msg.transactionEvent.TransactionId);
                        await SendToDltTableAsync(JsonConvert.SerializeObject(msg.transactionEvent), ex.Message).ConfigureAwait(false);
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

                try
                {
                    if (msg.transactionEvent == null) return;
                    await HandleTransactionAsync(msg, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Dead-letter only this message. Do not cancel or rethrow, so the rest of the batch is processed.
                    _logger.LogError(ex, "Failed to process TransactionId={TransactionId}", msg.transactionEvent.TransactionId);
                    await SendToDltTableAsync(JsonConvert.SerializeObject(msg.transactionEvent), ex.Message).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }


        private async Task SendToDltTableAsync(string messageData, string error)
        {
            var pipeLineRetry = _resilienceProvider.GetPipeline("exception");
            try
            {
                await pipeLineRetry.ExecuteAsync(async _ =>
                {
                    await WithRepositoryAsync(repository => repository.SaveDBDltErrorAsync(_topic, messageData, error)).ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to write message to DLT table for topic {Topic}. The batch will not be committed and will be retried.", _topic);
                throw;
            }
        }

        private async Task SendToDltAsync(TransactionEvent @event, CancellationToken cts)
        {
            var pipeLineRetry = _resilienceProvider.GetPipeline("exception");
            try
            {
                await pipeLineRetry.ExecuteAsync(async _ =>
                {
                    await _producer.ProduceDltAsync(@event, cts).ConfigureAwait(false);
                }, cts).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Failed to write message to DLT topic for TransactionId={TransactionId}. Recording in DLT table instead.", @event?.TransactionId);
                await SendToDltTableAsync(JsonConvert.SerializeObject(@event), $"DLT error: {ex.Message}").ConfigureAwait(false);
                throw;
            }
        }
    }
}
