using fraud_poc_project.CustomAttributes;
using fraud_poc_project.Exceptions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Models;
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
        private readonly ILoginService _loginService;
        private readonly IFraudQueryService _queryService;
        private readonly ILogger<FraudController> _logger;

        public FraudController(
            ILoginService loginService,
            IFraudQueryService queryService,
            ILogger<FraudController> logger)
        {
            _loginService = loginService;
            _queryService = queryService;
            _logger = logger;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                var result = _loginService.Login(request?.Username, request?.Password);

                switch (result.Outcome)
                {
                    case LoginOutcome.MissingCredentials:
                        return BadRequest(new { error = "Username and password required" });

                    case LoginOutcome.LockedOut:
                        Response.Headers["Retry-After"] = result.RetryAfterSeconds.ToString();
                        return StatusCode(429, new { error = $"Account temporarily locked due to repeated failed logins. Try again after {result.RetryAfterSeconds / 60} minute/s." });

                    case LoginOutcome.InvalidCredentials:
                        return Unauthorized(new { error = "Invalid credentials" });

                    default:
                        return Ok(new
                        {
                            token = result.Token,
                            validFrom = result.ValidFrom.ToUniversalTime(),
                            validTo = result.ValidTo.ToUniversalTime(),
                            expiresIn = string.Concat(result.ExpirationMinutes, " minutes"),
                            tokenType = "Bearer"
                        });
                }
            }
            catch (AuthenticationException ex)
            {
                _logger.LogError(ex, "Authentication error for username: {Username}", request?.Username);
                return Unauthorized(new { error = "Authentication failed", detail = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login");
                return StatusCode(500, new { error = "An unexpected error occurred during authentication" });
            }
        }

        [HttpGet("events")]
        [Authorize(Roles = $"{nameof(AuthenticationRoles.Analyst)},{nameof(AuthenticationRoles.Admin)}")]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _queryService.QueryEventsAsync(query).ConfigureAwait(false);
            return Ok(records);
        }

        [HttpGet("events/{fraudEventId:long}/rules")]
        [Authorize(Roles = $"{nameof(AuthenticationRoles.Analyst)},{nameof(AuthenticationRoles.Admin)}")]
        public async Task<ActionResult<IEnumerable<FraudRuleSetRecord>>> GetRuleResults(long fraudEventId)
        {
            var records = await _queryService.GetRuleResultsAsync(fraudEventId).ConfigureAwait(false);
            if (records == null)
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
