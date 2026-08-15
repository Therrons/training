using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace fraud_poc_project_repo.Kafka
{
    // Runs in the background for as long as the app is running. It continuously reads
    // transaction messages from Kafka, groups them into small batches, and passes each
    // batch off to be evaluated for fraud and saved to the database.
    public class FraudConsumer : BackgroundService
    {
        private readonly IConsumer<string, byte[]> _consumer;
        private readonly ITransactionEventHandler _eventHandler;
        private readonly IFraudProducer _deadLetterProducer;
        private readonly FraudKafkaConsumerSettings _consumerOptions;
        private readonly AppSettings _appSettings;
        private readonly ILogger<FraudConsumer> _logger;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public FraudConsumer(
            IOptions<FraudKafkaBrokerSettings> brokerOptions,
            IOptions<FraudKafkaConsumerSettings> consumerOptions,
            IOptions<AppSettings> appSettings,
            ITransactionEventHandler eventHandler,
            IFraudProducer deadLetterProducer,
            ILogger<FraudConsumer> logger)
        {
            _logger = logger;
            _consumerOptions = consumerOptions.Value;
            _eventHandler = eventHandler;
            _deadLetterProducer = deadLetterProducer;
            _appSettings = appSettings.Value;

            // Translate our own settings objects into the config class the Kafka client library expects.
            var config = new ConsumerConfig
            {
                BootstrapServers = brokerOptions.Value.BootstrapServers,
                SaslUsername = brokerOptions.Value.SaslUserName,
                SaslPassword = brokerOptions.Value.SaslPassword,
                SaslMechanism = brokerOptions.Value.SaslMechanism,
                SecurityProtocol = brokerOptions.Value.SecurityProtocol,
                AutoOffsetReset = _consumerOptions.AutoOffsetReset,
                PartitionAssignmentStrategy = _consumerOptions.PartitionAssignmentStrategy,
                EnableAutoCommit = _consumerOptions.EnableAutoCommit,
                MaxPollIntervalMs = _consumerOptions.MaxPollIntervalMs,
                SessionTimeoutMs = _consumerOptions.SessionTimeoutMs,
                AllowAutoCreateTopics = brokerOptions.Value.AllowAutoCreateTopics,
                EnablePartitionEof = false,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                // Batch optimization settings
                FetchMinBytes = _consumerOptions.FetchMinBytes,
                FetchMaxBytes = _consumerOptions.FetchMaxBytes,
                GroupId = _appSettings.GroupId
            };

            _consumer = new ConsumerBuilder<string, byte[]>(config)
                .SetLogHandler((_, message) => LogKafkaMessage(message))
                .SetErrorHandler((_, error) => LogKafkaError(error))
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    _logger.LogInformation("Partitions assigned: {Partitions}",
                        string.Join(", ", partitions));
                })
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    _logger.LogInformation("Partitions revoked: {Partitions}",
                        string.Join(", ", partitions));
                })
                .Build();

            _logger.LogInformation(
                "FraudKafkaConsumer initialized for topic: {Topic}, batchSize: {BatchSize}, batchTimeout: {BatchTimeout}s",
                _consumerOptions.TransactionTopic,
                _consumerOptions.BatchSize,
                _consumerOptions.BatchTimeoutSeconds);
        }

        // This runs automatically when the app starts, and keeps running until the app
        // shuts down. (We override ExecuteAsync, which BackgroundService calls for us,
        // instead of StartAsync.)
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_consumerOptions.TransactionTopic);

            _logger.LogInformation("Starting Kafka consumer for topic: {Topic} with batch processing",
                _consumerOptions.TransactionTopic);

            try
            {
                await Task.Run(async () => await RunConsumerLoopAsync(stoppingToken));
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("Kafka consumer closed");
            }
        }

        // The main loop: keep grabbing messages one at a time and collecting them into
        // a batch. Once the batch is either "full" (reached BatchSize) or has been
        // waiting too long (reached BatchTimeoutSeconds), process it and start a new batch.
        private async Task RunConsumerLoopAsync(CancellationToken cancellationToken)
        {
            var batch = new List<ConsumeResult<string, byte[]>>();
            var lastBatchTime = DateTime.UtcNow;
            var batchTimeout = TimeSpan.FromSeconds(_consumerOptions.BatchTimeoutSeconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Check for one new message, but don't wait long - we need to keep
                    // checking the batch-timeout condition below even if nothing new arrives.
                    var consumeResult = _consumer.Consume(TimeSpan.FromMilliseconds(100));

                    if (consumeResult?.Message != null)
                    {
                        batch.Add(consumeResult);

                        _logger.LogTrace(
                            "Added message to batch from partition: {Partition}, offset: {Offset}, current batch size: {BatchSize}",
                            consumeResult.Partition.Value,
                            consumeResult.Offset.Value,
                            batch.Count);
                    }

                    // Time to process the batch if it's full, or if it's non-empty and has
                    // been waiting around longer than the configured timeout.
                    var timeSinceLastBatch = DateTime.UtcNow - lastBatchTime;
                    var shouldProcessBatch = batch.Count >= _consumerOptions.BatchSize ||
                                            (batch.Count > 0 && timeSinceLastBatch >= batchTimeout);

                    if (shouldProcessBatch)
                    {
                        _logger.LogInformation(
                            "Processing batch of {Count} messages (trigger: {Trigger})",
                            batch.Count,
                            batch.Count >= _consumerOptions.BatchSize ? "size" : "timeout");

                        await ProcessBatchAsync(batch, cancellationToken);

                        // Commit the last offset after successful processing
                        if (batch.Count > 0)
                        {
                            _consumer.Commit(batch[^1]);
                            _logger.LogDebug("Committed offset: {Offset}", batch[^1].Offset.Value);  // batch[^1] is the new way of saying batch[batch.Count - 1]
                        }

                        batch.Clear();
                        lastBatchTime = DateTime.UtcNow;
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
                    _logger.LogInformation("Consumer operation cancelled");

                    // Process remaining messages in batch before stopping
                    if (batch.Count > 0)
                    {
                        _logger.LogInformation("Processing remaining {Count} messages before shutdown", batch.Count);
                        await ProcessBatchAsync(batch, CancellationToken.None);
                        if (batch.Count > 0)
                        {
                            _consumer.Commit(batch[^1]);  // batch[^1] is the new way of saying batch[batch.Count - 1]
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

        // Takes one batch of raw Kafka messages and turns them into fraud results:
        //   1. Turn each raw message into a TransactionEvent (skip/log any that fail).
        //   2. Hand the whole batch to the event handler to evaluate and save.
        //   3. If that fails, send every message in the batch to the dead-letter topic.
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
                    var transactionEvent = JsonSerializer.Deserialize<TransactionEvent>(consumeResult.Message.Value, _jsonOptions);

                    if (transactionEvent == null)
                    {
                        _logger.LogWarning("Deserialization returned null for offset: {Offset}", consumeResult.Offset.Value);
                        continue;
                    }

                    deserializedBatch.Add((transactionEvent, consumeResult));
                }
                catch (JsonException ex)
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
                await _eventHandler.HandleBatchAsync(deserializedBatch, cancellationToken);
                var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

                _logger.LogInformation(
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
                // Step 3: if handling the batch failed, don't lose the messages - send
                // every one of them to the dead-letter topic so they can be looked at later.
                _logger.LogError(ex, "Error processing batch of {Count} messages", deserializedBatch.Count);
                foreach (var itm in deserializedBatch)
                    await SendToDeadLetterAsync(itm.Event, $"Batch processing error: {ex.Message}");
            }
        }

        // Sends one failed transaction event to the dead-letter topic. If even that fails,
        // we just log it - there's nowhere else left to send it.
        private async Task SendToDeadLetterAsync(TransactionEvent consumeResult, string errorMessage)
        {
            try
            {
                await _deadLetterProducer.ProduceDltAsync(consumeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send message to dead letter topic");
            }
        }

        // Converts a raw Kafka log entry into one of our normal log levels, so it shows
        // up consistently alongside the rest of the app's logs.
        private void LogKafkaMessage(LogMessage logMessage)
        {
            var level = logMessage.Level switch
            {
                SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                SyslogLevel.Warning => LogLevel.Warning,
                SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                _ => LogLevel.Debug
            };

            _logger.Log(level, "Kafka log: {Message}", logMessage.Message);
        }

        // Logs a Kafka client error. "Fatal" errors mean the connection is broken and
        // the consumer likely can't recover on its own, so those get logged as critical.
        private void LogKafkaError(Error error)
        {
            if (error.IsFatal)
            {
                _logger.LogCritical("Kafka fatal error: {Code} - {Reason}", error.Code, error.Reason);
            }
            else
            {
                _logger.LogError("Kafka error: {Code} - {Reason}", error.Code, error.Reason);
            }
        }

        public override void Dispose()
        {
            try
            {
                _consumer?.Close();
                _consumer?.Dispose();
                _logger.LogInformation("FraudKafkaConsumer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing FraudKafkaConsumer");
            }

            base.Dispose();
        }

        ~FraudConsumer()
        {
            _logger.LogInformation("Stopping FraudConsumer...");
            base.StopAsync(CancellationToken.None);
        }
    }
}