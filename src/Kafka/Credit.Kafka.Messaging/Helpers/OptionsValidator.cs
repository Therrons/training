using System.ComponentModel.DataAnnotations;

namespace Credit.Kafka.Messaging.Helpers;

public static class OptionsValidator
{
    public static void ValidateOptions<T>(T options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var validationContext = new ValidationContext(options, serviceProvider: null, items: null);
        var validationResults = new List<ValidationResult>();
        if (Validator.TryValidateObject(options, validationContext, validationResults,
                validateAllProperties: true))
        {
            return;
        }

        var errors = validationResults.Select(validationResult => $"Validation error: {validationResult.ErrorMessage}").ToList();

        throw new ApplicationException(string.Join('\n', errors));
    }
}