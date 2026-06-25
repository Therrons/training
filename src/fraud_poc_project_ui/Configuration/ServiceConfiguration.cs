using Amazon.Extensions.NETCore.Setup;
using Amazon.SecretsManager;
using fraud_poc_project.Fraud.Services;
using fraud_poc_project_buss;
using fraud_poc_project_repo;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace fraud_poc_project.Configuration
{
    public static class ServiceConfiguration
    {
        public static IServiceCollection AddServices(this IServiceCollection services, WebApplicationBuilder builder)
        {
            var awsOptions = new AWSOptions
            {
                Region = Amazon.RegionEndpoint.GetBySystemName(
                builder.Configuration["AWSRegion"] ?? "af-south-1")
            };

            builder.Services.AddAWSService<IAmazonSecretsManager>(awsOptions)
                .AddSingleton<SecretsConfiguration>();


            // ── Fraud rules ───────────────────────────────────────────────────────
            builder.Services
                .AddSingleton<IFraudRule, HighAmountRule>()
                .AddSingleton<IFraudRule, ForeignCnpRule>()
                .AddSingleton<IFraudRule, AtmWithdrawalLimitRule>()
                .AddSingleton<IFraudRule, HighRiskMerchantCategoryRule>()
                .AddSingleton<IFraudRule, UnusualHoursRule>()
                .AddSingleton<IFraudRule, RoundAmountRule>();

            builder.Services
                .AddSingleton<IFraudEvaluationService, FraudEvaluationService>()
                .AddScoped<IFraudRepository, FraudRepository>();

            builder.Services.AddHostedService<KafkaConsumerService>();

            return services;
        }
    }
}
