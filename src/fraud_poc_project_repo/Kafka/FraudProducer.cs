using Confluent.Kafka;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace fraud_poc_project_repo.Kafka
{
    public class FraudProducer : IFraudProducer
    {
        private readonly IProducer<string, byte[]> _producer;
        private readonly FraudKafkaProducerSettings _producerOptions;
        private readonly AppSettings _appSettings;
        private readonly ILogger<FraudProducer> _logger;
        private bool _disposed;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public FraudProducer(
            IOptions<FraudKafkaBrokerSettings> brokerOptions,
            IOptions<FraudKafkaProducerSettings> producerOptions,
            IOptions<AppSettings> appSettings,
            ILogger<FraudProducer> logger)
        {
            _logger = logger;
            _appSettings = appSettings.Value;
            _producerOptions = producerOptions.Value;
            var producerConfig = new ProducerConfig
            {
                BootstrapServers = brokerOptions.Value.BootstrapServers,
                SaslUsername = brokerOptions.Value.SaslUserName,
                SaslPassword = brokerOptions.Value.SaslPassword,
                SaslMechanism = brokerOptions.Value.SaslMechanism,
                SecurityProtocol = brokerOptions.Value.SecurityProtocol,
                CompressionType = _producerOptions.CompressionType,
                EnableIdempotence = _producerOptions.EnableIdempotence,
                MessageTimeoutMs = _producerOptions.MessageTimeoutMs,
                LingerMs = _producerOptions.LingerMs,
                AllowAutoCreateTopics = brokerOptions.Value.AllowAutoCreateTopics,
                Acks = Acks.All,
                MaxInFlight = 5,
                MessageSendMaxRetries = int.MaxValue,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                ReconnectBackoffMs = _producerOptions.ReconnectBackoffMs,
                ReconnectBackoffMaxMs = _producerOptions.ReconnectBackoffMaxMs,
                ApiVersionRequestTimeoutMs = _producerOptions.ApiVersionRequestTimeoutMs
            };
            _producer = new ProducerBuilder<string, byte[]>(producerConfig)
                .SetLogHandler((_, message) => LogKafkaMessage(message))
                .SetErrorHandler((_, error) => LogKafkaError(error))
                .Build();

            _logger.LogInformation("FraudKafkaProducer initialized for application: {ApplicationName}",
                    _appSettings.ApplicationName);
        }

        public async Task<bool> ProduceAsync<T>(
            T message,
            CancellationToken cancellationToken = default) where T : TransactionEvent
        {
            string _topic = message.KafkaTopic;
            string _key = message.CustomerId;
            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

                var kafkaMessage = new Message<string, byte[]>
                {
                    Key = _key,
                    Value = payload,
                    Timestamp = new Timestamp(message.TransactionTime)
                };

                var deliveryResult = await _producer.ProduceAsync(_topic, kafkaMessage, cancellationToken);

                if (deliveryResult.Status == PersistenceStatus.Persisted)
                {
                    _logger.LogDebug(
                        "Successfully produced message to topic: {Topic}, partition: {Partition}, offset: {Offset}, correlationId: {CorrelationId}",
                        _topic,
                        deliveryResult.Partition.Value,
                        deliveryResult.Offset.Value,
                        message.CorrelationId);
                    return true;
                }

                _logger.LogError(
                    "Failed to persist message to topic: {Topic}, status: {Status}, correlationId: {CorrelationId}",
                    _topic,
                    deliveryResult.Status,
                    message.CorrelationId);
                return false;
            }
            catch (ProduceException<string, byte[]> ex)
            {
                _logger.LogError(ex,
                    "Kafka produce exception for topic: {Topic}, key: {Key}, correlationId: {CorrelationId}, error: {Error}",
                    _topic,
                    _key,
                    message.CorrelationId,
                    ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception producing message to topic: {Topic}, correlationId: {CorrelationId}",
                     _topic,
                    message.CorrelationId);
                return false;
            }
        }

        public bool Produce<T>(T message) where T : TransactionEvent
        {
            string _topic = message.KafkaTopic;
            string _key = message.CustomerId;

            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

                var kafkaMessage = new Message<string, byte[]>
                {
                    Key = _key,
                    Value = payload,
                    Timestamp = new Timestamp(message.TransactionTime)
                };

                _producer.Produce(message.KafkaTopic, kafkaMessage);
                return true;
            }
            catch (ProduceException<string, byte[]> ex)
            {
                _logger.LogError(ex,
                    "Kafka produce exception for topic: {Topic}, key: {Key}, correlationId: {CorrelationId}, error: {Error}",
                    _topic,
                    _key,
                    message.CorrelationId,
                    ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception producing message to topic: {Topic}, correlationId: {CorrelationId}",
                    _key,
                    message.CorrelationId);
                return false;
            }
        }

        public bool ProduceDlt<T>(T message) where T : TransactionEvent
        {
            try
            {
                var payload = JsonSerializer.SerializeToUtf8Bytes(message, _jsonOptions);

                var kafkaMessage = new Message<string, byte[]>
                {
                    Key = message.CustomerId,
                    Value = payload,
                    Timestamp = new Timestamp(message.TransactionTime)
                };

                _producer.Produce(message.KafkaTopic, kafkaMessage);
                return true;
            }
            catch (ProduceException<string, byte[]> ex)
            {
                _logger.LogError(ex,
                    "Kafka produce exception for topic: {Topic}, key: {Key}, correlationId: {CorrelationId}, error: {Error}",
                    message.KafkaTopic,
                    message.CustomerId,
                    message.CorrelationId,
                    ex.Error.Reason);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception producing message to topic: {Topic}, correlationId: {CorrelationId}",
                     message.KafkaTopic,
                    message.CorrelationId);
                return false;
            }
        }

        public async Task<bool> ProduceDltAsync<T>(T message, CancellationToken cancellationToken = default) where T : TransactionEvent
        {
            string _topic = message.KafkaTopic;
            if (string.IsNullOrEmpty(_topic))
            {
                _logger.LogError("DeadLetterTopic is not configured");
                return false;
            }
            return await ProduceAsync(message, cancellationToken);
        }

        public void Flush(TimeSpan? timeout = null)
        {
            try
            {
                _producer.Flush(timeout ?? TimeSpan.FromSeconds(30));
                _logger.LogDebug("Producer flushed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error flushing producer");
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

        public void Dispose()
        {
            if (_disposed) return;

            try
            {
                Flush(TimeSpan.FromSeconds(10));
                _producer?.Dispose();
                _logger.LogInformation("FraudKafkaProducer disposed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing FraudKafkaProducer");
            }

            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}