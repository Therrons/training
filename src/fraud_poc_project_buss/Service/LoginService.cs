using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using fraud_poc_project_buss.Interfaces;
using Microsoft.Extensions.Logging;

namespace fraud_poc_project_buss.Service
{
    public class LoginService : ILoginService
    {
        private const int MinimumRetryAfterSeconds = 60;

        private readonly IJwtService _jwtService;
        private readonly ILoginAttemptTracker _loginTracker;
        private readonly IAuthenticationMetrics _metrics;
        private readonly ILogger<LoginService> _logger;

        public LoginService(
            IJwtService jwtService,
            ILoginAttemptTracker loginTracker,
            IAuthenticationMetrics metrics,
            ILogger<LoginService> logger)
        {
            _jwtService = jwtService;
            _loginTracker = loginTracker;
            _metrics = metrics;
            _logger = logger;
        }

        public LoginResult Login(string? username, string? password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Login attempt with missing credentials");
                _metrics.RecordAuthenticationAttempt(false);
                return new LoginResult { Outcome = LoginOutcome.MissingCredentials };
            }

            if (_loginTracker.IsLockedOut(username, out var lockedUntil))
            {
                var secondsLeft = (int)Math.Ceiling((lockedUntil - DateTimeOffset.UtcNow).TotalSeconds);
                _logger.LogWarning("Login blocked, account temporarily locked for username: {Username}", username);
                _metrics.RecordAuthenticationAttempt(false);
                return new LoginResult
                {
                    Outcome = LoginOutcome.LockedOut,
                    RetryAfterSeconds = Math.Max(MinimumRetryAfterSeconds, secondsLeft)
                };
            }

            var token = _jwtService.GenerateToken(username, password);
            if (token == null)
            {
                _loginTracker.RecordFailure(username);
                _logger.LogWarning("Failed login attempt for username: {Username}", username);
                _metrics.RecordAuthenticationAttempt(false);
                return new LoginResult { Outcome = LoginOutcome.InvalidCredentials };
            }

            _loginTracker.RecordSuccess(username);
            _logger.LogInformationOnly("Successful login for username: {Username}", username);
            _metrics.RecordAuthenticationAttempt(true);

            return new LoginResult
            {
                Outcome = LoginOutcome.Succeeded,
                Token = token.Value.token,
                ValidFrom = token.Value.validFrom,
                ValidTo = token.Value.validTo,
                ExpirationMinutes = _jwtService.ExpirationMinutes
            };
        }
    }
}
