using fraud_poc_project.Interfaces;
using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;

namespace fraud_poc_project.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly string _validUsername;
        private readonly string _validPassword;
        private readonly ReferenceDataCache _referenceData;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(IConfiguration configuration, ReferenceDataCache referenceData, ILogger<AuthenticationService> logger)
        {
            _logger = logger;
            _referenceData = referenceData;

            // Configured values are only used when the logins could not be loaded from the database
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

            bool isValid = _referenceData.LoginsLoadedFromDatabase
                ? ValidateAgainstDatabase(username, password)
                : ValidateAgainstConfiguration(username, password);

            if (!isValid)
            {
                _logger.LogWarning($"Failed authentication attempt for username: {username}");
            }

            return isValid;
        }

        // The database is the source of truth once its logins have been loaded
        private bool ValidateAgainstDatabase(string username, string password)
        {
            var login = _referenceData.Logins
                .FirstOrDefault(l => string.Equals(l.UserserName, username, StringComparison.Ordinal));

            if (login == null)
            {
                return false;
            }

            // Stored passwords are PBKDF2 hashes; the comparison is constant-time
            return PasswordHasher.Verify(password, login.PassWord);
        }

        private bool ValidateAgainstConfiguration(string username, string password)
        {
            // Use constant-time comparison to prevent timing attacks
            bool usernameMatch = username.SensitiveDataCompare(_validUsername);
            bool passwordMatch = password.SensitiveDataCompare(_validPassword);

            return usernameMatch && passwordMatch;
        }
    }
}
