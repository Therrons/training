using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace fraud_poc_project.Configuration
{
    // Sets up where the app reads its settings from: appsettings.json, an
    // environment-specific settings file (e.g. appsettings.PROD.json), then
    // environment variables (which can override anything above).
    public static class ApplicationConfiguration
    {
        public static void AddConfigurations(this WebApplicationBuilder builder)
        {
            builder.Configuration.SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName.ToUpper()}.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables();
        }
    }
}
