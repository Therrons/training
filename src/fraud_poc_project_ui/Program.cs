using fraud_poc_project.Configuration;
using fraud_poc_project_models.Dto;
using fraud_poc_project_models.Models.Settings;
using fraud_poc_project_repo.Connection;
using HealthChecks.Kubernetes;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

public class Program
{
    public static string file_Path_Name = "";
    private const string FileName = "input.txt";

    // Main must be async so we can await Secrets Manager before the web server starts.
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .Enrich.FromLogContext()
            .WriteTo.File("/repo/data/logs/app-.log", rollingInterval: RollingInterval.Day)
            .ReadFrom.Configuration(builder.Configuration)
            .CreateLogger();

        builder.Services.AddSerilog(); // Add Serilog services to the DI container

        builder.Host.UseSerilog(); // Use Serilog for logging

        bool isLocal = builder.Environment.EnvironmentName.Equals("LOC", StringComparison.InvariantCultureIgnoreCase);
        if (isLocal)
            builder.Configuration.AddEnvironmentVariables();

        if (args != null && args.Length > 0)
            builder.Configuration.AddCommandLine(args);

        var envVariables = new Environment_Variables().Get_Environment_Values(builder);
        var connectionString = "";

        if (!isLocal)
            connectionString = $"Server={envVariables.DBHost};Port={envVariables.DBPort};Database={envVariables.DBName};" +
                               $"User Id={envVariables.DBUsername};Password={envVariables.DBPassword};" +
                               "Pooling=true;Connection Lifetime=0;SSLMode=Require;Trust Server Certificate=true;";
        else
            connectionString = $"Server={envVariables.DBHost};Database={envVariables.DBName};" +
                               $"User Id={envVariables.DBUsername};Password={envVariables.DBPassword};" +
                               "Pooling=true;Connection Lifetime=0;SSLMode=Disable;Trust Server Certificate=true;";


        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:PostgreSQL"] = connectionString
        });

        builder.Services.AddDbContextPool<DB_Connection_Read_Write>(options => options.UseNpgsql(connectionString));

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
            "/repo/data";

        var version_docker_build = Environment.GetEnvironmentVariable("Build_Version") ??
            "Docker Build Version: UNKNOWN";

        if (string.IsNullOrWhiteSpace(writeDir))
            throw new InvalidOperationException("A non-empty --write-dir or WRITE_DIR is required.");

        if (!Directory.Exists(writeDir)) Directory.CreateDirectory(writeDir);

        file_Path_Name = Path.Combine(writeDir, FileName);


        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddHealthChecks();

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

            c.MapType<FraudQueryDto>(() => new OpenApiSchema
            {
                Type = "object",
                Properties = new Dictionary<string, OpenApiSchema>
                {
                    ["dateFrom"] = new OpenApiSchema { Type = "string", Format = "date-time", Description = "Inclusive start date/time (transaction_time)." },
                    ["dateTo"] = new OpenApiSchema { Type = "string", Format = "date-time", Description = "Inclusive end date/time (transaction_time)." },
                    ["customerId"] = new OpenApiSchema { Type = "string", Description = "Optional customer identifier filter." },
                    ["isFlaggedOnly"] = new OpenApiSchema { Type = "boolean", Description = "Return only flagged events when true." },
                    ["transactionType"] = new OpenApiSchema { Type = "string", Description = "Optional transaction type filter (e.g. POS, ATM, EFT, CNP)." },
                    ["minFraudScore"] = new OpenApiSchema { Type = "number", Format = "decimal", Description = "Optional minimum fraud score (0-100)." }
                }
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(builder.Environment.ContentRootPath, xmlFile);
            if (File.Exists(xmlPath))
                c.IncludeXmlComments(xmlPath);

            c.OperationFilter<fraud_poc_project.Fraud.Swagger.FraudExamplesOperationFilter>();
        });

        builder.Services.Configure<HostFilteringOptions>(options =>
        {
            options.AllowedHosts = new[] { "*" };
        });

        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Docker Build Data: {time_docker_build}\r\nDocker Build Version: {version_docker_build}", time_docker_build, version_docker_build);

        var fwdOptions = new ForwardedHeadersOptions
        {
            ForwardedHeaders =
                ForwardedHeaders.XForwardedFor |
                ForwardedHeaders.XForwardedProto |
                ForwardedHeaders.XForwardedHost
        };

        app.UseForwardedHeaders(fwdOptions);
        app.UseHostFiltering();
        app.MapControllers();

        var configuredServerUrl = app.Configuration.GetValue<string>("Meta:Url");

        app.UseSwagger(options =>
        {
            options.PreSerializeFilters.Add((swaggerDoc, httpReq) =>
            {
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

        app.ConfigureHealthChecks();

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
