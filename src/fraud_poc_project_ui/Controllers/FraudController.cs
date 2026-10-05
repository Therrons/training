using fraud_poc_project.CustomAttributes;
using fraud_poc_project.Services;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger<FraudController> _logger;

        public FraudController(
            IFraudRepository repository,
            IJwtService jwtService,
            ILogger<FraudController> logger)
        {
            _repository = repository;
            _jwtService = jwtService;
            _logger = logger;
        }

        /// <summary>
        /// Authenticate with username and password to obtain a JWT token.
        /// </summary>
        /// <remarks>
        /// Use the returned token in the Authorization header as: Bearer {token}
        /// </remarks>
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request?.Username) || string.IsNullOrWhiteSpace(request?.Password))
            {
                _logger.LogWarning("Login attempt with missing credentials");
                return BadRequest(new { error = "Username and password required" });
            }

            var token = _jwtService.GenerateToken(request.Username, request.Password);

            if (token == null)
            {
                _logger.LogWarning("Failed login attempt for username: {Username}", request.Username);
                return Unauthorized(new { error = "Invalid credentials" });
            }

            _logger.LogInformation("Successful login for username: {Username}", request.Username);
            return Ok(new
            {
                token,
                expiresIn = 3600,
                tokenType = "Bearer"
            });
        }

        /// <summary>
        /// Query fraud-evaluated transaction events by date range and optional filters.
        /// </summary>
        /// <remarks>
        /// The DateFrom and DateTo formats expects the following format: 2024-01-01 09:00:00
        /// Requires authentication: Include JWT token in Authorization header as "Bearer {token}"
        /// </remarks>

        [HttpGet("events")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FraudEventRecord>>> QueryEvents([FromQuery] FraudQueryDto query)
        {
            var records = await _repository.QueryFraudEventsAsync(query);
            return Ok(records);
        }

        /// <summary>
        /// Retrieve all fraud rule results for a specific fraud event.
        /// </summary>
        /// <remarks>
        /// Requires authentication: Include JWT token in Authorization header as "Bearer {token}"
        /// </remarks>
        [HttpGet("events/{fraudEventId:long}/rules")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<FraudRuleSetRecord>>> GetRuleResults(long fraudEventId)
        {
            var records = await _repository.GetRuleResultsForEventAsync(fraudEventId);
            return Ok(records);
        }
    }

    /// <summary>
    /// Request model for user login
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// The username to authenticate
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The password to authenticate
        /// </summary>
        public string Password { get; set; }
    }
}
