using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using fraud_poc_project.Controllers;
using fraud_poc_project.Kafka.Consumer;
using fraud_poc_project.Settings;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Models.Database;
using fraud_poc_project_buss.Models.Kafka;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Connection;
using fraud_poc_project_repo.Interfaces;
using fraud_poc_project_repo.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;

namespace fraud_poc_project.Configuration
{
    // This is where the app tells its Dependency Injection container about every
    // service it needs: AWS secrets, app settings, the database, the fraud rules,
    // and Kafka. Each piece is split into its own small method below so it's easy to
    // see, at a glance, everything the app is wired up to use.
    public static class ServiceConfiguration
    {
        public static void AddServices_AddDI(this WebApplicationBuilder builder)
        {
            RegisterAwsSecrets(builder);
            RegisterAppSettings(builder);

            var connectionString = BuildDatabaseConnectionString(builder);
            RegisterDatabaseAccess(builder, connectionString);
            RegisterFraudRules(builder);
            RegisterTestOnlyControllers(builder);
            RegisterRetryPipeline(builder);
            RegisterKafka(builder);

            builder.ConfigureSwagger();
        }

        private static void RegisterRetryPipeline(WebApplicationBuilder builder)
        {
            builder.Services.AddResiliencePipeline("exception", x =>
            {
                x.AddRetry(
                    new RetryStrategyOptions
                    {
                        BackoffType = DelayBackoffType.Constant,
                        Delay = TimeSpan.FromMilliseconds(15),
                        MaxRetryAttempts = 2,
                        UseJitter = true,
                        ShouldHandle = new PredicateBuilder().Handle<Exception>()
                    });
            });
        }

        // Lets the app fetch secrets (like database passwords) from AWS Secrets Manager.
        private static void RegisterAwsSecrets(WebApplicationBuilder builder)
        {
            var awsOptions = new AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName(
                    builder.Configuration["AWSRegion"] ?? "af-south-1")
            };

            builder.Services.AddAWSService<IAmazonSecretsManager>(awsOptions)
                .AddSingleton<AWSSecretsConfiguration>();
        }

        // Loads AppSettings and Database settings from config (e.g. appsettings.json)
        // and checks that all their required fields are filled in.
        private static void RegisterAppSettings(WebApplicationBuilder builder)
        {
            // AppSettings is bound here so the Kafka configuration (added later) can resolve it.
            builder.Services.AddAndValidateOptions<AppSettings>("AppSettings");
            builder.Services.AddAndValidateOptions<Database>("Database");
        }

        // Builds the PostgreSQL connection string from environment variables (falling
        // back to config values), then adds it to the app's configuration so anything
        // that asks for "ConnectionStrings:PostgreSQL" can find it.
        private static string BuildDatabaseConnectionString(WebApplicationBuilder builder)
        {
            var envVariables = new Environment_Variables().Get_Environment_Values(builder);
            var isLocal = builder.Environment.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);

            var connectionString = new StringBuilder();
            connectionString.Append($"Server={envVariables.DBHost};");
            connectionString.Append($"Database={envVariables.DBName};");
            connectionString.Append($"User Id={envVariables.DBUsername};");
            connectionString.Append($"Password={envVariables.DBPassword};");
            connectionString.Append($"Pooling=true;");
            connectionString.Append($"Connection Lifetime=0;");
            connectionString.Append(!isLocal ? "SSLMode=Require;" : "SSLMode=Disable;");
            connectionString.Append("Trust Server Certificate = true;");

            var result = connectionString.ToString();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = result
            });

            return result;
        }

        // Registers everything needed to read from and write to the database.
        private static void RegisterDatabaseAccess(WebApplicationBuilder builder, string connectionString)
        {
            builder.Services.AddDbContextPool<IDBConnection, DBConnection>(options => options.UseNpgsql(connectionString));
            builder.Services.AddScoped<IFraudRepository, FraudRepository>();
            builder.Services.AddScoped<IConfiguration>(p => builder.Configuration);
        }

        // Registers every fraud rule (see FraudRules.cs) and the service that runs them.
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

        // Registers Kafka configuration, creates any missing topics, and wires up the
        // producer/consumer that use them.
        private static void RegisterKafka(WebApplicationBuilder builder)
        {
            builder.Services.AddKafkaConfigurations(builder.Configuration)
               .KafkaSetupTopics();

            builder.Services.AddSingleton<IFraudProducer, FraudProducer>();
            builder.Services.AddSingleton<FraudConsumer>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FraudConsumer>());
            builder.Services.AddTransient<ITransactionEventHandler, FraudConsumerWorker>();
        }

        // UserInputController only exists to help with manual testing, so it's only
        // registered in Debug builds.
        private static void RegisterTestOnlyControllers(WebApplicationBuilder builder)
        {
            if (Extensions_Helper.IsDebugMode)
            {
                //builder.Services.AddTransient<UserInputController>();
                builder.Services.AddTransient<LoadSimulatorController>();
                builder.Services.AddTransient<FraudController>();
            }
        }
    }
}
