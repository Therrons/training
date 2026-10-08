using fraud_poc_project_buss.Helper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace fraud_poc_project.Services
{
    public interface IAuthenticationService
    {
        bool ValidateCredentials(string username, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _validUsername;
        private readonly string _validPassword;
        private readonly ILogger<AuthenticationService> _logger;

        private const string DummyHash = "$2a$11$1234567890123456789012uXyZ...fakehash...";

        public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _logger = logger;
            _validUsername = configuration["API_USERNAME"]
                ?? throw new InvalidOperationException("API_USERNAME not configured");
            _validPassword = configuration["API_PASSWORD"]
                ?? throw new InvalidOperationException("API_PASSWORD not configured");

            _logger.LogInformationOnly("AuthenticationService initialized");
        }

        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Credential validation failed: empty username or password");
                return false;
            }

            // Use constant-time comparison to prevent timing attacks
            bool usernameMatch = username.SensitiveDataCompare(_validUsername);
            bool passwordMatch = password.SensitiveDataCompare(_validPassword);

            bool isValid = usernameMatch && passwordMatch;

            if (!isValid)
            {
                _logger.LogWarning($"Failed authentication attempt for username: {username}");
            }

            return isValid;
        }
    }
}
