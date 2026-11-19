using fraud_poc_project.Controllers;
using fraud_poc_project.Interfaces;
using fraud_poc_project.Kafka.Consumer;
using fraud_poc_project.Services;
using fraud_poc_project.Settings;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models.Database;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;

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
            var resilienceSettings = builder.Configuration.GetSection("ResiliencePipeLineSettings").Get<ResiliencePipeLineSettings>();

            builder.Services.AddResiliencePipeline("exception", x =>
            {
                x.AddRetry(
                    new RetryStrategyOptions
                    {
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromMilliseconds(resilienceSettings.DelayCount),
                        MaxRetryAttempts = resilienceSettings.RetryCount,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder().Handle<Exception>()
                    }).AddTimeout(TimeSpan.FromSeconds(resilienceSettings.TimeoutValue));
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
            builder.Services.AddScoped<IFraudRepository, FraudRepository>();

            // Customer history is read in a singleton evaluation service, so it is registered as a singleton
            var historySchema = builder.Configuration["Database:DBSchema"] ?? "fr";
            builder.Services.AddSingleton<ICustomerHistoryProvider>(sp =>
                new CustomerHistoryProvider(connectionString, historySchema,
                    sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<CustomerHistoryProvider>>()));
            builder.Services.AddSingleton<ReferenceDataCache>();
            builder.Services.AddScoped<IConfiguration>(p => builder.Configuration);
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
            builder.Services.AddAndValidateOptions<FraudRulesSettings>("FraudRules");

            builder.Services
                .AddSingleton<IFraudRule, HighAmountRule>()
                .AddSingleton<IFraudRule, ForeignCnpRule>()
                .AddSingleton<IFraudRule, AtmWithdrawalLimitRule>()
                .AddSingleton<IFraudRule, HighRiskMerchantCategoryRule>()
                .AddSingleton<IFraudRule, RoundAmountRule>();

            // Rules that also look at the customer's history
            builder.Services
                .AddSingleton<IHistoricalFraudRule, VelocityRule>()
                .AddSingleton<IHistoricalFraudRule, AmountDeviationRule>();

            // Register the fraud evaluation service
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
        }

        private static void RegisterAuthentication(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

            builder.Services.Configure<LoginSecuritySettings>(builder.Configuration.GetSection("LoginSecuritySettings"));
            builder.Services.AddSingleton<ILoginAttemptTracker, LoginAttemptTracker>();

            // Login and query logic live in the buss project; the controller only translates results to HTTP
            builder.Services.AddScoped<ILoginService, LoginService>();
            builder.Services.AddScoped<IFraudQueryService, FraudQueryService>();
            builder.Services.AddSingleton<IAuthenticationMetrics>(sp => sp.GetRequiredService<IMetricsService>());
        }

        private static void RegisterJwt(WebApplicationBuilder builder)
        {
            builder.Services.AddScoped<IJwtService, JwtService>();

            var jwtSettings = builder.Configuration.GetSection("JwtSettings");
            // The secret is never stored in appsettings. It comes from JwtSettings__Secret (environment variable)
            // or, locally, from secrets.json. HS256 needs a key of at least 256 bits (32 bytes).
            var secret = jwtSettings["Secret"];
            if (string.IsNullOrWhiteSpace(secret))
                throw new InvalidOperationException("JWT secret is not configured. Set JwtSettings__Secret (environment variable) or JwtSettings:Secret in secrets.json.");

            var key = System.Text.Encoding.UTF8.GetBytes(secret);
            if (key.Length < 32)
                throw new InvalidOperationException("JWT secret must be at least 32 bytes long.");

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
            if (ExtensionsHelper.IsDebugMode)
            {
                builder.Services.AddTransient<LoadSimulatorController>();
                builder.Services.AddTransient<FraudController>();
            }
        }

        private static void RegisterMetrics(WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<IMetricsService, MetricsService>();
            builder.Services.AddHttpClient();
        }
    }
}
