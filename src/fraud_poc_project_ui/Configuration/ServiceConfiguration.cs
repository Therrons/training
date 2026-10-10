using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

using Polly;
using Polly.Retry;

using fraud_poc_project.Controllers;
using fraud_poc_project.Kafka.Consumer;
using fraud_poc_project.Services;
using fraud_poc_project.Settings;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Database;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Connection;
using fraud_poc_project_repo.Interfaces;
using fraud_poc_project_repo.Kafka;
using fraud_poc_project_repo.Optimizations;

namespace fraud_poc_project.Configuration
{
    public static class ServiceConfiguration
    {
        private static Environment_Variables envVariables;

        public static void AddServices_AddDI(this WebApplicationBuilder builder)
        {
            envVariables = new Environment_Variables().Get_Environment_Values(builder);

            RegisterAppSettings(builder);

            var connectionString = BuildDatabaseConnectionString(builder);
            RegisterDatabaseAccess(builder, connectionString);
            RegisterFraudRules(builder);
            RegisterMemoryCache(builder);  // OPTIMIZATION: Add memory caching
            RegisterTestOnlyControllers(builder);
            RegisterRetryPipeline(builder);
            RegisterAuthentication(builder);
            RegisterJwt(builder);
            RegisterMetrics(builder);
            RegisterKafka(builder);
            SetupDatabase(builder);
        }

        private static void RegisterRetryPipeline(WebApplicationBuilder builder)
        {
            builder.Services.AddResiliencePipeline("exception", x =>
            {
                x.AddRetry(
                    new RetryStrategyOptions
                    {
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromMilliseconds(1),
                        MaxRetryAttempts = 2,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder().Handle<Exception>()
                    }).AddTimeout(TimeSpan.FromSeconds(30));
            });
        }

        private static void RegisterAppSettings(WebApplicationBuilder builder)
        {
            builder.Services.AddAndValidateOptions<AppSettings>("AppSettings");
            builder.Services.AddAndValidateOptions<Database>("Database");
        }

        #region Database Operations
        private static string BuildDatabaseConnectionString(WebApplicationBuilder builder)
        {
            var isLocal = true; // builder.Environment.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);
            var isRancher = true; // builder.Environment.EnvironmentName.Equals("RELEASE", StringComparison.InvariantCultureIgnoreCase);

            var connectionString = new StringBuilder();
            connectionString.Append($"Server={envVariables.DBHost};");
            connectionString.Append($"Database={envVariables.DBName};");
            connectionString.Append($"User Id={envVariables.DBUsername};");
            connectionString.Append($"Password={envVariables.DBPassword};");
            connectionString.Append($"Pooling=true;");
            connectionString.Append($"Connection Lifetime=0;");

            connectionString.Append((isLocal || isRancher) ? "SSLMode=Disable;" : "SSLMode=Require;");
            connectionString.Append("Trust Server Certificate = true;");

            var result = connectionString.ToString();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = result
            });

            return result;
        }

        private static void RegisterDatabaseAccess(WebApplicationBuilder builder, string connectionString)
        {
            builder.Services.AddDbContextPool<IDBConnection, DBConnection>(options => options.UseNpgsql(connectionString));

            // Use optimized repository
            builder.Services.AddScoped<IOptimizedFraudRepository, OptimizedFraudRepository>();
            builder.Services.AddScoped<IFraudRepository>(provider => provider.GetRequiredService<IOptimizedFraudRepository>());

            builder.Services.AddScoped<IConfiguration>(p => builder.Configuration);
        }

        private static void RegisterMemoryCache(WebApplicationBuilder builder)
        {
            builder.Services.AddMemoryCache(options =>
            {
                options.SizeLimit = 100 * 1024 * 1024;
            });

            builder.Services.AddScoped<ICachedFraudRuleService, CachedFraudRuleService>();
        }

        private static void SetupDatabase(WebApplicationBuilder builder)
        {
            var createDbFlag = builder.Configuration["Database:CreateDatabaseOnStartup"]?.ToLowerInvariant();
            if (createDbFlag == "yes" || createDbFlag == "true")
            {
                builder.Services.ConfigureDatabaseServices(builder, builder.Configuration);
            }
        }
        #endregion

        private static void RegisterFraudRules(WebApplicationBuilder builder)
        {
            builder.Services
                .AddSingleton<IFraudRule, HighAmountRule>()
                .AddSingleton<IFraudRule, ForeignCnpRule>()
                .AddSingleton<IFraudRule, AtmWithdrawalLimitRule>()
                .AddSingleton<IFraudRule, HighRiskMerchantCategoryRule>()
                .AddSingleton<IFraudRule, RoundAmountRule>();

            builder.Services
                .AddSingleton<IFraudEvaluationService, FraudEvaluationService>();
        }

        private static void RegisterKafka(WebApplicationBuilder builder)
        {
            builder.Services.AddKafkaConfigurations(builder.Configuration, envVariables)
               .KafkaSetupTopics();

            builder.Services.AddSingleton<IFraudProducer, FraudProducer>();
            builder.Services.AddSingleton<FraudConsumer>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FraudConsumer>());
            builder.Services.AddTransient<ITransactionEventHandler, FraudConsumerWorker>();
        }

        private static void RegisterAuthentication(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
        }

        private static void RegisterJwt(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IJwtService, JwtService>();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            var secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            var key = System.Text.Encoding.ASCII.GetBytes(secret);

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Bearer";
                options.DefaultChallengeScheme = "Bearer";
            })
            .AddJwtBearer("Bearer", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidateAudience = true,
                    ValidAudience = jwtSettings["Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            builder.Services.AddAuthorization();
        }

        private static void RegisterTestOnlyControllers(WebApplicationBuilder builder)
        {
            if (Extensions_Helper.IsDebugMode)
            {
                builder.Services.AddTransient<LoadSimulatorController>();
                builder.Services.AddTransient<FraudController>();
            }
        }

        private static void RegisterMetrics(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IMetricsService, MetricsService>();
        }
    }
}
