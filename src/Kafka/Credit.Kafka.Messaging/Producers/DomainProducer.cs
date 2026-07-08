using Confluent.Kafka;
using Credit.Kafka.Messaging.Config;
using Credit.Kafka.Messaging.Contracts;
using Credit.Kafka.Messaging.Handlers;
using Credit.Kafka.Messaging.Helpers;
using Credit.Kafka.Messaging.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;

namespace Credit.Kafka.Messaging.Producers;

public class DomainProducer : IDomainProducer, IDisposable
{
    private readonly IProducer<byte[], byte[]> _producer;
    private readonly string _appName;
    private readonly ILogger<DomainProducer> _logger;
    private readonly string _producerId;

    public DomainProducer(IKafkaAuthHandler kafkaAuthHandler, IOptions<BrokerOptions> brokerOptions, IOptions<ProducerOptions> producerOptions, ILogger<DomainProducer> logger)
    {
        _logger = logger;
        _producerId = "default";
        var config = new ProducerConfig
        {
            BootstrapServers = brokerOptions.Value.BootstrapServers,
            SaslUsername = brokerOptions.Value.SaslUserName,
            SaslPassword = brokerOptions.Value.SaslPassword,
            SaslMechanism = brokerOptions.Value.SaslMechanism,
            SecurityProtocol = brokerOptions.Value.SecurityProtocol,
            CompressionType = producerOptions.Value.CompressionType,
            EnableIdempotence = true,
            MessageTimeoutMs = producerOptions.Value.MessageTimeoutMs,
            LingerMs = producerOptions.Value.LingerMs,
            SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None  // Required for self-signed cert
        };

        _appName = producerOptions.Value.ProducingApplicationName;

        var builder = new ProducerBuilder<byte[], byte[]>(config)
            .SetLogHandler((producer, message) => KafkaLogHandler.LogMessage(producer, message, _logger))
            .SetErrorHandler((producer, error) => KafkaErrorHandler.HandleError(producer, error, _logger));

        if (brokerOptions.Value.SaslMechanism == SaslMechanism.OAuthBearer)
        {
            if (!string.IsNullOrEmpty(brokerOptions.Value.IamRoleArn))
            {
                var roleArn = brokerOptions.Value.IamRoleArn;
                builder.SetOAuthBearerTokenRefreshHandler((client, cfg) =>
                    kafkaAuthHandler.OauthCallbackProducerWithRole(client, cfg, roleArn));
            }
            else
            {
                builder.SetOAuthBearerTokenRefreshHandler(kafkaAuthHandler.OauthCallbackProducer);
            }
        }

        _producer = builder.Build();
    }

    public string ProducerId => _producerId;

    public IProducer<byte[], byte[]> KafkaProducer => _producer;

    public async Task<bool> ProduceAsync<T>(string topic, string key, T eventMessage, Dictionary<string, string>? additionalHeaders = null) where T : DomainEvent
    {
        var headers = BuildHeaders(eventMessage, additionalHeaders);

        byte[] payload = Serialize(eventMessage);

        try
        {
            var deliveryReport = await _producer.ProduceAsync(topic,
                new Message<byte[], byte[]> { Key = Encoding.UTF8.GetBytes(key), Value = payload, Headers = headers });

            if (deliveryReport.Status == PersistenceStatus.Persisted)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Produced event {EventType} to topic: {Topic}, key: {Key}, offset: {Offset}, eventId: {EventId}, correlationId: {CorrelationId}",
                        eventMessage.Metadata.Type,
                        deliveryReport.Topic,
                        deliveryReport.Key,
                        deliveryReport.Offset,
                        eventMessage.EventId,
                        eventMessage.Metadata.CorrelationId);
                return true;
            }

