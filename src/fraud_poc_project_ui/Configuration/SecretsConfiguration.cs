using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;

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

            // release is the environment name used in the AWS deployment, so treat it as local for the purposes of loading secrets.
            var isLocal = (envName == "loc" || envName == "release");

            if (!isLocal)
                return; // in every other environment, secrets come from AWS Secrets Manager instead.

            builder.Configuration.AddJsonFile("secrets.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddEnvironmentVariables();
        }
    }
}
