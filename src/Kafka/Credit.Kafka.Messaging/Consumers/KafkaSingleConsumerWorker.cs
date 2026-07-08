using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Contracts;
using Credit.Kafka.Messaging.Extensions;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Consumers;

public class KafkaSingleConsumerWorker
{
    private readonly ILogger<KafkaSingleConsumerWorker> _logger;
    private readonly ConsumerOptions _consumerOptions;
    private readonly BrokerOptions _brokerOptions;
    private IConsumer<byte[], byte[]>? _consumer;
    private readonly IKafkaAuthHandler _kafkaAuthHandler;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    internal string Id { get; }

    public KafkaSingleConsumerWorker(string id, ILogger<KafkaSingleConsumerWorker> logger, ConsumerOptions consumerOptions, BrokerOptions brokerOptions, IServiceScopeFactory serviceScopeFactory, IKafkaAuthHandler kafkaAuthHandler)
    {
        Id = id;
        _logger = logger;
        _consumerOptions = consumerOptions;
        _brokerOptions = brokerOptions;
        _serviceScopeFactory = serviceScopeFactory;
        _kafkaAuthHandler = kafkaAuthHandler;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Starting KafkaSingleConsumerWorker with ID {WorkerId}", Id);
        }

        var builder = new ConsumerBuilder<byte[], byte[]>(new ConsumerConfig
        {
            BootstrapServers = _brokerOptions.BootstrapServers,
            SaslUsername = _brokerOptions.SaslUserName,
            SaslPassword = _brokerOptions.SaslPassword,
            SaslMechanism = _brokerOptions.SaslMechanism,
            SecurityProtocol = _brokerOptions.SecurityProtocol,
            GroupId = _consumerOptions.GroupId,
            AutoOffsetReset = _consumerOptions.AutoOffsetReset,
            PartitionAssignmentStrategy = _consumerOptions.PartitionAssignmentStrategy,
            AllowAutoCreateTopics = _consumerOptions.AllowAutoCreateTopics,
            ClientId = Id,
            EnableAutoCommit = true,
            EnableAutoOffsetStore = false,
            EnablePartitionEof = true
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
            })
            .SetPartitionsLostHandler((c, partitions) =>
            {
                _logger.LogInformation(
                    "ConsumerConfiguration group partitions lost: [{PartitionsLost}]",
                    string.Join(',', partitions.Select(p => $"{p.Topic} {p.Partition.Value}")));
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
        _consumer!.Subscribe(_consumerOptions.Topics);
        _logger.LogInformation("Subscribing to topics: {Topics}", string.Join(',', _consumerOptions.Topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            var result = new ConsumeResult<byte[], byte[]>();

            try
            {
                result = _consumer.Consume(stoppingToken);

                if (result is null)
                    continue;

                if (result.IsPartitionEOF)
                {
                    _logger.LogInformation("Reached end of Topic: {Topic}, Partition: {Partition}, at Offset: {Offset}",
                        result.Topic, result.Partition, result.Offset);
                    continue;
                }

                if (result.Message is null)
                {
                    continue;
                }

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Consumed message with key {MessageKey} from topic {Topic}, partition {Partition}, offset {Offset}",
                        result.Message.Key, result.Topic, result.Partition, result.Offset);
                }

                await ProcessMessage(result, stoppingToken);
                _consumer.StoreOffset(result);

                if (_logger.IsEnabled(LogLevel.Debug))
                {
                    _logger.LogDebug("Processed and stored offset for message with key {MessageKey} from topic {Topic}, partition {Partition}, offset {Offset}",
                        result.Message.Key, result.Topic, result.Partition, result.Offset);
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
                SeekToCurrentOffset(result);
            }
            catch (KafkaException ex) when (ex.Error.Code == ErrorCode.Local_State)
            {
                _logger.LogError(ex, "The Kafka consumer is in an invalid state for the operation being attempted");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception occurred");
                SeekToCurrentOffset(result);
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
            _logger.LogError("Attempted to close Kafka consumer, but it was not in a valid state: {ErrorMessage}", ex.Message);
        }

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("KafkaSingleConsumerWorker with ID {WorkerId} has stopped", Id);
        }
    }

    private async Task ProcessMessage(ConsumeResult<byte[], byte[]> result, CancellationToken stoppingToken)
    {
        if (_consumerOptions.ConsumeDomainEvents)
        {
            await HandleDomainEventMessage(result, stoppingToken);
            return;
        }

        await HandleTopicMessage(result, stoppingToken);
    }

    private async Task HandleDomainEventMessage(ConsumeResult<byte[], byte[]> result, CancellationToken stoppingToken)
    {
        if (!result.Message.TryGetDomainEventType(out string? eventType))
        {
            _logger.LogWarning("No {DomainEventType} header found in message on Topic: {Topic}, Partition: {Partition}, Offset: {Offset}",
                nameof(DomainEventHeaders.EventType), result.Topic, result.Partition, result.Offset);
            return;
        }

        if (string.IsNullOrEmpty(eventType) || !_consumerOptions.MessageHandlers.TryGetValue(eventType, out var handlerType))
        {
            _logger.LogInformation("No handler found for Event-Type: {EventType}, skipping message", eventType);

            return;
        }

        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            _logger.LogError("Handler for event {EventType} does not implement {Handler}", eventType, nameof(IMessageHandler));
            throw new ApplicationException($"Handler for topic {result.Topic} does not implement {nameof(IMessageHandler)}");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Invoking handler for event type {EventType} with message key {MessageKey}", eventType, result.Message.Key);
        }

        await handler.HandleMessageAsync(result, stoppingToken);
    }

    private async Task HandleTopicMessage(ConsumeResult<byte[], byte[]> result, CancellationToken stoppingToken)
    {
        if (!_consumerOptions.MessageHandlers.TryGetValue(result.Topic, out var handlerType))
        {
            _logger.LogError("No handler found for topic {Topic}, skipping message", result.Topic);
            throw new ApplicationException($"No handler found for topic {result.Topic}");
        }

        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            _logger.LogError("Handler for topic {Topic} does not implement {Handler}", result.Topic, nameof(IMessageHandler));
            throw new ApplicationException($"Handler for topic {result.Topic} does not implement {nameof(IMessageHandler)}");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);

        if (_logger.IsEnabled(LogLevel.Debug))
        {
            _logger.LogDebug("Invoking handler for topic {Topic} with message key {MessageKey}", result.Topic, result.Message.Key);
        }

        await handler.HandleMessageAsync(result, stoppingToken);
    }

    private IMessageHandler FetchHandler(ConsumeResult<byte[], byte[]> result, Type handlerType)
    {

        if (!typeof(IMessageHandler).IsAssignableFrom(handlerType))
        {
            _logger.LogError("Handler for topic {Topic} does not implement {Handler}", result.Topic, nameof(IMessageHandler));
            throw new ApplicationException($"Handler for topic {result.Topic} does not implement {nameof(IMessageHandler)}");
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var handler = (IMessageHandler)scope.ServiceProvider.GetRequiredService(handlerType);

        return handler;
    }

    private void SeekToCurrentOffset(ConsumeResult<byte[], byte[]> result)
    {
        if (_consumer == null) return;

        _logger.LogInformation("Seeking to current offset: {TopicPartitionOffset}", result.TopicPartitionOffset);
        _consumer.Seek(result.TopicPartitionOffset);
    }
}