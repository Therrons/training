using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Configuration
{
    public class BrokerHealthCheckResult
    {
        public bool IsHealthy { get; set; }
        public bool IdempotenceEnabled { get; set; }
        public string Message { get; set; }
    }

    public class KafkaBrokerHealthCheck
    {
        private readonly FraudKafkaBrokerSettings _brokerSettings;
        private readonly ILogger<KafkaBrokerHealthCheck> _logger;

        public KafkaBrokerHealthCheck(
            IOptions<FraudKafkaBrokerSettings> brokerSettings,
            ILogger<KafkaBrokerHealthCheck> logger)
        {
            _brokerSettings = brokerSettings.Value;
            _logger = logger;
        }

        public async Task<BrokerHealthCheckResult> WaitForBrokerReadyWithFallbackAsync(
            int maxRetries = 30,
            int delayMs = 2000,
            int coordinatorTimeoutMs = 15000)
        {
            _logger.LogInformation("Attempting to connect to Kafka broker at {BootstrapServers} with idempotence enabled...", _brokerSettings.BootstrapServers);

            var result = await TryConnectAsync(maxRetries, delayMs, coordinatorTimeoutMs);

            if (result.IsHealthy)
            {
                _logger.LogInformation("Kafka broker is ready with idempotence enabled!");
                return result;
            }

            _logger.LogWarning("Broker connection failed with idempotence enabled. Attempting fallback to non-idempotent mode...");
            result = await TryConnectAsync(maxRetries, delayMs, coordinatorTimeoutMs, enableIdempotenceTest: false);

            if (result.IsHealthy)
            {
                _logger.LogWarning("Kafka broker is ready in NON-IDEMPOTENT mode. Producer will NOT guarantee exactly-once semantics.");
                return result;
            }

            throw new InvalidOperationException(
                $"Failed to connect to Kafka broker at {_brokerSettings.BootstrapServers} after {maxRetries * 2} attempts (with and without idempotence). " +
                "Please ensure the broker is running and accessible.");
        }

        private async Task<BrokerHealthCheckResult> TryConnectAsync(
            int maxRetries,
            int delayMs,
            int coordinatorTimeoutMs,
            bool enableIdempotenceTest = true)
        {
            string mode = enableIdempotenceTest ? "idempotent" : "non-idempotent";
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using (var adminClient = new AdminClientBuilder(new AdminClientConfig
                    {
                        BootstrapServers = _brokerSettings.BootstrapServers,
                        SaslUsername = _brokerSettings.SaslUserName,
                        SaslPassword = _brokerSettings.SaslPassword,
                        SaslMechanism = _brokerSettings.SaslMechanism,
                        SecurityProtocol = _brokerSettings.SecurityProtocol,
                        SocketTimeoutMs = 5000,
                        ConnectionsMaxIdleMs = 5000
                    }).Build())
                    {
                        var metadata = adminClient.GetMetadata(TimeSpan.FromMilliseconds(coordinatorTimeoutMs));

                        return new BrokerHealthCheckResult
                        {
                            IsHealthy = true,
                            IdempotenceEnabled = enableIdempotenceTest,
                            Message = $"Found {metadata.Brokers.Count} brokers and {metadata.Topics.Count} topics with Idempotence set to '{mode}'"
                        };
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(
                        "Broker health check ({Mode}) attempt {Attempt}/{MaxRetries} failed: {ErrorMessage}. Retrying in {DelayMs}ms...",
                        mode,
                        attempt,
                        maxRetries,
                        ex.Message,
                        delayMs);

                    if (attempt < maxRetries)
                    {
                        await Task.Delay(delayMs);
                    }
                }
            }

            return new BrokerHealthCheckResult
            {
                IsHealthy = false,
                IdempotenceEnabled = enableIdempotenceTest,
                Message = $"Failed after {maxRetries} attempts"
            };
        }
    }
}
