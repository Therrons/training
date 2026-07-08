using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace fraud_poc_project.Configuration
{
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
