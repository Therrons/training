using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.IO;

namespace fraud_poc_project.Configuration
{
    public static class ConfigureSwaggerForRancher
    {
        // This method sets up the Swagger documentation and UI, including the 
        // title, version, description, and contact information. It also includes XML comments for better documentation.
        public static void ConfigureSwagger(this WebApplicationBuilder builder)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
                {
                    Title = "Fraud Detection API",
                    Version = "v1",
                    Description = "Consumes categorized transaction events from Kafka, applies fraud rules, stores results in PostgreSQL, and exposes them via this API.",
                    Contact = new OpenApiContact
                    {
                        Email = "centralisedsystems@capitecbank.co.za",
                        Name = "Centralised Systems"
                    }
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
