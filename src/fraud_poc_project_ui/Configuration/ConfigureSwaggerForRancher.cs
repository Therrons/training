using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace fraud_poc_project.Configuration
{
    public static class ConfigureSwaggerForRancher
    {
        /// <summary>
        /// Configure Swagger to dynamically set the server URL based on the incoming request 
        /// this is because the service is hosted in K8s and I still want the swagger to work
        /// </summary>
        /// <param name="builder"></param>
        public static void ConfigureSwagger(this WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Fraud Detection API",
                    Version = "v1",
                    Description = "API for Fraud Detection Service"
                });
                // Set the comments path for the Swagger JSON and UI.
                var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }
    }
}
