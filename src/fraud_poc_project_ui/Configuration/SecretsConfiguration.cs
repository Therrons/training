using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System;

namespace fraud_poc_project.Configuration
{
    public static class SecretsConfiguration
    {
        public static void ConfigureSecrets(this WebApplicationBuilder builder)
        {
            if (builder.Environment.EnvironmentName.Equals("LOC", StringComparison.InvariantCultureIgnoreCase))
            {
                builder.Configuration.AddJsonFile("secrets.json", true, true);
                builder.Configuration.AddEnvironmentVariables();
                return;
            }
        }
    }
}
