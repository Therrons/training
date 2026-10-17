using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace fraud_poc_project.Services
{
    /// <summary>
    /// Discovers pod endpoints for metrics aggregation using configuration or environment variables.
    /// Solution 1: Cross-Pod Aggregation - Simple, no Kubernetes API required.
    /// Supports both local development (localhost) and Kubernetes deployments (service DNS or hardcoded IPs).
    /// </summary>
    public interface IPodDiscoveryService
    {
        Task<List<PodInfo>> DiscoverPodsAsync(string appLabel = "fraud-poc-api");
        Task<string> GetCurrentPodNameAsync();
        Task<string> GetPodNamespaceAsync();
    }

    public class PodInfo
    {
        public string Name { get; set; }
        public string PodIP { get; set; }
        public string Namespace { get; set; }
        public int Port { get; set; } = 8080;
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Cross-pod aggregation service using configuration-based pod discovery.
    /// Works in three modes:
    /// 1. Local development: Uses localhost
    /// 2. Kubernetes with service DNS: Uses fraud-api-service.default.svc.cluster.local
    /// 3. Kubernetes with hardcoded pods: Uses environment-configured pod endpoints
    /// </summary>
    public class KubernetesPodDiscoveryService : IPodDiscoveryService
    {
        private readonly ILogger<KubernetesPodDiscoveryService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _currentPodName;
        private readonly string _podNamespace;
        private readonly string _discoveryMode;

        public KubernetesPodDiscoveryService(
            ILogger<KubernetesPodDiscoveryService> logger,
            IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Get pod identification from environment
            _currentPodName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "local-pod";
            _podNamespace = Environment.GetEnvironmentVariable("POD_NAMESPACE")
                ?? configuration["Kubernetes:Namespace"]
                ?? "default";

            // Determine discovery mode from configuration
            _discoveryMode = configuration["MetricsAggregation:DiscoveryMode"]
                ?? Environment.GetEnvironmentVariable("DISCOVERY_MODE")
                ?? "local";

            _logger.LogInformation($"Pod discovery initialized - Mode: {_discoveryMode}, Pod: {_currentPodName}, Namespace: {_podNamespace}");
        }

        public async Task<string> GetCurrentPodNameAsync()
        {
            return await Task.FromResult(_currentPodName);
        }

        public async Task<string> GetPodNamespaceAsync()
        {
            return await Task.FromResult(_podNamespace);
        }

        /// <summary>
        /// Discovers pods based on configured discovery mode:
        /// - "local": Returns localhost endpoint
        /// - "service": Uses Kubernetes service DNS name
        /// - "hardcoded": Uses comma-separated POD_ENDPOINTS environment variable
        /// </summary>
        public async Task<List<PodInfo>> DiscoverPodsAsync(string appLabel = "fraud-poc-api")
        {
            return _discoveryMode switch
            {
                "service" => await DiscoverViaServiceDnsAsync(),
                "hardcoded" => await DiscoverViaHardcodedEndpointsAsync(),
                _ => await DiscoverLocalAsync()
            };
        }

        /// <summary>
        /// Local mode: Single localhost endpoint for development
        /// </summary>
        private async Task<List<PodInfo>> DiscoverLocalAsync()
        {
            _logger.LogInformation("Pod discovery mode: LOCAL - Using localhost");

            return await Task.FromResult(new List<PodInfo>
            {
                new PodInfo
                {
                    Name = _currentPodName,
                    PodIP = "localhost",
                    Namespace = _podNamespace,
                    Port = 8080,
                    Status = "Running",
                    CreatedAt = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// Service DNS mode: Uses Kubernetes service discovery
        /// Requires: MetricsAggregation:ServiceName config (e.g., "fraud-api-service")
        /// In Kubernetes, this resolves to: service-name.namespace.svc.cluster.local
        /// </summary>
        private async Task<List<PodInfo>> DiscoverViaServiceDnsAsync()
        {
            _logger.LogInformation("Pod discovery mode: SERVICE - Using Kubernetes service DNS");

            var serviceName = _configuration["MetricsAggregation:ServiceName"]
                ?? Environment.GetEnvironmentVariable("FRAUD_API_SERVICE_NAME")
                ?? "fraud-api-service";

            var serviceHost = $"{serviceName}.{_podNamespace}.svc.cluster.local";
            var port = int.Parse(_configuration["MetricsAggregation:PodPort"] ?? "8080");

            _logger.LogInformation($"Connecting to service: {serviceHost}:{port}");

            // Service DNS returns all pod IPs, we treat it as a single endpoint
            return await Task.FromResult(new List<PodInfo>
            {
                new PodInfo
                {
                    Name = serviceName,
                    PodIP = serviceHost,
                    Namespace = _podNamespace,
                    Port = port,
                    Status = "Running",
                    CreatedAt = DateTime.UtcNow
                }
            });
        }

        /// <summary>
        /// Hardcoded mode: Uses comma-separated pod endpoints from configuration
        /// Configuration format: "pod1:10.42.0.10:8080,pod2:10.42.0.20:8080,pod3:10.42.0.25:8080"
        /// Environment variable: POD_ENDPOINTS
        /// Configuration key: MetricsAggregation:PodEndpoints
        /// </summary>
        private async Task<List<PodInfo>> DiscoverViaHardcodedEndpointsAsync()
        {
            _logger.LogInformation("Pod discovery mode: HARDCODED - Using configured pod endpoints");

            var podEndpoints = _configuration["MetricsAggregation:PodEndpoints"]
                ?? Environment.GetEnvironmentVariable("POD_ENDPOINTS")
                ?? "fraud-api-pod-1:localhost:8080";

            var pods = new List<PodInfo>();
            var endpointsList = podEndpoints.Split(',', StringSplitOptions.RemoveEmptyEntries);

            foreach (var endpoint in endpointsList)
            {
                var parts = endpoint.Trim().Split(':');
                if (parts.Length != 3)
                {
                    _logger.LogWarning($"Invalid pod endpoint format: {endpoint}. Expected: podName:podIP:port");
                    continue;
                }

                var podName = parts[0];
                var podIP = parts[1];
                if (!int.TryParse(parts[2], out var port))
                {
                    _logger.LogWarning($"Invalid port in pod endpoint: {endpoint}");
                    continue;
                }

                pods.Add(new PodInfo
                {
                    Name = podName,
                    PodIP = podIP,
                    Namespace = _podNamespace,
                    Port = port,
                    Status = "Running",
                    CreatedAt = DateTime.UtcNow
                });

                _logger.LogInformation($"Discovered pod: {podName} at {podIP}:{port}");
            }

            if (pods.Count == 0)
            {
                _logger.LogWarning("No valid pod endpoints configured, falling back to localhost");
                return await DiscoverLocalAsync();
            }

            _logger.LogInformation($"Pod discovery complete: found {pods.Count} pods");
            return await Task.FromResult(pods);
        }
    }
}
