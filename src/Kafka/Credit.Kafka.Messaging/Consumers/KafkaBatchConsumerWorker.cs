using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Consumers;

public class KafkaBatchConsumerWorker
{
    private readonly ILogger<KafkaBatchConsumerWorker> _logger;
    private readonly BatchConsumerOptions _batchConsumerOptions;
    private readonly BrokerOptions _brokerOptions;
    private IConsumer<byte[], byte[]>? _consumer;
    private readonly IKafkaAuthHandler _kafkaAuthHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly List<ConsumeResult<byte[], byte[]>> _batch = [];
    internal string Id { get; }

    public KafkaBatchConsumerWorker() { }

    public KafkaBatchConsumerWorker(string id, ILogger<KafkaBatchConsumerWorker> logger, BatchConsumerOptions batchConsumerOptions, BrokerOptions brokerOptions, IServiceScopeFactory serviceScopeFactory, IKafkaAuthHandler kafkaAuthHandler)
    {
        _logger = logger;
        _batchConsumerOptions = batchConsumerOptions;
        _brokerOptions = brokerOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaAuthHandler = kafkaAuthHandler;
        Id = id;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting KafkaBatchConsumerWorker with ID: {WorkerId}", Id);
        }

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

                _logger.LogInformation(
                    "ConsumerConfiguration group partitions revoked: [{RevokedPartitions}], remaining:[{RemainingPartitions}]",
                    string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")),
                    string.Join(',', remaining.Select(p => $"{p.Topic} {p.Partition.Value}")));

                _batch.Clear();
            })
            .SetPartitionsLostHandler((c, partitions) =>
            {
                _logger.LogInformation(
                    "ConsumerConfiguration group partitions lost: [{PartitionsLost}]",
                    string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")));

                _batch.Clear();
            })
            .SetPartitionsAssignedHandler((c, partitions) =>
            {
                _logger.LogInformation(
                    "ConsumerConfiguration group additional partitions assigned: [{AdditionalPartitons}] all partitions: [{AllPartitions}]",
                    string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")),
                    string.Join(',', c.Assignment.Concat(partitions).Select(p => $"{p.Topic} {p.Partition.Value}")));
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

        await Task.Run(() => Consume(stoppingToken), stoppingToken).ConfigureAwait(false);
    }

    private async Task Consume(CancellationToken stoppingToken)
    {
        var topics = _batchConsumerOptions.MessageHandlers.Select(x => x.Key).ToList();
        _consumer!.Subscribe(topics);
        _logger.LogInformation("Subscribing to topics: {Topics}", string.Join(',', topics));

        string? currentTopic = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = _consumer.Consume(TimeSpan.FromSeconds(5));

                if (result is null || result.Message is null && !result.IsPartitionEOF)
                    continue;

                currentTopic ??= result.Topic;

                if (ShouldProcessBatch(result, currentTopic, _batch))
                {
                    await ProcessBatch(_batch, currentTopic, stoppingToken);
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
}