            _logger.LogError(
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                deliveryReport.Topic,
                deliveryReport.Key,
                deliveryReport.Offset,
                eventMessage.Metadata.CorrelationId,
                eventMessage.Metadata.Type);
            return false;
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(produceException,
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                produceException.DeliveryResult.Topic,
                produceException.DeliveryResult.Key,
                produceException.DeliveryResult.Offset,
                eventMessage.Metadata.CorrelationId,
                eventMessage.Metadata.Type);
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred.");
            return false;
        }
    }
    public async Task<bool> ProduceToDltAsync(string topic, Message<byte[], byte[]> eventMessage)
    {
        try
        {
            var deliveryReport = await _producer.ProduceAsync(topic, eventMessage);

            if (deliveryReport.Status == PersistenceStatus.Persisted)
            {
                if (_logger.IsEnabled(LogLevel.Debug))
                    _logger.LogDebug("Produced event to topic: {Topic}, key: {Key}, offset: {Offset}",
                        deliveryReport.Topic,
                        deliveryReport.Key,
                        deliveryReport.Offset);
                return true;
            }

            _logger.LogError(
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}",
                deliveryReport.Topic,
                deliveryReport.Key,
                deliveryReport.Offset);
            return false;
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(produceException,
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}",
                produceException.DeliveryResult.Topic,
                produceException.DeliveryResult.Key,
                produceException.DeliveryResult.Offset);
            return false;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred.");
            return false;
        }
    }

    private void Produce(string topic, string key, DomainEvent eventMessage, Dictionary<string, string>? additionalHeaders = null, Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null)
    {
        var headers = BuildHeaders(eventMessage, additionalHeaders);

        byte[] payload = Serialize(eventMessage);

        try
        {
            _producer.Produce(topic,
                new Message<byte[], byte[]> { Key = Encoding.UTF8.GetBytes(key), Value = payload, Headers = headers },
                deliveryHandler);
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(produceException,
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                produceException.DeliveryResult.Topic,
                produceException.DeliveryResult.Key,
                produceException.DeliveryResult.Offset,
                eventMessage.Metadata.CorrelationId,
                eventMessage.Metadata.Type);

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred.");
            throw;
        }
    }

    public void Produce<T>(string topic, string key, T eventMessage, Dictionary<string, string>? additionalHeaders = null, Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null) where T : DomainEvent
    {
        var headers = BuildHeaders(eventMessage, additionalHeaders);

        byte[] payload = Serialize(eventMessage);

        try
        {
            _producer.Produce(topic,
                new Message<byte[], byte[]> { Key = Encoding.UTF8.GetBytes(key), Value = payload, Headers = headers },
                deliveryHandler);
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(produceException,
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                produceException.DeliveryResult.Topic,
                produceException.DeliveryResult.Key,
                produceException.DeliveryResult.Offset,
                eventMessage.Metadata.CorrelationId,
                eventMessage.Metadata.Type);

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred.");
            throw;
        }
    }

    public void ProduceToDlt(string topic, Message<byte[], byte[]> eventMessage, Action<DeliveryReport<byte[], byte[]>>? deliveryHandler = null)
    {
        try
        {
            _producer.Produce(topic,
                eventMessage,
                deliveryHandler);
        }
        catch (ProduceException<string, string> produceException)
        {
            _logger.LogError(produceException,
                "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}",
                produceException.DeliveryResult.Topic,
                produceException.DeliveryResult.Key,
                produceException.DeliveryResult.Offset);

            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled error occurred.");
            throw;
        }
    }

    public bool ProduceBatch(Dictionary<string, List<ProduceDomainEventMessage<DomainEvent>>> topicEvents)
    {
        bool success = true;

        foreach (var (topic, events) in topicEvents)
        {
            foreach (var domainEvent in events)
            {
                Produce(
                topic,
                domainEvent.Key,
                domainEvent.Message,
                domainEvent.AdditionalHeaders,
                (deliveryReport) =>
                {
                    if (deliveryReport.Status == PersistenceStatus.Persisted)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Produced event {EventType} to topic: {Topic}, key: {Key}, offset: {Offset}, eventId: {EventId}, correlationId: {CorrelationId}",
                                domainEvent.Message.Metadata.Type,
                                deliveryReport.Topic,
                                deliveryReport.Key,
                                deliveryReport.Offset,
                                domainEvent.Message.EventId,
                                domainEvent.Message.Metadata.CorrelationId);
                        return;
                    }

                    success = false;
                    _logger.LogError(
                        "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                        deliveryReport.Topic,
                        deliveryReport.Key,
                        deliveryReport.Offset,
                        domainEvent.Message.Metadata.CorrelationId,
                        domainEvent.Message.Metadata.Type);
                });
            }
        }

        _producer.Flush();

        if (success)
        {
            _logger.LogInformation("Produced batch {Batch}",
                topicEvents.Select(x => new { Topic = x.Key, Events = x.Value.Count }).ToList());
        }

        return success;
    }

    public bool ProduceBatch(string topic, List<ProduceDomainEventMessage<DomainEvent>> eventMessages)
    {
        bool success = true;

        foreach (var domainEvent in eventMessages)
        {
            Produce(
                topic,
                domainEvent.Key,
                domainEvent.Message,
                domainEvent.AdditionalHeaders,
                (deliveryReport) =>
                {
                    if (deliveryReport.Status == PersistenceStatus.Persisted)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Produced event {EventType} to topic: {Topic}, key: {Key}, offset: {Offset}, eventId: {EventId}, correlationId: {CorrelationId}",
                                domainEvent.Message.Metadata.Type,
                                deliveryReport.Topic,
                                deliveryReport.Key,
                                deliveryReport.Offset,
                                domainEvent.Message.EventId,
                                domainEvent.Message.Metadata.CorrelationId);
                        return;
                    }

                    success = false;
                    _logger.LogError(
                        "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                        deliveryReport.Topic,
                        deliveryReport.Key,
                        deliveryReport.Offset,
                        domainEvent.Message.Metadata.CorrelationId,
                        domainEvent.Message.Metadata.Type);
                });
        }

        _producer.Flush();

        if (success)
        {
            _logger.LogInformation("Produced batch of {BatchSize} events to {Topic}",
                eventMessages.Count,
                topic);
        }

        return success;
    }

    public bool ProduceBatch<T>(string topic, List<ProduceDomainEventMessage<T>> eventMessages) where T : DomainEvent
    {
        bool success = true;

        foreach (var domainEvent in eventMessages)
        {
            Produce(
                topic,
                domainEvent.Key,
                domainEvent.Message,
                domainEvent.AdditionalHeaders,
                (deliveryReport) =>
                {
                    if (deliveryReport.Status == PersistenceStatus.Persisted)
                    {
                        if (_logger.IsEnabled(LogLevel.Debug))
                            _logger.LogDebug("Produced event {EventType} to topic: {Topic}, key: {Key}, offset: {Offset}, eventId: {EventId}, correlationId: {CorrelationId}",
                                domainEvent.Message.Metadata.Type,
                                deliveryReport.Topic,
                                deliveryReport.Key,
                                deliveryReport.Offset,
                                domainEvent.Message.EventId,
                                domainEvent.Message.Metadata.CorrelationId);
                        return;
                    }

                    success = false;
                    _logger.LogError(
                        "Failed to deliver message to topic: {Topic}, key: {Key} with offset: {Offset}, correlationId: {CorrelationId}, eventType: {EventType} ",
                        deliveryReport.Topic,
                        deliveryReport.Key,
                        deliveryReport.Offset,
                        domainEvent.Message.Metadata.CorrelationId,
                        domainEvent.Message.Metadata.Type);
                });
        }

        _producer.Flush();

        if (success)
        {
            _logger.LogInformation("Produced batch of {BatchSize} events to {Topic}",
                eventMessages.Count,
                topic);
        }

        return success;
    }

    public void Flush(TimeSpan? timeout = null)
    {
        if (timeout != null)
        {
            _producer.Flush(timeout.Value);
            return;
        }

        _producer.Flush();
    }

    private Headers BuildHeaders<T>(T domainEvent, Dictionary<string, string>? additionalHeaders = null) where T : DomainEvent

    {
        if (domainEvent.Metadata.CorrelationId == Guid.Empty)
        {
            throw new ValidationException($"Kafka header {nameof(domainEvent.Metadata.CorrelationId)} is required");
        }

        var headers = new Headers
        {
            { DomainEventHeaders.PublishedBy, Encoding.ASCII.GetBytes(_appName) },
            { DomainEventHeaders.CorrelationId, Encoding.ASCII.GetBytes(domainEvent.Metadata.CorrelationId.ToString()) },
            { DomainEventHeaders.PublishedAt,Encoding.ASCII.GetBytes(domainEvent.Metadata.PublishedAt.ToString("O")) },
            { DomainEventHeaders.EventType, Encoding.ASCII.GetBytes(domainEvent.Metadata.Type) },
            { DomainEventHeaders.Id, Encoding.ASCII.GetBytes(domainEvent.EventId.ToString()) },
            { DomainEventHeaders.Version, Encoding.ASCII.GetBytes(domainEvent.Metadata.Version) },
            { DomainEventHeaders.ContentType, "application/json"u8.ToArray() }
        };

        if (additionalHeaders == null)
            return headers;

        foreach (var (key, value) in additionalHeaders)
        {
            headers.Add(key, Encoding.ASCII.GetBytes(value));
        }

        return headers;
    }

    private static byte[] Serialize(DomainEvent eventMessage)
    {
        return JsonSerializer.SerializeToUtf8Bytes(eventMessage, eventMessage.GetType());
    }

    private static byte[] Serialize<T>(T eventMessage) where T : DomainEvent
    {
        return JsonSerializer.SerializeToUtf8Bytes(eventMessage);
    }

    public void Dispose()
    {
        _producer.Flush();
        _producer.Dispose();
    }
}