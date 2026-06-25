using System.ComponentModel.DataAnnotations;

namespace fraud_poc_project_models.Helper
{
    public static class Model_Extensions_Helper
    {
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
    }
}
