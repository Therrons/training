using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
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
            // Create test web application factory
            _factory = new WebApplicationFactory<Program>();
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

            var content = await response.Content.ReadAsAsync<LoginResponse>();
            return content.Token;
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
            public string Token { get; set; }
            public int ExpiresIn { get; set; }
            public string TokenType { get; set; }
        }
    }

    /// <summary>
    /// xUnit collection definition for API tests
    /// Ensures tests share the same fixture instance
    /// </summary>
    [CollectionDefinition("API Integration Tests")]
    public class ApiTestCollection : ICollectionFixture<ApiTestFixture>
    {
    }
}
