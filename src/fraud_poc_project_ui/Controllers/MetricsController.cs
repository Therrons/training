using fraud_poc_project.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using fraud_poc_project_buss.Helper;

namespace fraud_poc_project.Controllers
{
    /// <summary>
    /// Metrics API Controller
    /// Exposes fraud detection system metrics and monitoring endpoints
    ///
    /// Requires: Bearer token authentication (same as other protected endpoints)
    /// </summary>
    [ApiController]
    [Route("api/metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly IMetricsService _metricsService;
        private readonly ILogger<MetricsController> _logger;

        public MetricsController(IMetricsService metricsService, ILogger<MetricsController> logger)
        {
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Get current system metrics snapshot
        /// </summary>
        /// <remarks>
        /// Returns comprehensive metrics including:
        /// - Transaction volume and fraud rates
        /// - Average processing times and percentiles
        /// - Authentication statistics
        /// - Per-rule execution metrics
        /// - Database and Kafka operation metrics
        ///
        /// Requires authentication: Include JWT token in Authorization header as "Bearer {token}"
        /// </remarks>
        /// <response code="200">Returns current metrics snapshot</response>
        /// <response code="401">Unauthorized - Missing or invalid JWT token</response>
        [HttpGet("snapshot")]
        [Authorize]
        [ProducesResponseType(typeof(MetricsSnapshot), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetMetricsSnapshot()
        {
            _logger.LogInformationOnly("Metrics snapshot requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(snapshot);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics snapshot");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve metrics" });
            }
        }

        /// <summary>
        /// Get metrics as formatted text
        /// </summary>
        /// <remarks>
        /// Returns metrics in human-readable format (text/plain)
        /// Useful for monitoring dashboards and log aggregation
        /// </remarks>
        /// <response code="200">Returns formatted metrics text</response>
        /// <response code="401">Unauthorized - Missing or invalid JWT token</response>
        [HttpGet("summary")]
        [Authorize]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetMetricsSummary()
        {
            _logger.LogInformationOnly("Metrics summary requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Content(snapshot.ToString(), "text/plain");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving metrics summary");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve metrics" });
            }
        }

        /// <summary>
        /// Get transaction volume metrics
        /// </summary>
        /// <remarks>
        /// Returns:
        /// - Total transactions processed
        /// - Number flagged as fraud
        /// - Fraud detection rate
        /// - Average fraud score
        /// </remarks>
        /// <response code="200">Returns transaction metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("transactions")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetTransactionMetrics()
        {
            _logger.LogInformationOnly("Transaction metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    total = snapshot.TotalTransactions,
                    flagged = snapshot.FlaggedTransactions,
                    fraudRate = snapshot.FraudRate,
                    avgFraudScore = snapshot.AvgFraudScore
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve transaction metrics" });
            }
        }

        /// <summary>
        /// Get performance metrics
        /// </summary>
        /// <remarks>
        /// Returns transaction processing time metrics:
        /// - Average, min, max processing times
        /// - P95 and P99 percentiles (important for SLAs)
        /// </remarks>
        /// <response code="200">Returns performance metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("performance")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetPerformanceMetrics()
        {
            _logger.LogInformationOnly("Performance metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    processingTimeMs = new
                    {
                        avg = snapshot.AvgProcessingTime,
                        min = snapshot.MinProcessingTime,
                        max = snapshot.MaxProcessingTime,
                        p95 = snapshot.P95ProcessingTime,
                        p99 = snapshot.P99ProcessingTime
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving performance metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve performance metrics" });
            }
        }

        /// <summary>
        /// Get fraud rule execution metrics
        /// </summary>
        /// <remarks>
        /// Returns per-rule statistics:
        /// - Number of times each rule was executed
        /// - Number of times triggered
        /// - Trigger rate (triggered / total executions)
        /// - Average execution time
        /// </remarks>
        /// <response code="200">Returns rule metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("rules")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetRuleMetrics()
        {
            _logger.LogInformationOnly("Rule metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    rules = snapshot.RuleMetrics.Select(r => new
                    {
                        ruleCode = r.RuleCode,
                        executionCount = r.ExecutionCount,
                        triggeredCount = r.TriggeredCount,
                        triggeredRate = r.TriggeredRate,
                        avgExecutionTimeMs = r.AvgExecutionTime
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving rule metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve rule metrics" });
            }
        }

        /// <summary>
        /// Get authentication metrics
        /// </summary>
        /// <remarks>
        /// Returns login statistics:
        /// - Total authentication attempts
        /// - Successful authentications
        /// - Success rate
        /// </remarks>
        /// <response code="200">Returns authentication metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("authentication")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetAuthenticationMetrics()
        {
            _logger.LogInformationOnly("Authentication metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    totalAttempts = snapshot.TotalAuthenticationAttempts,
                    successful = snapshot.SuccessfulAuthentications,
                    successRate = snapshot.AuthenticationSuccessRate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving authentication metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve authentication metrics" });
            }
        }

        /// <summary>
        /// Get database operation metrics
        /// </summary>
        /// <remarks>
        /// Returns database performance metrics:
        /// - Operation counts per type (SELECT, INSERT, UPDATE, etc.)
        /// - Total and average execution times
        /// </remarks>
        /// <response code="200">Returns database metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("database")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetDatabaseMetrics()
        {
            _logger.LogInformationOnly("Database metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    operations = snapshot.DatabaseOperationMetrics.Select(m => new
                    {
                        operationType = m.OperationType,
                        count = m.Count,
                        totalTimeMs = m.TotalTime,
                        avgTimeMs = m.AvgTime
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving database metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve database metrics" });
            }
        }

        /// <summary>
        /// Get Kafka operation metrics
        /// </summary>
        /// <remarks>
        /// Returns Kafka event metrics:
        /// - Event counts per type (produce, consume)
        /// - Total and average operation times
        /// </remarks>
        /// <response code="200">Returns Kafka metrics</response>
        /// <response code="401">Unauthorized</response>
        [HttpGet("kafka")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult GetKafkaMetrics()
        {
            _logger.LogInformationOnly("Kafka metrics requested");

            try
            {
                var snapshot = _metricsService.GetSnapshot();
                return Ok(new
                {
                    timestamp = snapshot.Timestamp,
                    operations = snapshot.KafkaOperationMetrics.Select(m => new
                    {
                        operationType = m.OperationType,
                        count = m.Count,
                        totalTimeMs = m.TotalTime,
                        avgTimeMs = m.AvgTime
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving Kafka metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to retrieve Kafka metrics" });
            }
        }

        /// <summary>
        /// Reset all metrics (Admin only)
        /// </summary>
        /// <remarks>
        /// Clears all collected metrics. Should only be called for testing or
        /// when starting a new monitoring period.
        /// </remarks>
        /// <response code="200">Metrics reset successfully</response>
        /// <response code="401">Unauthorized</response>
        [HttpPost("reset")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public IActionResult ResetMetrics()
        {
            _logger.LogWarning("Metrics reset requested - clearing all data");

            try
            {
                _metricsService.Reset();
                return Ok(new { message = "Metrics reset successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting metrics");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Failed to reset metrics" });
            }
        }
    }
}
