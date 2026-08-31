using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Helper
{
    // Small shared helper functions used in a few places around the codebase.
    public static class Model_Extensions_Helper
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
    }
}
