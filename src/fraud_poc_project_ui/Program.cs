using fraud_poc_project.Configuration;
using fraud_poc_project_buss.Models.Kafka;
using HealthChecks.Kubernetes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

// This is the app's starting point - it runs once when the app boots up.
// It wires everything together, in order: logging, secrets, configuration,
// application services (database, Kafka, fraud rules), then starts the web server.
public class Program
{
    public static string file_Path_Name = "";
    private const string FileName = "input.txt";

    // Main must be async so we can await Secrets Manager before the web server starts.
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Step 1: set up logging (Serilog) so everything below can log to the console
        // and to a rolling daily log file.
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .WriteTo.File("/repo/data/logs/app-.log", rollingInterval: RollingInterval.Day)
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Services.AddSerilog();  // Add Serilog services to the DI container
        builder.Host.UseSerilog();      // Use Serilog for logging

        // Step 2: load secrets and settings, then register every service the app needs.
        builder.ConfigureSecrets();     // Load secrets and add them to the configuration
        builder.AddConfigurations();    // Add configurations from appsettings.json, environment variables, and command line arguments
        builder.AddServices_AddDI();    // Add application services to the DI container
        builder.AddCorsConfiguration(); // Add CORS configuration to the DI container - the alternative would be to add CORS via Nginx
                                        // or native cloud solution, eg AWS API Gateway

        if (args != null && args.Length > 0)
            builder.Configuration.AddCommandLine(args);

        // Step 3: optionally run the database setup scripts before anything else starts.
        // ── Database initialization ───────────────────────────
        var createDbFlag = builder.Configuration["Database:CreateDatabaseOnStartup"]?.ToLowerInvariant();
        if (createDbFlag == "yes" || createDbFlag == "true")
        {
            builder.Services.ConfigureDatabaseServices(builder, builder.Configuration);
        }

        var time_docker_build = DateTime.Now.ToString();

        var writeDir =
            builder.Configuration["write-dir"] ??
            Environment.GetEnvironmentVariable("write_dir") ??
            "/repo/data"; // Default write directory once running in K8s -  if not specified in configuration or environment variable

        var version_docker_build = Environment.GetEnvironmentVariable("Build_Version") ??
            "Docker Build Version: UNKNOWN";

        if (!Directory.Exists(writeDir)) Directory.CreateDirectory(writeDir);


        // use for testing purposes only, to write a file to the host machine
#if DEBUG
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string> { { "file_Path_Name", Path.Combine(writeDir, FileName) } });
#endif

        var isLocal = builder.Environment.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);

        // Step 4: register the API framework pieces (controllers, Swagger, health checks).
        builder.Services.AddControllers();          // Add controller services to the DI container
        builder.Services.AddEndpointsApiExplorer(); // Add API explorer services to the DI container
        builder.Services.AddHealthChecks();         // Add health check services to the DI container

        if (!isLocal)
        {
            builder.Services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
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
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(builder.Environment.ContentRootPath, xmlFile);
                if (File.Exists(xmlPath))
                    c.IncludeXmlComments(xmlPath);
            });
        }
        else
            builder.Services.AddSwaggerGen();

        builder.Services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowedHosts = new[] { "*" };
        });

        // Step 5: if idempotent Kafka producing is turned on, double-check the broker
        // supports it before we finish starting up (see KafkaIdempotence.cs).
        var kafKaProducerIdemPotence = builder.Configuration["KafkaSettings:ProducerSettings:EnableIdempotence"]?.ToLowerInvariant();
        if (kafKaProducerIdemPotence == "true") builder.AddKafkaProducerIdempotence();

        // Step 6: build the app and start handling web requests.
        var app = builder.Build();
        app.UseRouting();
        app.UseSwagger();

        if (!isLocal)
            app.UseSwaggerUI(settings => settings.SupportedSubmitMethods(Array.Empty<SubmitMethod>())); // Read-only documentation in production
        else
            app.UseSwaggerUI();

        app.ConfigureHealthChecks();

        app.MapControllers();

        if (!isLocal)
            app.Run("http://0.0.0.0:8080");
        else
            app.Run();
    }

    
}
