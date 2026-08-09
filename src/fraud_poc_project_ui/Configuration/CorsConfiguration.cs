using fraud_poc_project_buss.Models.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace fraud_poc_project.Configuration
{
    public static class CorsConfiguration
    {
        public static void AddCorsConfiguration(this WebApplicationBuilder builder)
        {
            builder.Services.AddOptions<CorsOptions>().PostConfigure<IOptions<AppSettings>>((corsOptions, appSettings) =>
            {
                var corsSettings = appSettings.Value.CORS;
                if (corsSettings != null && corsSettings.Enabled)
                {
                    corsOptions.AddDefaultPolicy(policy =>
                    {
                        policy.WithOrigins(corsSettings.Origins)
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                }
                ;
            });
        }
    }
}