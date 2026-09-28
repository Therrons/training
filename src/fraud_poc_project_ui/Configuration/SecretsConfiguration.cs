using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace fraud_poc_project.Configuration
{
    // When running locally, loads secrets from a local secrets.json file instead of AWS
    // Secrets Manager, so developers don't need AWS access just to run the app on their
    // own machine.
    public static class SecretsConfiguration
    {
        public static void ConfigureSecrets(this WebApplicationBuilder builder)
        {
            var envName = builder.Environment.EnvironmentName.Trim().ToLower();

            if (envName == "loc")
                builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);

        }
    }
}
