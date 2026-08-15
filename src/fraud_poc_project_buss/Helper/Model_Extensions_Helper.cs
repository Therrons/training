using Npgsql;
using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_buss.Helper
{
    // Small shared helper functions used in a few places around the codebase.
    public static class Model_Extensions_Helper
    {
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
    }
}
