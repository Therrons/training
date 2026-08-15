using fraud_poc_project_buss.Models.Settings;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace fraud_poc_project.Configuration
{
    // CORS controls which other websites are allowed to call this API directly from a
    // browser. This only adds a policy if CORS is turned on in AppSettings; otherwise
    // browser calls from other sites will be blocked by default, which is the safer option.
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
            });
        }
    }
}