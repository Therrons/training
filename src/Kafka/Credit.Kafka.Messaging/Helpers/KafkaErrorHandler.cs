using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Helpers;

public class KafkaErrorHandler
{
    public static void HandleError<TKey, TValue, TLogger>(IConsumer<TKey, TValue> consumer, Error error, ILogger<TLogger> logger)
    {
        HandleError(error, logger);
    }

    public static void HandleError<TKey, TValue, TLogger>(IProducer<TKey, TValue> producer, Error error, ILogger<TLogger> logger)
    {
        HandleError(error, logger);
    }

    private static void HandleError(Error error, ILogger logger)
    {
        if (error.IsFatal)
        {
            logger.LogError("Kafka (librdkafka): fatal error, code: {Code}, reason: {Reason}.", error.Code, error.Reason);
            return;
        }

        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Kafka (librdkafka): error, code: {Code}, reason: {Reason}.", error.Code, error.Reason);
    }
}