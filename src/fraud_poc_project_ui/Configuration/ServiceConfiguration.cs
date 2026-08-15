using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using fraud_poc_project.Controllers;
using fraud_poc_project.Kafka.Consumer;
using fraud_poc_project.Settings;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Models.Database;
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
using System;
using System.Collections.Generic;
using System.Text;

namespace fraud_poc_project.Configuration
{
    public static class ServiceConfiguration
    {
        public static void AddServices_AddDI(this WebApplicationBuilder builder)
        {
            var awsOptions = new AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName(
                builder.Configuration["AWSRegion"] ?? "af-south-1")
            };

            builder.Services.AddAWSService<IAmazonSecretsManager>(awsOptions)
                .AddSingleton<AWSSecretsConfiguration>();

            // ── Bind AppSettings so Kafka configuration can resolve it ──
            builder.Services.AddOptions<AppSettings>()
               .BindConfiguration("AppSettings")
               .ValidateDataAnnotations()
               .ValidateOnStart();

            builder.Services.AddOptions<Database>()
              .BindConfiguration("Database")
              .ValidateDataAnnotations()
              .ValidateOnStart();

            var envVariables = new Environment_Variables().Get_Environment_Values(builder);
            var isLocal = builder.Environment.EnvironmentName.Contains("loc", StringComparison.InvariantCultureIgnoreCase);

            // build db connection string from environment variables and add it to configuration
            StringBuilder connectionString = new StringBuilder();
            connectionString.Append($"Server={envVariables.DBHost};");
            connectionString.Append($"Database={envVariables.DBName};");
            connectionString.Append($"User Id={envVariables.DBUsername};");
            connectionString.Append($"Password={envVariables.DBPassword};");
            connectionString.Append($"Pooling=true;");
            connectionString.Append($"Connection Lifetime=0;");
            connectionString.Append(!isLocal ? "SSLMode=Require;" : "SSLMode=Disable;");
            connectionString.Append("Trust Server Certificate = true;");

            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:PostgreSQL"] = connectionString.ToString()
            });

            // ── Add db functionality ───
            builder.Services.AddDbContextPool<IDBConnection, DBConnection>(options => options.UseNpgsql(connectionString.ToString()));
            builder.Services.AddScoped<ITransactionEventHandler, FraudBatchConsumerWorker>();
            builder.Services.AddScoped<IFraudRepository, FraudRepository>();
            builder.Services.AddScoped<IConfiguration>(p => builder.Configuration);

                        // ── Fraud rules ───
                        builder.Services
                            .AddSingleton<IFraudRule, HighAmountRule>()
                            .AddSingleton<IFraudRule, ForeignCnpRule>()
                            .AddSingleton<IFraudRule, AtmWithdrawalLimitRule>()
                            .AddSingleton<IFraudRule, HighRiskMerchantCategoryRule>()
                            .AddSingleton<IFraudRule, UnusualHoursRule>()
                            .AddSingleton<IFraudRule, RoundAmountRule>();

                        builder.Services
                            .AddSingleton<IFraudEvaluationService, FraudEvaluationService>()
                            .AddTransient<FraudBatchConsumerWorker>();

            // this is only for testing purposes
#if DEBUG
            builder.Services.AddTransient<UserInputController>();
#endif

            builder.Services.AddKafkaConfigurations(builder.Configuration)
               .KafkaSetupTopics();

            // setup instances that use the above kafka classes via DI
            builder.Services.AddSingleton<IFraudProducer, FraudProducer>();
            builder.Services.AddSingleton<FraudConsumer>();
            builder.Services.AddHostedService(sp => sp.GetRequiredService<FraudConsumer>());

            
            builder.ConfigureSwagger();
        }
    }
}
