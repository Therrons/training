using fraud_poc_project.Configuration;
using fraud_poc_project.Middleware;
using HealthChecks.Kubernetes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public class Program
{
    public static string file_Path_Name = "";
    private const string FileName = "input.txt";

    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Step 1: set up logging (Serilog) so everything below can log to the console
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
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

        // Step 3: determine the write directory for the app, and create it if it doesn't exist.
        var writeDir =
            builder.Configuration["write-dir"] ??
            Environment.GetEnvironmentVariable("write_dir") ??
            Path.Combine(AppContext.BaseDirectory, "data"); // Default write directory - if not specified in configuration or environment variable

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

        // Validate incoming requests for XSS attacks.
        // Applied only to endpoints marked with [ValidateXss] attribute.
        // Must be after UseRouting() so endpoint metadata is available.
        app.UseMiddleware<CheckForXssMiddleware>();

        app.UseSwagger();

        // Step 7: configure Swagger UI based on the environment (local vs production).
        if (!isLocal)
            app.UseSwaggerUI(settings => settings.SupportedSubmitMethods(Array.Empty<SubmitMethod>())); // Read-only documentation in production
        else
            app.UseSwaggerUI(c => c.DefaultModelRendering(ModelRendering.Example));

        app.ConfigureHealthChecks();

        app.MapControllers();

        if (!isLocal)
            app.Run("http://0.0.0.0:8080");
        else
            app.Run();
    }


}
