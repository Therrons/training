using fraud_poc_project.Configuration;
using fraud_poc_project.Controllers;
using fraud_poc_project.Middleware;
using fraud_poc_project.Utilities;
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
    private const string FileName = "input.txt";

    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Services.AddSerilog();
        builder.Host.UseSerilog();

        builder.AddConfigurations();
        builder.ConfigureSecrets();
        builder.AddServices_AddDI();
        builder.AddCorsConfiguration();

        if (args != null && args.Length > 0)
            builder.Configuration.AddCommandLine(args);

        var configuredWriteDir = builder.Configuration["write-dir"] ?? Environment.GetEnvironmentVariable("write_dir");
        var writeDir = PathUtility.GetDataDirectory(configuredWriteDir);
        PathUtility.EnsureDirectoryExists(writeDir);

        var version_docker_build = Environment.GetEnvironmentVariable("Build_Version") ??
            "Docker Build Version: UNKNOWN";

#if DEBUG
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string> { { "file_Path_Name", Path.Combine(writeDir, FileName) } });
#endif

        var isLocal = builder.Environment.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });

        builder.Services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowedHosts = new[] { "*" };
        });

        // Validate idempotent producer config before startup (see KafkaIdempotence.cs)
        var kafKaProducerIdemPotence = builder.Configuration["KafkaSettings:ProducerSettings:EnableIdempotence"]?.ToLowerInvariant();
        if (kafKaProducerIdemPotence == "true") builder.AddKafkaProducerIdempotence();

        builder.ConfigureSwagger();
        var app = builder.Build();

        app.UseRouting();
        app.UseCors();

        // XSS validation middleware - only on [ValidateXss] endpoints
        app.UseMiddleware<CheckForXssMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.UseSwagger();

        // Restrict Swagger UI submit methods in production
        if (!isLocal)
            app.UseSwaggerUI(settings => settings.SupportedSubmitMethods([SubmitMethod.Get, SubmitMethod.Post, SubmitMethod.Put]));
        else
            app.UseSwaggerUI(c => c.DefaultModelRendering(ModelRendering.Example));

        app.ConfigureHealthChecks();

        app.MapControllers();

        if (!isLocal)
            app.Run("http://0.0.0.0:8083");
        else
            app.Run();
    }


}
