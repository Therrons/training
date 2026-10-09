using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace fraud_poc_project.Tests.Integration.Fixtures
{
    /// <summary>
    /// Fixture for API integration tests using WebApplicationFactory
    /// Provides a test HTTP client and JWT token management
    /// </summary>
    public class ApiTestFixture : IAsyncLifetime
    {
        private WebApplicationFactory<Program> _factory;
        public HttpClient Client { get; private set; }
        public string JwtToken { get; private set; }

        public async Task InitializeAsync()
        {
            // Set environment variables FIRST - these are checked by configuration system with HIGHEST priority
            // Environment_Variables.Get_Environment_Values() and other services read these first
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "LOC");
            Environment.SetEnvironmentVariable("DB_HOST", "localhost");  // Override "postgres" from appsettings.LOC.json
            Environment.SetEnvironmentVariable("DB_PORT", "5432");
            Environment.SetEnvironmentVariable("DB_NAME", "fraud_db");   // Use the correct database name
            Environment.SetEnvironmentVariable("DB_USERNAME", "postgres");
            Environment.SetEnvironmentVariable("DB_PASSWORD", "postgres");
            Environment.SetEnvironmentVariable("API_USERNAME", "fraud-analyst");  // API authentication credential
            Environment.SetEnvironmentVariable("API_PASSWORD", "SecurePass123!");  // API authentication credential

            // Create test web application factory with test configuration
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Set environment name within the builder context
                    builder.UseEnvironment("LOC");

                    // Configure logging to suppress expected warnings from authentication tests
                    builder.ConfigureLogging((context, logging) =>
                    {
                        logging.AddFilter("fraud_poc_project.Services.JwtService", LogLevel.Error);
                        logging.AddFilter("fraud_poc_project.Services.AuthenticationService", LogLevel.Error);
                        logging.AddFilter("fraud_poc_project.Controllers.FraudController", LogLevel.Error);
                    });

                    // Add supplemental configuration via in-memory collection for other required settings
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            // JWT configuration for test - MUST match appsettings.LOC.json values
                            ["JwtSettings:Secret"] = "your-super-duper-secret-key-of-min-32-characters-long",
                            ["JwtSettings:Issuer"] = "fraud-poc-api",
                            ["JwtSettings:Audience"] = "fraud-poc-api-client",
                            ["JwtSettings:ExpirationMinutes"] = "60",
                            // API authentication credentials for test
                            ["API_USERNAME"] = "fraud-analyst",
                            ["API_PASSWORD"] = "SecurePass123!",
                            // Kafka configuration for test
                            ["KafkaSettings:BrokerSettings:BootstrapServers"] = "localhost:9094",
                            ["KafkaSettings:BrokerSettings:SaslMechanism"] = "Plain",
                            ["KafkaSettings:BrokerSettings:SaslPassword"] = "test_password",
                            ["KafkaSettings:BrokerSettings:SaslUserName"] = "test_user",
                            ["KafkaSettings:BrokerSettings:SecurityProtocol"] = "SaslPlaintext",
                            ["KafkaSettings:BrokerSettings:AllowAutoCreateTopics"] = "true"
                        });
                    });
                });

            Client = _factory.CreateClient();

            // Authenticate and get JWT token
            JwtToken = await AuthenticateAsync();
        }

        public async Task DisposeAsync()
        {
            Client?.Dispose();
            _factory?.Dispose();
            await Task.CompletedTask;
        }

        /// <summary>
        /// Authenticates with the API and retrieves a JWT token
        /// </summary>
        private async Task<string> AuthenticateAsync()
        {
            var loginRequest = new
            {
                username = "fraud-analyst",
                password = "SecurePass123!"
            };

            var response = await Client.PostAsJsonAsync("/api/fraud/login", loginRequest);
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK,
                "Login should succeed with default credentials");

            var content = await response.Content.ReadFromJsonAsync<LoginResponse>();
            content.Should().NotBeNull();
            return content!.Token;
        }

        /// <summary>
        /// Sets the Authorization header for authenticated requests
        /// </summary>
        public void AuthorizeClient()
        {
            Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", JwtToken);
        }

        /// <summary>
        /// Clears the Authorization header
        /// </summary>
        public void DeauthorizeClient()
        {
            Client.DefaultRequestHeaders.Authorization = null;
        }

        private class LoginResponse
        {
            [JsonPropertyName("token")]
            public string Token { get; set; }

            [JsonPropertyName("expiresIn")]
            public string ExpiresIn { get; set; }  // Accept as string since API returns it as a string

            [JsonPropertyName("tokenType")]
            public string TokenType { get; set; }
        }
    }

    /// <summary>
    /// xUnit collection definition for API tests
    /// Ensures tests share the same fixture instance
    /// DisableParallelization is required because tests modify shared HttpClient headers
    /// </summary>
    [CollectionDefinition("API Integration Tests", DisableParallelization = true)]
    public class ApiTestCollection : ICollectionFixture<ApiTestFixture>
    {
    }
}
