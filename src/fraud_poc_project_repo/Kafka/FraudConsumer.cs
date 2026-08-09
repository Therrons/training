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

        // Override ExecuteAsync instead of StartAsync
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _consumer.Subscribe(_consumerOptions.TransactionTopic);

            _logger.LogInformation("Starting Kafka consumer for topic: {Topic} with batch processing",
                _consumerOptions.TransactionTopic);

            try
            {
                await RunConsumerLoopAsync(stoppingToken);
            }
            finally
            {
                _consumer.Close();
                _logger.LogInformation("Kafka consumer closed");
            }
        }

        private async Task RunConsumerLoopAsync(CancellationToken cancellationToken)
        {
            var batch = new List<ConsumeResult<string, byte[]>>();
            var lastBatchTime = DateTime.UtcNow;
            var batchTimeout = TimeSpan.FromSeconds(_consumerOptions.BatchTimeoutSeconds);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // Poll with a short timeout to check batch conditions frequently
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

                    // Check if we should process the batch
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
                            _logger.LogDebug("Committed offset: {Offset}", batch[^1].Offset.Value);
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
                            _consumer.Commit(batch[^1]);
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

        //private async Task ProcessBatchAsync(List<ConsumeResult<string, byte[]>> batch, CancellationToken cancellationToken)
        //{
        //    if (batch.Count == 0) return;

        //    var startTime = DateTime.UtcNow;
        //    var deserializedBatch = new List<(TransactionEvent Event, ConsumeResult<string, byte[]> ConsumeResult)>();
        //    var failedMessages = new List<(TransactionEvent Event, string Error)>();
        //    //var failedMessages = new List<(ConsumeResult<string, byte[]> ConsumeResult, string Error)>();

        //    // Deserialize all messages in the batch
        //    foreach (var consumeResult in batch)
        //    {
        //        try
        //        {
        //            var transactionEvent = JsonSerializer.Deserialize<TransactionEvent>(
        //                consumeResult.Message.Value,
        //                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        //            if (transactionEvent == null)
        //            {
        //                failedMessages.Add((consumeResult, "Deserialization returned null"));
        //                continue;
        //            }

        //            deserializedBatch.Add((transactionEvent, consumeResult));
        //        }
        //        catch (JsonException ex)
        //        {
        //            _logger.LogError(ex, "JSON deserialization error for offset: {Offset}", consumeResult.Offset.Value);
        //            failedMessages.Add((consumeResult, $"JSON error: {ex.Message}"));
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Unexpected deserialization error for offset: {Offset}", consumeResult.Offset.Value);
        //            failedMessages.Add((consumeResult, $"Deserialization error: {ex.Message}"));
        //        }
        //    }

        //    // Process the batch
        //    if (deserializedBatch.Count > 0)
        //    {
        //        try
        //        {
        //            bool success = await _eventHandler.HandleBatchAsync(deserializedBatch, cancellationToken);

        //            var processingTime = (DateTime.UtcNow - startTime).TotalMilliseconds;

        //            if (success)
        //            {
        //                _logger.LogInformation(
        //                    "Successfully processed batch of {Count} messages in {ProcessingTime}ms ({Throughput} msg/sec)",
        //                    deserializedBatch.Count,
        //                    processingTime,
        //                    Math.Round(deserializedBatch.Count / (processingTime / 1000), 2));
        //            }
        //            else
        //            {
        //                _logger.LogWarning("Batch processing failed, sending all messages to DLT");

        //                // Send all messages in batch to dead letter
        //                foreach (var itm in deserializedBatch)
        //                {
        //                    await SendToDeadLetterAsync(itm.Event, "Batch handler returned false");
        //                }
        //            }
        //        }
        //        catch (Exception ex)
        //        {
        //            _logger.LogError(ex, "Error processing batch of {Count} messages", deserializedBatch.Count);

        //            // Send all messages in batch to dead letter on exception
        //            foreach (var itm in deserializedBatch)
        //            {
        //                await SendToDeadLetterAsync(itm.Event, $"Batch processing error: {ex.Message}");
        //            }
        //        }
        //    }

        //    // Handle failed deserializations
        //    foreach (var itm in failedMessages)
        //    {
        //        await SendToDeadLetterAsync(itm., itm.Error);
        //    }
        //}

        private async Task ProcessBatchAsync(List<ConsumeResult<string, byte[]>> batch, CancellationToken cancellationToken)
        {
            if (batch.Count == 0) return;

            var startTime = DateTime.UtcNow;
            var deserializedBatch = new List<(TransactionEvent Event, ConsumeResult<string, byte[]> ConsumeResult)>();

            // Deserialize all messages in the batch
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
                _logger.LogError(ex, "Error processing batch of {Count} messages", deserializedBatch.Count);
                foreach (var itm in deserializedBatch)
                    await SendToDeadLetterAsync(itm.Event, $"Batch processing error: {ex.Message}");
            }
        }

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