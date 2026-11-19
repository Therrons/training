
using fraud_poc_project.Interfaces;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;

namespace fraud_poc_project.Services
{
    public class JwtService : IJwtService
    {
        private readonly IAuthenticationService _authService;
        private readonly ReferenceDataCache _referenceData;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes = 60;

        public int ExpirationMinutes => _expirationMinutes;

        public JwtService(
            IConfiguration configuration,
            IAuthenticationService authService,
            ReferenceDataCache referenceData,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _authService = authService;
            _referenceData = referenceData;
            _logger = logger;

            var jwtSettings = _configuration.GetSection("JwtSettings");
            _secret = jwtSettings["Secret"] ?? throw new InvalidOperationException("JWT Secret not configured");
            _issuer = jwtSettings["Issuer"] ?? "fraud-poc-api";
            _audience = jwtSettings["Audience"] ?? "fraud-poc-api-client";
            _expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

            _logger.LogInformationOnly("JwtService initialized with issuer: {Issuer}, audience: {Audience}", _issuer, _audience);
        }

        public (string token, DateTime validFrom, DateTime validTo)? GenerateToken(string username, string password)
        {
            if (!_authService.ValidateCredentials(username, password))
            {
                _logger.LogWarning("Token generation failed: invalid credentials for username {Username}", username);
                return null;
            }

            var token = GenerateToken(username, ResolveRoles(username));
            _logger.LogInformationOnly("JWT token generated successfully for username: {Username}", username);
            return token;
        }

        // Database logins get the role linked to them in the database.
        // Configured credentials (used when the database logins are unavailable) get the Analyst role.
        private IEnumerable<string> ResolveRoles(string username)
        {
            if (!_referenceData.LoginsLoadedFromDatabase)
            {
                return new[] { "Analyst" };
            }

            var roles = _referenceData.GetRoleNamesForUsername(username);
            if (roles.Count == 0)
            {
                _logger.LogWarning("No role found in the database for username {Username}", username);
            }
            return roles;
        }

        private (string token, DateTime validFrom, DateTime validTo) GenerateToken(string username, IEnumerable<string> roles)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username)
            };
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(_expirationMinutes),
                Issuer = _issuer,
                Audience = _audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var localNow = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.Local);
            return (tokenHandler.WriteToken(token), localNow, localNow.AddMinutes(_expirationMinutes));
        }
    }
}
