using fraud_poc_project_buss.Models.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Configuration
{
    // Only used when the producer's EnableIdempotence setting is true. Checks the
    // Kafka broker before the app finishes starting up, and quietly turns idempotence
    // off if the broker doesn't support it - better to start in a degraded mode than
    // not start at all.
    public static class KafkaIdempotence
    {
        public static async Task AddKafkaProducerIdempotence(this WebApplicationBuilder builder)
        {
            // Check the broker now, before the app finishes starting, because idempotent
            // mode can take longer to become ready and we want to adjust settings if needed.
            var brokerSettings = builder.Services.BuildServiceProvider().GetRequiredService<IOptions<FraudKafkaBrokerSettings>>();
            var preFlightLogger = builder.Services.BuildServiceProvider().GetRequiredService<ILogger<KafkaBrokerHealthCheck>>();

            var preFlightHealthCheck = new KafkaBrokerHealthCheck(
                Options.Create(brokerSettings.Value),
                preFlightLogger);

            BrokerHealthCheckResult healthResult = null;
            try
            {
                healthResult = await preFlightHealthCheck.WaitForBrokerReadyWithFallbackAsync(
                    maxRetries: 10,
                    delayMs: 500,
                    coordinatorTimeoutMs: 5000);

                if (!healthResult.IdempotenceEnabled)
                {
                    builder.Services.Configure<FraudKafkaProducerSettings>(options => options.EnableIdempotence = false);
                }
            }
            catch (Exception ex)
            {
                preFlightLogger.LogError(ex, "Kafka broker health check failed. Application will start but Kafka operations may fail. Ensure Kafka broker is running at {BootstrapServers}", brokerSettings.Value.BootstrapServers);
                healthResult = new BrokerHealthCheckResult
                {
                    IsHealthy = false,
                    IdempotenceEnabled = true,
                    Message = "Health check failed but app proceeding"
                };
            }
            finally
            {
                if (healthResult != null)
                {
                    if (!healthResult.IdempotenceEnabled)
                    {
                        preFlightLogger.LogWarning("Kafka broker is running in NON-IDEMPOTENT mode. Producer will NOT guarantee exactly-once semantics.");
                    }
                    else
                    {
                        preFlightLogger.LogWarning("Kafka broker is running with idempotence enabled.");
                    }
                }
                else
                {
                    preFlightLogger.LogWarning("Kafka broker is NOT available. Swagger UI will start but Kafka operations will fail when attempted.");
                }
            }
        }
    }
}
