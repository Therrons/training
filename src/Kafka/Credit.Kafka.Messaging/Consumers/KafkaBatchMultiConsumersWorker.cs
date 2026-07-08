using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Consumers
{
    public class KafkaBatchMultiConsumersWorker
    {
        private readonly ILogger<KafkaBatchMultiConsumersWorker> _logger;
        private readonly BatchConsumerOptions _batchConsumerOptions;
        private readonly BrokerOptions _brokerOptions;
        private IConsumer<byte[], byte[]>? _consumer;
        private readonly IKafkaAuthHandler _kafkaAuthHandler;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly List<ConsumeResult<byte[], byte[]>> _batch = [];
        // legacy static pause flag (kept for compatibility)
        //private bool _pauseJob = false;

        // instance-level pause and per-topic map
        private bool _instancePaused = false;

        private readonly List<string> _instanceTopics = [];

        private readonly string _workerName;
        internal string Id { get; }

        public KafkaBatchMultiConsumersWorker(string id,
        ILogger<KafkaBatchMultiConsumersWorker> logger,
        BatchConsumerOptions batchConsumerOptions,
        BrokerOptions brokerOptions,
        IServiceScopeFactory serviceScopeFactory,
        IKafkaAuthHandler kafkaAuthHandler,
        string workerName)
        {
            Id = id;
            _logger = logger;
            _batchConsumerOptions = batchConsumerOptions;
            _brokerOptions = brokerOptions;
            _serviceScopeFactory = serviceScopeFactory;
            _kafkaAuthHandler = kafkaAuthHandler;
            _workerName = workerName;

            // instance topics should be initialized from options/topics when the worker starts consumer
            if (_batchConsumerOptions?.MessageHandlers?.Keys != null)
            {
                _instanceTopics = _batchConsumerOptions.MessageHandlers.Keys
                    .Where(kv => !string.IsNullOrWhiteSpace(kv))
                    .Select(t => t)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
        }

        public async Task StartAsync(CancellationToken stoppingToken)
        {
            var builder = new ConsumerBuilder<byte[], byte[]>(new ConsumerConfig
            {
                BootstrapServers = _brokerOptions.BootstrapServers,
                SaslUsername = _brokerOptions.SaslUserName,
                SaslPassword = _brokerOptions.SaslPassword,
                SaslMechanism = _brokerOptions.SaslMechanism,
                SecurityProtocol = _brokerOptions.SecurityProtocol,
                GroupId = _batchConsumerOptions.GroupId,
                AutoOffsetReset = _batchConsumerOptions.AutoOffsetReset,
                PartitionAssignmentStrategy = _batchConsumerOptions.PartitionAssignmentStrategy,
                AllowAutoCreateTopics = _batchConsumerOptions.AllowAutoCreateTopics,
                MaxPollIntervalMs = _batchConsumerOptions.MaxPollIntervalMs,
                SessionTimeoutMs = _batchConsumerOptions.SessionTimeoutMs,
                EnableAutoCommit = false,
                EnableAutoOffsetStore = false,
                EnablePartitionEof = true,
                ClientId = Id
            })
                .SetLogHandler((consumer, message) => KafkaLogHandler.LogMessage(consumer, message, _logger))
                .SetErrorHandler((consumer, error) => KafkaErrorHandler.HandleError(consumer, error, _logger))
                .SetPartitionsRevokedHandler((c, partitions) =>
                {
                    var remaining =
                        c.Assignment.Where(topicPartition => partitions.All(offset => offset.TopicPartition != topicPartition));
                    _batch.Clear();
                })
                .SetPartitionsLostHandler((c, partitions) =>
                {
                    _batch.Clear();
                })
                .SetPartitionsAssignedHandler((c, partitions) =>
                {
                    //_logger.LogInformation(
                    //    "ConsumerConfiguration group additional partitions assigned: [{AdditionalPartitons}] all partitions: [{AllPartitions}]",
                    //    string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")),
                    //    string.Join(',', c.Assignment.Concat(partitions).Select(p => $"{p.Topic} {p.Partition.Value}")));
                });

            if (_brokerOptions.SaslMechanism == SaslMechanism.OAuthBearer)
            {
                if (!string.IsNullOrEmpty(_brokerOptions.IamRoleArn))
                {
                    builder.SetOAuthBearerTokenRefreshHandler((c, cfg) =>
                        _kafkaAuthHandler.OauthCallbackConsumerWithRole(c, cfg, _brokerOptions.IamRoleArn));
                }
                else
                {
                    builder.SetOAuthBearerTokenRefreshHandler((c, cfg) => _kafkaAuthHandler.OauthCallbackConsumer(c, cfg));
                }
            }
            _consumer = builder.Build();
            await Task.Run(async () => await Consume(stoppingToken)).ConfigureAwait(false);
        }

        private async Task Consume(CancellationToken stoppingToken)
        {
            _logger.LogInformation("{threadid}: thread id - Subscribed worker topics {topics}", GetCurrentThreadId(), _instanceTopics);
            _consumer!.Subscribe(_instanceTopics);

            string? currentTopic = null;
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(50, stoppingToken).ConfigureAwait(false);

                    bool bContinue = !(Volatile.Read(ref _instancePaused));
                    // in Consume loop: read the latest value
                    if (bContinue)
                    {
                        var result = _consumer.Consume(TimeSpan.FromSeconds(5));

                        if (result is null || result.Message is null && !result.IsPartitionEOF)
                            continue;

                        currentTopic ??= result.Topic;

                        if (ShouldProcessBatch(result, currentTopic, _batch))
                        {
                            await ProcessBatch(_batch, currentTopic, stoppingToken).ConfigureAwait(false);
                            CommitOffsetsForBatch(currentTopic, _batch);
                            _batch.Clear();
                            currentTopic = result.Topic;

                            if (!ShouldAddCurrentMessageToNextBatch(result))
                            {
                                continue;
                            }
                        }
                        _batch.Add(result);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Operation canceled, closing consumer.");
                    break;
                }
                catch (ConsumeException cex)
                {
                    _logger.LogError(cex, "Error occurred while consuming message: {Error}", cex.Error.Reason);
                    HandleConsumeException(_batch);
                }
                catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_State)
                {
                    _logger.LogError(ex, "The Kafka consumer is in an invalid state for the operation being attempted");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unhandled exception occurred");
                    HandleConsumeException(_batch);
                }
            }

            try
            {
                _logger.LogInformation("Closing Consumer");
                _consumer.Close();
                _consumer.Dispose();
            }
            catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_State)
            {
                _logger.LogError($"Attempted to close Kafka consumer, but it was not in a valid state: {ex.Message}");
            }
        }

        public void PauseConsumer()
        {
            Volatile.Write(ref _instancePaused, false);
        }

        // explicit instance-level pause/resume
        //public void PauseConsumer(bool pause)
        //{
        //    //if (_logger.IsEnabled(LogLevel.Information))
        //    //{
        //    //    _logger.LogInformation($"Worker topics: {string.Join(',', _instanceTopics)} set instance pause - {pause}");
        //    //}
        //    Volatile.Write(ref _instancePaused, pause);
        //}

        // per-topic configuration: dictionary expected as topic -> enabled (true = enabled, false = disabled)
        public void PauseConsumer(Dictionary<string, bool> newConfig)
        {
            if (newConfig == null || !newConfig.Any()) return;

            // check which 
            var newConfigMatched = newConfig.Where(kv => _instanceTopics.Contains(kv.Key.Trim().ToLower()))
                                   .Select(kv => kv.Value)
                                   .ToList();

            _logger.LogInformation("\r\n\r\n\r\n\r\nThe new config settings for worker {workerName} is {configsettings}\r\n", _workerName, newConfig);

            if (newConfigMatched.Any())
            {
                bool enable = newConfigMatched.All(v => v);
                Volatile.Write(ref _instancePaused, !enable);
                //if (_logger.IsEnabled(LogLevel.Information))
                //{
                //    _logger.LogInformation($"Worker {GetCurrentThreadId()} instance paused set to {!enable} based on topic-level toggles");
                //}
            }
            _logger.LogInformation("The status for worker {workername} is now {workerpaused}\r\n\r\n\r\n\r\n\r\n", _workerName, _instancePaused == true ? "PAUSED" : "RUNNING");
        }

        private bool ShouldProcessBatch(ConsumeResult<byte[], byte[]> result, string? currentTopic, List<ConsumeResult<byte[], byte[]>> batch)
        {
            return batch.Count >= _batchConsumerOptions.BatchSize || result.IsPartitionEOF || result.Topic != currentTopic;
        }

        private bool ShouldAddCurrentMessageToNextBatch(ConsumeResult<byte[], byte[]> result)
        {
            if (!result.IsPartitionEOF)
                return true;

            _logger.LogInformation("Reached end of topic {Topic}, partition {Partition}, offset {Offset}.",
                result.Topic, result.Partition, result.Offset);
            return false;
        }

        private void CommitOffsetsForBatch(string? topic, List<ConsumeResult<byte[], byte[]>> batch)
        {
            var commitOffsets = new List<TopicPartitionOffset>();
            foreach (var message in batch.Where(message => message.Topic == topic))
            {
                var tpo = new TopicPartitionOffset(topic, message.TopicPartitionOffset.Partition, message.TopicPartitionOffset.Offset + 1);
                commitOffsets.Add(tpo);
                _consumer!.StoreOffset(message);
            }

            try
            {
                if (commitOffsets.Count == 0)
                    return;
                _consumer!.Commit();
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Committed offsets for topic {Topic}: {Offsets}", topic, string.Join(", ", commitOffsets));

                _logger.LogInformation("Committed {OffsetAmount} offsets for topic {Topic}", commitOffsets.Count, topic);
            }
            catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_NoOffset)
            {
                _logger.LogInformation("No Offsets to store on commit");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error Occured while committing offsets");
                throw;
            }
        }

        private async Task ProcessBatch(List<ConsumeResult<byte[], byte[]>> batch, string? topic, CancellationToken stoppingToken)
        {
            if (batch.Count == 0)
                return;

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug("Processing batch of {BatchSize} messages", batch.Count);
                _logger.LogDebug("Messages: {Messages}", string.Join(", ", batch.Select(x => x.TopicPartitionOffset)));
            }

            if (string.IsNullOrEmpty(topic) || !_batchConsumerOptions.MessageHandlers.TryGetValue(topic, out var handlerType))
            {
                _logger.LogError("No handler found for topic {Topic}, skipping batch", topic);
                throw new ApplicationException($"No handler found for topic {topic}");
            }

            using var scope = _serviceScopeFactory.CreateScope();
            object handler = scope.ServiceProvider.GetRequiredService(handlerType);

            if (!typeof(IBatchMessageHandler).IsAssignableFrom(handlerType))
            {
                _logger.LogError("BatchHandler for topic {Topic} does not implement {Handler}", topic, nameof(IBatchMessageHandler));
                throw new ApplicationException($"BatchHandler for topic {topic} does not implement {nameof(IBatchMessageHandler)}");
            }

            var batchHandler = handler as IBatchMessageHandler;

            await batchHandler!.HandleBatchMessagesAsync(batch, stoppingToken);
        }

        private void HandleConsumeException(List<ConsumeResult<byte[], byte[]>> batch)
        {
            var lowestOffsets = new Dictionary<TopicPartition, Offset>();

            foreach (var message in batch)
            {
                var topicPartition = message.TopicPartitionOffset.TopicPartition;
                var offset = message.TopicPartitionOffset.Offset;

                if (lowestOffsets.TryGetValue(topicPartition, out var value) && offset >= value)
                    continue;

                value = offset;
                lowestOffsets[topicPartition] = value;
            }

            foreach (var kvp in lowestOffsets)
            {
                _consumer!.Seek(new TopicPartitionOffset(kvp.Key, kvp.Value));

                _logger.LogInformation("Seeking back to Topic: {Topic} Partition: {Partition} Offset: {Offset}",
                    kvp.Key.Topic, kvp.Key.Partition, kvp.Value);
            }

            batch.Clear();
        }

        //private void HandleConsumeException(List<ConsumeResult<byte[], byte[]>> batch)
        //{
        //    var lowestOffsets = new Dictionary<TopicPartition, Offset>();

        //    foreach (var message in batch)
        //    {
        //        var topicPartition = message.TopicPartitionOffset.TopicPartition;
        //        var offset = message.TopicPartitionOffset.Offset;

        //        if (lowestOffsets.TryGetValue(topicPartition, out var value) && offset >= value)
        //            continue;

        //        value = offset;
        //        lowestOffsets[topicPartition] = value;
        //    }

        //    foreach (var kvp in lowestOffsets)
        //    {
        //        _consumer!.Seek(new TopicPartitionOffset(kvp.Key, kvp.Value));

        //        _logger.LogInformation("Seeking back to Topic: {Topic} Partition: {Partition} Offset: {Offset}",
        //            kvp.Key.Topic, kvp.Key.Partition, kvp.Value);
        //    }

        //    batch.Clear();
        //}

        private static int GetCurrentThreadId()
        {
            return Thread.CurrentThread.ManagedThreadId;
        }
    }
}