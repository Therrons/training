using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace Credit.Kafka.Messaging.Helpers;

public static class KafkaLogHandler
{
    public static void LogMessage<TKey, TValue, TLogger>(IConsumer<TKey, TValue> consumer, LogMessage message, ILogger<TLogger> logger)
    {
        LogMessage(message, logger);
    }

    public static void LogMessage<TKey, TValue, TLogger>(IProducer<TKey, TValue> producer, LogMessage message,
        ILogger<TLogger> logger)
    {
        LogMessage(message, logger);
    }

    private static void LogMessage(LogMessage message, ILogger logger)
    {
        if (logger.IsEnabled(LogLevel.Debug))
            logger.LogDebug("Kafka (librdkafka): {Message}.", message.Message);
    }
}