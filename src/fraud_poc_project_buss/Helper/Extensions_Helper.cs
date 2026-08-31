using Confluent.Kafka;
using fraud_poc_project_buss.Models.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace fraud_poc_project_buss.Helper
{
    // Small shared helper functions used in a few places around the codebase.
    public static class Extensions_Helper
    {
        public static readonly bool IsDebugMode = IsDebugBuild();

        private static bool IsDebugBuild()
        {
#if DEBUG
            return true;
#else
        return false;
#endif
        }

        // Converts a Kafka message with a byte[] value into a Kafka message with a string value.
        public static Message<string, string> Stream_Byte_Key_Value_To_String_Key_Value(this Message<string, byte[]> kvp)
        {
            return new Message<string, string>
            {
                Key = kvp?.Key ?? "",
                Value = System.Text.Encoding.UTF8.GetString(kvp?.Value ?? new byte[0]) ?? ""
            };
        }

        // Checks that a settings object (e.g. loaded from appsettings.json) has all its
        // required fields filled in. If something is missing, this throws an error that
        // explains exactly what's wrong, instead of failing later in a confusing way.
        public static void ValidateOptions<T>(T options)
        {
            var validationContext = new ValidationContext(options);
            var validationResults = new List<ValidationResult>();
            if (Validator.TryValidateObject(options, validationContext, validationResults,
                    validateAllProperties: true))
            {
                return;
            }
            var errors = validationResults.Select(validationResult => $"Validation error: {validationResult.ErrorMessage}").ToList();
            throw new ApplicationException(string.Join('\n', errors));
        }

        // Reads a text column from a database row, returning null instead of throwing
        // if the database value was actually NULL.
        public static string? DBNullString(this NpgsqlDataReader r, string colName)
        {
            var ord = r.GetOrdinal(colName);
            return r.IsDBNull(ord) ? null : r.GetString(ord);
        }

        public static void LogInformationOnly<T>(this ILogger<T> logger, string message, params object[] args)
        {
            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(message, args);
        }

        public static void LogSensitiveData<T>(this ILogger<T> logger, string message, params object[] args)
        {

            if (IsDebugMode && logger.IsEnabled(LogLevel.Information))
                logger.LogInformation(message, args);
        }

        public static DateTime? ToDateTimeFrom_yyyyMMddHHmmss(this string dateString)
        {
            if (string.IsNullOrWhiteSpace(dateString))
            {
                return null;
            }

            const string format = "yyyy-MM-dd HH:mm:ss";

            if (DateTime.TryParseExact(dateString, format, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime result))
            {
                return result;
            }

            return null;
        }

        /// <summary>
        /// Registers and configures options with automatic binding from configuration,
        /// data annotation validation, and startup validation.
        /// </summary>
        /// <typeparam name="T">The options class to register</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configurationSection">The configuration section name to bind to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAndValidateOptions<T>(
            this IServiceCollection services,
            string configurationSection) where T : class
        {
            services.AddOptions<T>()
                .BindConfiguration(configurationSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }

        public static AdminClientConfig CreateAdminClientConfig(this FraudKafkaBrokerSettings brokerSettings)
        {
            return new AdminClientConfig
            {
                BootstrapServers = brokerSettings.BootstrapServers,
                SaslUsername = brokerSettings.SaslUserName,
                SaslPassword = brokerSettings.SaslPassword,
                SaslMechanism = brokerSettings.SaslMechanism,
                SecurityProtocol = brokerSettings.SecurityProtocol,
                SslEndpointIdentificationAlgorithm = SslEndpointIdentificationAlgorithm.None,
                AllowAutoCreateTopics = brokerSettings.AllowAutoCreateTopics
            };
        }

        /// <summary>
        /// Converts a raw Kafka log entry into appropriate log levels for consistent logging.
        /// </summary>
        public static void LogKafkaMessage<T>(this ILogger<T> logger, LogMessage logMessage)
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
        public static void LogKafkaError<T>(this ILogger<T> logger, Error error)
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
