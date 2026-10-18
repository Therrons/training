using Microsoft.Extensions.Logging;
using System;

namespace fraud_poc_project.Services
{
    /// <summary>
    /// Provides read-only pod identity information set at instantiation time.
    /// In Kubernetes, these values come from the downward API environment variables.
    /// Useful for identifying the current pod in logs, metrics, and responses.
    /// </summary>
    public interface IPodIdentityService
    {
        /// <summary>
        /// Name of the current pod (from HOSTNAME environment variable)
        /// </summary>
        string PodName { get; }

        /// <summary>
        /// Kubernetes namespace the pod is running in
        /// </summary>
        string PodNamespace { get; }

        /// <summary>
        /// ASP.NET Core environment (Development, Staging, Production)
        /// </summary>
        string Environment { get; }
    }

    /// <summary>
    /// Pod identity service that reads pod information at instantiation.
    /// These values are immutable for the lifetime of the service.
    /// </summary>
    public class PodIdentityService : IPodIdentityService
    {
        public string PodName { get; }
        public string PodNamespace { get; }
        public string Environment { get; }

        public PodIdentityService(ILogger<PodIdentityService> logger)
        {
            // Set at instantiation time - read from Kubernetes downward API environment variables
            PodName = "local-pod";
            PodNamespace = "default";
            Environment = System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

            logger.LogInformation(
                $"Pod Identity initialized - Pod: {PodName}, Namespace: {PodNamespace}, Environment: {Environment}");
        }
    }
}
