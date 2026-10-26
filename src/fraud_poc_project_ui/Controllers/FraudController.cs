using fraud_poc_project.CustomAttributes;
using fraud_poc_project.Exceptions;
using fraud_poc_project.Interfaces;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly ILoginAttemptTracker _loginTracker;

        public FraudController(
            IFraudRepository repository,
            IJwtService jwtService,
            IMetricsService metricsService,
            ILogger<FraudController> logger,
            ILoginAttemptTracker loginTracker)
        {
            _repository = repository;
            _jwtService = jwtService;
            _metricsService = metricsService;
            _logger = logger;
            _loginTracker = loginTracker;
        }

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

                if (_loginTracker.IsLockedOut(request.Username, out var lockedUntil))
                {
                    var retryAfterSeconds = Math.Max(1, (int)Math.Ceiling((lockedUntil - DateTimeOffset.UtcNow).TotalSeconds));

                    retryAfterSeconds = retryAfterSeconds > 60 ? retryAfterSeconds : 60; // Ensure a minimum of 60 seconds  

                    Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
                    _logger.LogWarning("Login blocked, account temporarily locked for username: {Username}", request.Username);
                    _metricsService.RecordAuthenticationAttempt(false);
                    return StatusCode(429, new { error = $"Account temporarily locked due to repeated failed logins. Try again after {retryAfterSeconds / 60} minute/s." });
                }

                var token = _jwtService.GenerateToken(request.Username, request.Password);

                if (token == null)
                {
                    _loginTracker.RecordFailure(request.Username);
                    _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                    _metricsService.RecordAuthenticationAttempt(false);
                    return Unauthorized(new { error = "Invalid credentials" });
                }

                _loginTracker.RecordSuccess(request.Username);
                _logger.LogInformationOnly("Successful login for username: {Username}", request.Username);
                _metricsService.RecordAuthenticationAttempt(true);
                return Ok(new
                {
                    token.Value.token,
                    validFrom = token.Value.validFrom.ToShortTimeString(),
                    validTo = token.Value.validTo.ToShortTimeString(),
                    expiresIn = string.Concat(_jwtService.ExpirationMinutes, " minutes"),
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

        [HttpGet("events")]
        [Authorize(Roles = "Analyst")]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _repository.QueryFraudEventsAsync(query, false).ConfigureAwait(false);
            return Ok(records);
        }

        [HttpGet("events/{fraudEventId:long}/rules")]
        [Authorize(Roles = "Analyst")]
        public async Task<ActionResult<IEnumerable<FraudRuleSetRecord>>> GetRuleResults(long fraudEventId)
        {
            // Every stored fraud event has one result per rule, so an empty list means the event does not exist.
            var records = (await _repository.GetRuleResultsForEventAsync(fraudEventId).ConfigureAwait(false)).ToList();
            if (records.Count == 0)
                return NotFound(new { error = $"No fraud event found with id {fraudEventId}." });

            return Ok(records);
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
