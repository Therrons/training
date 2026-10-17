using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using fraud_poc_project.Services;
using fraud_poc_project_buss.Helper;

namespace fraud_poc_project.Services
{
    /// <summary>
    /// Aggregates metrics from all pods discovered via IPodDiscoveryService.
    /// Queries each pod's local metrics endpoint and combines results.
    /// </summary>
    public interface IMetricsAggregatorService
    {
        Task<AggregatedMetrics> GetAggregatedMetricsAsync();
        Task<List<PodMetricsSnapshot>> GetPerPodMetricsAsync();
        Task<AggregatedMetrics> GetAggregatedMetricsWithTimeoutAsync(TimeSpan timeout);
    }

    public class PodMetricsSnapshot
    {
        public string PodName { get; set; }
        public string Status { get; set; }
        public DateTime Timestamp { get; set; }
        public MetricsSnapshot Metrics { get; set; }
        public long? ResponseTimeMs { get; set; }
        public bool? IsHealthy { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class AggregatedMetrics
    {
        public DateTime AggregatedAt { get; set; }
        public int TotalPods { get; set; }
        public int HealthyPods { get; set; }
        public int UnhealthyPods { get; set; }

        // Aggregated transaction statistics
        public long TotalTransactions { get; set; }
        public long FlaggedTransactions { get; set; }
        public decimal AverageFraudRate { get; set; }
        public decimal AverageFraudScore { get; set; }

        // Aggregated performance metrics
        public long AverageProcessingTime { get; set; }
        public long MaxProcessingTime { get; set; }
        public long MinProcessingTime { get; set; }
        public long AverageP95ProcessingTime { get; set; }
        public long AverageP99ProcessingTime { get; set; }

        // Aggregated authentication metrics
        public long TotalAuthenticationAttempts { get; set; }
        public long TotalSuccessfulAuthentications { get; set; }
        public decimal AverageAuthenticationSuccessRate { get; set; }

        // Aggregated rule execution metrics
        public int TotalRulesExecuted { get; set; }
        public Dictionary<string, AggregatedRuleMetrics> RuleMetrics { get; set; } = new();

        // Per-pod breakdown
        public List<PodMetricsSnapshot> PodMetrics { get; set; } = new();

        public decimal OverallFraudRate => TotalTransactions > 0
            ? (decimal)FlaggedTransactions / TotalTransactions
            : 0;
    }

    public class AggregatedRuleMetrics
    {
        public string RuleCode { get; set; }
        public long TotalExecutionCount { get; set; }
        public long TotalTriggeredCount { get; set; }
        public long AverageExecutionTime { get; set; }
        public decimal AverageTriggeredRate { get; set; }
    }

    public class MetricsAggregatorService : IMetricsAggregatorService
    {
        private readonly IPodDiscoveryService _podDiscoveryService;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IPodIdentityService _podIdentity;
        private readonly ILogger<MetricsAggregatorService> _logger;

        public MetricsAggregatorService(
            IPodDiscoveryService podDiscoveryService,
            IHttpClientFactory httpClientFactory,
            IPodIdentityService podIdentity,
            ILogger<MetricsAggregatorService> logger)
        {
            _podDiscoveryService = podDiscoveryService;
            _httpClientFactory = httpClientFactory;
            _podIdentity = podIdentity;
            _logger = logger;
        }

        /// <summary>
        /// Get metrics from all pods and aggregate them (default 30 second timeout)
        /// </summary>
        public async Task<AggregatedMetrics> GetAggregatedMetricsAsync()
        {
            return await GetAggregatedMetricsWithTimeoutAsync(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Get aggregated metrics with custom timeout
        /// </summary>
        public async Task<AggregatedMetrics> GetAggregatedMetricsWithTimeoutAsync(TimeSpan timeout)
        {
            var aggregated = new AggregatedMetrics
            {
                AggregatedAt = DateTime.UtcNow
            };

            try
            {
                // Get per-pod metrics with timeout
                var perPodMetrics = await GetPerPodMetricsWithTimeoutAsync(timeout);
                aggregated.PodMetrics = perPodMetrics;
                aggregated.TotalPods = perPodMetrics.Count;
                aggregated.HealthyPods = perPodMetrics.Count(m => m.IsHealthy == true);
                aggregated.UnhealthyPods = aggregated.TotalPods - aggregated.HealthyPods;

                _logger.LogInformation($"[{_podIdentity.PodName}] Aggregating metrics from {aggregated.HealthyPods}/{aggregated.TotalPods} healthy pods");

                // Aggregate statistics
                long totalTransactions = 0;
                long flaggedTransactions = 0;
                List<long> avgProcessingTimes = new();
                List<long> maxProcessingTimes = new();
                List<long> minProcessingTimes = new();
                List<long> p95ProcessingTimes = new();
                List<long> p99ProcessingTimes = new();

                long totalAuthAttempts = 0;
                long totalSuccessfulAuth = 0;
                List<decimal> authSuccessRates = new();

                var ruleMetricsDict = new Dictionary<string, (long execCount, long triggeredCount, long totalTime, List<decimal> rates)>();

                foreach (var podMetric in perPodMetrics.Where(m => m.Metrics != null && m.IsHealthy == true))
                {
                    try
                    {
                        var snapshot = podMetric.Metrics;

                        // Aggregate transaction metrics
                        totalTransactions += snapshot.TotalTransactions;
                        flaggedTransactions += snapshot.FlaggedTransactions;

                        // Aggregate processing times
                        avgProcessingTimes.Add(snapshot.AvgProcessingTime);
                        maxProcessingTimes.Add(snapshot.MaxProcessingTime);
                        minProcessingTimes.Add(snapshot.MinProcessingTime);
                        p95ProcessingTimes.Add(snapshot.P95ProcessingTime);
                        p99ProcessingTimes.Add(snapshot.P99ProcessingTime);

                        // Aggregate authentication metrics
                        totalAuthAttempts += snapshot.TotalAuthenticationAttempts;
                        totalSuccessfulAuth += snapshot.SuccessfulAuthentications;
                        authSuccessRates.Add(snapshot.AuthenticationSuccessRate);

                        // Aggregate rule metrics
                        foreach (var rule in snapshot.RuleMetrics)
                        {
                            if (!ruleMetricsDict.ContainsKey(rule.RuleCode))
                            {
                                ruleMetricsDict[rule.RuleCode] = (0, 0, 0, new List<decimal>());
                            }

                            var current = ruleMetricsDict[rule.RuleCode];
                            current.execCount += rule.ExecutionCount;
                            current.triggeredCount += rule.TriggeredCount;
                            current.totalTime += rule.TotalExecutionTime;
                            current.rates.Add(rule.TriggeredRate);
                            ruleMetricsDict[rule.RuleCode] = current;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Error aggregating metrics from pod {podMetric.PodName}");
                    }
                }

                // Set aggregated transaction metrics
                aggregated.TotalTransactions = totalTransactions;
                aggregated.FlaggedTransactions = flaggedTransactions;
                aggregated.AverageFraudRate = aggregated.OverallFraudRate;
                aggregated.AverageFraudScore = aggregated.TotalTransactions > 0
                    ? perPodMetrics
                        .Where(m => m.Metrics != null && m.IsHealthy == true)
                        .Average(m => m.Metrics.AvgFraudScore)
                    : 0;

                // Set aggregated performance metrics
                aggregated.AverageProcessingTime = avgProcessingTimes.Count > 0 ? (long)avgProcessingTimes.Average() : 0;
                aggregated.MaxProcessingTime = maxProcessingTimes.Count > 0 ? maxProcessingTimes.Max() : 0;
                aggregated.MinProcessingTime = minProcessingTimes.Count > 0 ? minProcessingTimes.Min() : 0;
                aggregated.AverageP95ProcessingTime = p95ProcessingTimes.Count > 0 ? (long)p95ProcessingTimes.Average() : 0;
                aggregated.AverageP99ProcessingTime = p99ProcessingTimes.Count > 0 ? (long)p99ProcessingTimes.Average() : 0;

                // Set aggregated authentication metrics
                aggregated.TotalAuthenticationAttempts = totalAuthAttempts;
                aggregated.TotalSuccessfulAuthentications = totalSuccessfulAuth;
                aggregated.AverageAuthenticationSuccessRate = authSuccessRates.Count > 0
                    ? authSuccessRates.Average()
                    : 0;

                // Set aggregated rule metrics
                aggregated.TotalRulesExecuted = ruleMetricsDict.Count;
                foreach (var kvp in ruleMetricsDict)
                {
                    var rule = kvp.Value;
                    aggregated.RuleMetrics[kvp.Key] = new AggregatedRuleMetrics
                    {
                        RuleCode = kvp.Key,
                        TotalExecutionCount = rule.execCount,
                        TotalTriggeredCount = rule.triggeredCount,
                        AverageExecutionTime = rule.execCount > 0 ? rule.totalTime / rule.execCount : 0,
                        AverageTriggeredRate = rule.rates.Count > 0 ? rule.rates.Average() : 0
                    };
                }

                _logger.LogInformation(
                    $"[{_podIdentity.PodName}] Aggregation complete: {aggregated.TotalTransactions} txns, " +
                    $"{aggregated.FlaggedTransactions} flagged, " +
                    $"avg latency: {aggregated.AverageProcessingTime}ms");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during metrics aggregation");
            }

            return aggregated;
        }

        /// <summary>
        /// Get metrics from each pod individually
        /// </summary>
        public async Task<List<PodMetricsSnapshot>> GetPerPodMetricsAsync()
        {
            return await GetPerPodMetricsWithTimeoutAsync(TimeSpan.FromSeconds(30));
        }

        /// <summary>
        /// Get per-pod metrics with custom timeout
        /// </summary>
        private async Task<List<PodMetricsSnapshot>> GetPerPodMetricsWithTimeoutAsync(TimeSpan timeout)
        {
            var pods = await _podDiscoveryService.DiscoverPodsAsync();

            _logger.LogInformationOnly($"[{_podIdentity.PodName}] The pods picked up for metrics aggregation are: " + string.Join(", ", pods.Select(p => p.Name)));

            var client = _httpClientFactory.CreateClient();
            client.Timeout = timeout;

            var tasks = pods.Select(pod => QueryPodMetricsAsync(pod, client)).ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Some pod metric queries timed out or failed");
            }

            return tasks
                .Where(t => t.IsCompletedSuccessfully)
                .Select(t => t.Result)
                .Where(m => m != null)
                .ToList();
        }

        /// <summary>
        /// Query a single pod for its metrics snapshot
        /// </summary>
        private async Task<PodMetricsSnapshot> QueryPodMetricsAsync(PodInfo pod, HttpClient client)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var url = $"http://{pod.PodIP}:{pod.Port}/api/metrics/static";
                _logger.LogDebug($"[{_podIdentity.PodName}] Querying pod metrics: {url}");

                _logger.LogInformationOnly($"[{_podIdentity.PodName}] Getting statistics for pod", JsonConvert.SerializeObject(pod));

                var response = await client.GetAsync(url);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var metrics = JsonConvert.DeserializeObject<MetricsSnapshot>(json);

                    return new PodMetricsSnapshot
                    {
                        PodName = pod.Name,
                        Status = pod.Status,
                        Timestamp = DateTime.UtcNow,
                        Metrics = metrics,
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        IsHealthy = true
                    };
                }
                else
                {
                    _logger.LogInformationOnly($"[{_podIdentity.PodName}] No statistics for pod", JsonConvert.SerializeObject(pod));

                    _logger.LogWarning($"[{_podIdentity.PodName}] Pod {pod.Name} returned status {response.StatusCode}");
                    return new PodMetricsSnapshot
                    {
                        PodName = pod.Name,
                        Status = "UnhealthyResponse",
                        ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                        IsHealthy = false,
                        ErrorMessage = $"HTTP {response.StatusCode}"
                    };
                }
            }
            catch (HttpRequestException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning($"[{_podIdentity.PodName}] Failed to query pod {pod.Name} at {pod.PodIP}: {ex.Message}");

                return new PodMetricsSnapshot
                {
                    PodName = pod.Name,
                    Status = "Unreachable",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    IsHealthy = false,
                    ErrorMessage = "Connection failed"
                };
            }
            catch (OperationCanceledException ex)
            {
                stopwatch.Stop();
                _logger.LogWarning($"[{_podIdentity.PodName}] Query to pod {pod.Name} timed out after {stopwatch.ElapsedMilliseconds}ms");

                return new PodMetricsSnapshot
                {
                    PodName = pod.Name,
                    Status = "Timeout",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    IsHealthy = false,
                    ErrorMessage = "Request timeout"
                };
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, $"[{_podIdentity.PodName}] Unexpected error querying pod {pod.Name}");

                return new PodMetricsSnapshot
                {
                    PodName = pod.Name,
                    Status = "Error",
                    ResponseTimeMs = stopwatch.ElapsedMilliseconds,
                    IsHealthy = false,
                    ErrorMessage = ex.Message
                };
            }
        }
    }
}
