using docke_web_Api.Controllers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using ILogger = Serilog.ILogger;

public class Program
{
    public static string file_Path_Name = "";
    private const string FileName = "input.txt";

    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // --------- Serilog setup: appsettings.json ----------
        // NOTE: remember to tell the app how to find the Serilog configuration if you use appsettings.json or environment variables for Serilog settings.
        // =================================

        // Serilog configuration: logs to console and rolling file with daily intervals
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            //.WriteTo.File("/repo/data/logs/app-.log", rollingInterval: RollingInterval.Day) //
            .ReadFrom.Configuration(builder.Configuration) // Optional: read additional Serilog settings from configuration 
            .CreateLogger();

        builder.Host.UseSerilog(); // Use Serilog for logging instead of the default .NET logger

        // Ensure command-line args are in configuration (CreateBuilder already adds them, this is fine)
        builder.Configuration.AddCommandLine(args);

        var time_docker_build = DateTime.Now.ToString();

        // --------- Write-dir handling ----------
        var writeDir =
            builder.Configuration["write-dir"] ??
            Environment.GetEnvironmentVariable("WRITE_DIR") ??
            "/repo/data";

        // --------- Docker build metadata (optional) ----------
        var version_docker_build = Environment.GetEnvironmentVariable("Build_Version") ??
            "Docker Build Version: UNKNOWN";

        //Console.WriteLine($"\r\nDocker Build Data: {time_docker_build}\r\nDocker Build Version:{version_docker_build}");

        if (string.IsNullOrWhiteSpace(writeDir))
            throw new InvalidOperationException("A non-empty --write-dir or WRITE_DIR is required.");

        Directory.CreateDirectory(writeDir);
        file_Path_Name = Path.Combine(writeDir, FileName);
        // ---------------------------------------------------

        var env = builder.Environment;
        var isLocal = env.IsDevelopment() ||
                      env.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);

        // Controllers + Swagger services
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            // Common, expressive metadata
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Docker Web API",
                Version = "v1",
                Description = "This API is used for testing docker and c# web api",
                Contact = new OpenApiContact
                {
                    Email = "centralisedsystems@capitecbank.co.za",
                    Name = "Centralised Systems"
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(builder.Environment.ContentRootPath, xmlFile);
            if (File.Exists(xmlPath))
            {
                c.IncludeXmlComments(xmlPath);
            }
        });

        var app = builder.Build();

        // Resolve logger from DI so it's wired to Serilog
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Docker Build Data: {time_docker_build}\r\nDocker Build Version: {version_docker_build}", time_docker_build, version_docker_build);

        // required for swagger exploration
        // ================================
        app.MapControllers();
        app.UseSwagger();
        // ================================

        //---------Forwarded Headers for reverse proxies (Nginx) ----------
        //This ensures Request.Scheme / Host are correct when Nginx terminates TLS or rewrites host.

        var fwdOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost
        };

        // In containerized setups (Docker bridge), the proxy IP is dynamic.
        // Clearing KnownNetworks/Proxies lets us trust all forwarded headers.
        // Only do this if you fully trust the network (as in docker-compose/Nginx scenario).
        //fwdOptions.KnownNetworks.Clear();
        //fwdOptions.KnownProxies.Clear();

        app.UseForwardedHeaders(fwdOptions); // Must be before any middleware that uses Request.Scheme/Host, including Swagger pre-serialization filter.
        // ------------------------------------------------------------------

        // OPTIONAL: Only enable HTTPS redirection if you run TLS at Kestrel.
        // If Nginx terminates TLS and speaks HTTP to Kestrel, leave this OFF to avoid loops.
        // app.UseHttpsRedirection();

        // ---- Swagger: available in ALL environments, with dynamic "servers" URL ----
        // If Meta:Url is provided in configuration, it will be used.
        var configuredServerUrl = app.Configuration.GetValue<string>("Meta:Url");

        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
                // Prefer configured Meta:Url if present; otherwise infer from the request (after forwarded headers)
                var scheme = httpReq.Scheme;
                var host = httpReq.Headers["X-Forwarded-Host"].FirstOrDefault()
                           ?? httpReq.Host.Value;

                var pathBase = httpReq.Headers["X-Forwarded-Prefix"].FirstOrDefault()
                               ?? httpReq.PathBase.Value;

                var serverUrl = string.IsNullOrWhiteSpace(configuredServerUrl)
                    ? $"{scheme}://{host}{pathBase}"
                    : configuredServerUrl.TrimEnd('/');

                swaggerDoc.Servers = new List<OpenApiServer>
                {
                    new OpenApiServer { Url = serverUrl }
                };
            });
        });

        if (!isLocal)
        {
            app.UseSwaggerUI(settings => settings.SupportedSubmitMethods());
            app.Run("http://0.0.0.0:8080");
        }
        else
        {
            app.UseSwaggerUI();
            app.Run();
        }
    }
}