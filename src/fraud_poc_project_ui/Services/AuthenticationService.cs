using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;

namespace fraud_poc_project.Services
{
    /// <summary>
    /// Service for authenticating API users against configured credentials from AWS Secrets Manager
    /// </summary>
    public interface IAuthenticationService
    {
        bool ValidateCredentials(string username, string password);
    }

    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _validUsername;
        private readonly string _validPassword;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IConfiguration configuration, ILogger<AuthenticationService> logger)
        {
            _logger = logger;
            _validUsername = configuration["API_USERNAME"]
                ?? throw new InvalidOperationException("API_USERNAME not configured");
            _validPassword = configuration["API_PASSWORD"]
                ?? throw new InvalidOperationException("API_PASSWORD not configured");

            _logger.LogInformationOnly("AuthenticationService initialized");
        }

        /// <summary>
        /// Validates provided credentials against configured credentials using constant-time comparison
        /// </summary>
        public bool ValidateCredentials(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                _logger.LogWarning("Credential validation failed: empty username or password");
                return false;
            }

            // Use constant-time comparison to prevent timing attacks
            bool usernameMatch = ConstantTimeCompare(username, _validUsername);
            bool passwordMatch = ConstantTimeCompare(password, _validPassword);

            bool isValid = usernameMatch && passwordMatch;

            if (!isValid)
            {
                _logger.LogWarning($"Failed authentication attempt for username: {username}");
            }

            return isValid;
        }

        /// <summary>
        /// Constant-time string comparison to prevent timing attacks
        /// </summary>
        private static bool ConstantTimeCompare(string a, string b)
        {
            if (a == null || b == null)
                return a == b;

            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }
    }
}
