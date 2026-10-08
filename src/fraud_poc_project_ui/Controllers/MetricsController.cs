using fraud_poc_project.CustomAttributes;
using fraud_poc_project.Services;
using fraud_poc_project_buss.Helper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace fraud_poc_project.Controllers
{
    /// <summary>
    /// Metrics API Controller
    /// Exposes fraud detection system metrics and monitoring endpoints
    ///
    /// Requires: Bearer token authentication (same as other protected endpoints)
    /// </summary>
    [ApiController]
    [ValidateXss]
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
        /// Get metrics snapshot (transactions, performance, rules, auth, database, Kafka).
        /// </summary>
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
        /// Get metrics as plain text (for monitoring dashboards).
        /// </summary>
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
        /// Get transaction volume metrics.
        /// </summary>
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
        /// Get performance metrics (avg, min, max, P95, P99).
        /// </summary>
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
        /// Get fraud rule execution metrics (per-rule stats).
        /// </summary>
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
        /// Get authentication metrics (login attempts and success rate).
        /// </summary>
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
        /// Get database operation metrics (counts and timings).
        /// </summary>
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
        /// Get Kafka operation metrics (produce/consume timings).
        /// </summary>
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
        /// Reset all metrics (testing only).
        /// </summary>
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
