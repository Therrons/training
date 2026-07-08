using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using fraud_poc_project_models.Models.Database;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

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
