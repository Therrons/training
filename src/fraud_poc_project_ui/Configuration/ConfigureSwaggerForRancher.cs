using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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

                // ════════════════════════════════════════════════════════════════
                // JWT Bearer Token Security Scheme
                // This enables the Authorize button in Swagger UI
                // ════════════════════════════════════════════════════════════════
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme. " +
                                  "Example: \"Authorization: Bearer {token}\"\n\n" +
                                  "Steps:\n" +
                                  "1. Call POST /api/fraud/login with username='XXX' and password='YYY'\n" +
                                  "2. Copy the token from the response\n" +
                                  "3. Click the Authorize button and paste the token (with or without 'Bearer ' prefix)\n" +
                                  "4. All subsequent requests will include the token in the Authorization header"
                });

                // Apply JWT security requirement to all endpoints
                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        new List<string>()
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
