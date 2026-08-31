using Confluent.Kafka;
using Microsoft.Extensions.Logging;

namespace fraud_poc_project_repo.Kafka.Helpers
{
    /// <summary>
    /// Centralized Kafka logging utility to eliminate duplicate logging code across producers and consumers.
    /// </summary>
    public static class KafkaLoggingHelper
    {
        /// <summary>
        /// Converts a raw Kafka log entry into appropriate log levels for consistent logging.
        /// </summary>
        public static void LogKafkaMessage(ILogger logger, LogMessage logMessage)
        {
            var level = logMessage.Level switch
            {
                SyslogLevel.Emergency or SyslogLevel.Alert or SyslogLevel.Critical or SyslogLevel.Error => LogLevel.Error,
                SyslogLevel.Warning => LogLevel.Warning,
                SyslogLevel.Notice or SyslogLevel.Info => LogLevel.Information,
                _ => LogLevel.Debug
            };

            logger.Log(level, "Kafka (librdkafka): {Message}", logMessage.Message);
        }

        /// <summary>
        /// Logs a Kafka client error. Fatal errors indicate the connection is broken and recovery is unlikely.
        /// </summary>
        public static void LogKafkaError(ILogger logger, Error error)
        {
            if (error.IsFatal)
            {
                logger.LogCritical("Kafka fatal error: {Code} - {Reason}", error.Code, error.Reason);
            }
            else
            {
                logger.LogError("Kafka error: {Code} - {Reason}", error.Code, error.Reason);
            }
        }
    }
}
