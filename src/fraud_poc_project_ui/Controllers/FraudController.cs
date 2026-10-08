using fraud_poc_project.CustomAttributes;
using fraud_poc_project.Exceptions;
using fraud_poc_project.Services;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace fraud_poc_project.Controllers
{
    [ValidateXss]
    [ApiController]
    [Route("api/fraud")]
    public class FraudController : ControllerBase
    {
        private readonly IFraudRepository _repository;
        private readonly IJwtService _jwtService;
        private readonly IMetricsService _metricsService;
        private readonly ILogger<FraudController> _logger;

        public FraudController(
            IFraudRepository repository,
            IJwtService jwtService,
            IMetricsService metricsService,
            ILogger<FraudController> logger)
        {
            _repository = repository;
            _jwtService = jwtService;
            _metricsService = metricsService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate with username and password. Returns JWT token for use in Authorization header.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request?.Password))
                {
                    _logger.LogWarning("Login attempt with missing credentials");
                    _metricsService.RecordAuthenticationAttempt(false);
                    return BadRequest(new { error = "Username and password required" });
                }

                var token = _jwtService.GenerateToken(request.Username, request.Password);

                if (token == null)
                {
                    _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                    _metricsService.RecordAuthenticationAttempt(false);
                    return Unauthorized(new { error = "Invalid credentials" });
                }

                _logger.LogInformationOnly("Successful login for username: {Username}", request.Username);
                _metricsService.RecordAuthenticationAttempt(true);
                return Ok(new
                {
                    token.Value.token,
                    validFrom = token.Value.validFrom.ToShortTimeString(),
                    validTo = token.Value.validTo.ToShortTimeString(),
                    expiresIn = string.Concat(3600 / 60, " minutes"),
                    tokenType = "Bearer"
                });
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication error for username: {Username}", request?.Username);
                _metricsService.RecordAuthenticationAttempt(false);
                return Unauthorized(new { error = "Authentication failed", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return StatusCode(500, new { error = "An unexpected error occurred during authentication" });
            }
        }

        /// <summary>
        /// Query fraud events by date range and filters. Date format: yyyy-MM-dd HH:mm:ss
        /// </summary>

        [HttpGet("events")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _repository.QueryFraudEventsAsync(query);
            return Ok(records);
        }

        /// <summary>
        /// Get all rule results for a fraud event.
        /// </summary>
        [HttpGet("events/{fraudEventId:long}/rules")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FraudRuleSetRecord>>> GetRuleResults(long fraudEventId)
        {
            var records = await _repository.GetRuleResultsForEventAsync(fraudEventId);
            return Ok(records);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
