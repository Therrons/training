using Microsoft.Extensions.DependencyInjection;


namespace fraud_poc_project_buss.Configuration
{
    /// <summary>
    /// Extension methods for simplifying options configuration registration.
    /// Eliminates boilerplate by consolidating AddOptions, BindConfiguration, and validation.
    /// </summary>
    public static class ConfigurationExtensions
    {
        /// <summary>
        /// Registers and configures options with automatic binding from configuration,
        /// data annotation validation, and startup validation.
        /// </summary>
        /// <typeparam name="T">The options class to register</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="configurationSection">The configuration section name to bind to</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddAndValidateOptions<T>(
            this IServiceCollection services,
            string configurationSection) where T : class
        {
            services.AddOptions<T>()
                .BindConfiguration(configurationSection)
                .ValidateDataAnnotations()
                .ValidateOnStart();

            return services;
        }
    }
}
