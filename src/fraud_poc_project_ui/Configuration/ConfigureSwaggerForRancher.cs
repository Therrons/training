using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace fraud_poc_project.Configuration
{
    public static class ConfigureSwaggerForRancher
    {
        /// <summary>
        /// Registers Swagger (the API documentation/testing page) so it keeps working
        /// when the app is hosted behind Rancher/Kubernetes.
        /// Note: Program.cs also registers Swagger separately with a bit more detail
        /// (contact info, XML comments). Both run - this one just adds a second,
        /// simpler registration on top.
        /// </summary>
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
