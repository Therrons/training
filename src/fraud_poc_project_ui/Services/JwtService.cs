
using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace fraud_poc_project.Services
{
    public interface IJwtService
    {
        (string token, DateTime validFrom, DateTime validTo)? GenerateToken(string username, string password);
        ClaimsPrincipal? ValidateToken(string token);
    }

    public class JwtService : IJwtService
    {
        private readonly IAuthenticationService _authService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<JwtService> _logger;
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _expirationMinutes;

        public JwtService(
            IConfiguration configuration,
            IAuthenticationService authService,
            ILogger<JwtService> logger)
        {
            _configuration = configuration;
            _authService = authService;
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

            var token = GenerateToken(username);
            _logger.LogInformationOnly("JWT token generated successfully for username: {Username}", username);
            return token;
        }

        private (string token, DateTime validFrom, DateTime validTo) GenerateToken(string username)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_secret);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, username),
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, "Analyst"),
                new Claim(ClaimTypes.Role, "Admin")
            };

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

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(_secret);

                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _issuer,
                    ValidateAudience = true,
                    ValidAudience = _audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                _logger.LogDebug("JWT token validated successfully");
                return principal;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("JWT token validation failed: {Exception}", ex.Message);
                return null;
            }
        }
    }
}
