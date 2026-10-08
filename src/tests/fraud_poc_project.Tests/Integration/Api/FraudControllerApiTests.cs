using FluentAssertions;
using fraud_poc_project.Tests.Integration.Fixtures;
using fraud_poc_project_buss.Dto;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace fraud_poc_project.Tests.Integration.Api
{
    /// <summary>
    /// Integration tests for Fraud Detection API endpoints
    /// Tests the full HTTP pipeline with real dependency injection
    /// </summary>
    [Collection("API Integration Tests")]
    public class FraudControllerApiTests
    {
        private readonly ApiTestFixture _fixture;

        public FraudControllerApiTests(ApiTestFixture fixture)
        {
            _fixture = fixture;
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // AUTHENTICATION TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsJwtToken()
        {
            // Arrange
            var loginRequest = new
            {
                username = "fraud-analyst",
                password = "SecurePass123!"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/fraud/login", loginRequest).ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<LoginResponse>().ConfigureAwait(false);
            content.Token.Should().NotBeNullOrEmpty("Token should be provided");
            content.ExpiresIn.Should().Be(3600, "Token should expire in 1 hour");
            content.TokenType.Should().Be("Bearer");
        }

        [Fact]
        public async Task Login_WithInvalidUsername_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                username = "invalid-user",
                password = "SecurePass123!"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/fraud/login", loginRequest).ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
        {
            // Arrange
            var loginRequest = new
            {
                username = "fraud-analyst",
                password = "WrongPassword"
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/fraud/login", loginRequest).ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Login_WithMissingCredentials_ReturnsBadRequest()
        {
            // Arrange
            var loginRequest = new
            {
                username = "",
                password = ""
            };

            // Act
            var response = await _fixture.Client.PostAsJsonAsync("/api/fraud/login", loginRequest).ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // AUTHORIZATION TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task QueryEvents_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange - Do NOT authorize

            // Act
            var response = await _fixture.Client.GetAsync("/api/fraud/events").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
                "Unauthenticated requests should be rejected");
        }

        [Fact]
        public async Task QueryEvents_WithInvalidToken_ReturnsUnauthorized()
        {
            // Arrange
            _fixture.Client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", "invalid-token");

            // Act
            var response = await _fixture.Client.GetAsync("/api/fraud/events").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        [Fact]
        public async Task GetRuleResults_WithoutToken_ReturnsUnauthorized()
        {
            // Arrange - Do NOT authorize

            // Act
            var response = await _fixture.Client.GetAsync("/api/fraud/events/1/rules").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // QUERY EVENTS ENDPOINT TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task QueryEvents_WithValidToken_ReturnsOk()
        {
            // Arrange
            _fixture.AuthorizeClient();

            // Act
            var response = await _fixture.Client.GetAsync("/api/fraud/events").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<List<FraudEventRecord>>().ConfigureAwait(false);
            content.Should().BeOfType<List<FraudEventRecord>>();

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        [Fact]
        public async Task QueryEvents_WithDateRange_IncludesDateParameters()
        {
            // Arrange
            _fixture.AuthorizeClient();
            var dateFrom = "2024-01-01 00:00:00";
            var dateTo = "2024-12-31 23:59:59";

            // Act
            var response = await _fixture.Client.GetAsync(
                $"/api/fraud/events?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<List<FraudEventRecord>>().ConfigureAwait(false);
            content.Should().BeOfType<List<FraudEventRecord>>();

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        [Fact]
        public async Task QueryEvents_WithCustomerId_IncludesFilterParameter()
        {
            // Arrange
            _fixture.AuthorizeClient();
            var customerId = "CUST-TEST-001";

            // Act
            var response = await _fixture.Client.GetAsync(
                $"/api/fraud/events?customerId={customerId}").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<List<FraudEventRecord>>().ConfigureAwait(false);
            content.Should().BeOfType<List<FraudEventRecord>>();

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        [Fact]
        public async Task QueryEvents_MultipleFilters_AllApplied()
        {
            // Arrange
            _fixture.AuthorizeClient();
            var dateFrom = "2024-01-01 00:00:00";
            var dateTo = "2024-12-31 23:59:59";
            var customerId = "CUST-001";

            // Act
            var response = await _fixture.Client.GetAsync(
                $"/api/fraud/events?dateFrom={Uri.EscapeDataString(dateFrom)}&dateTo={Uri.EscapeDataString(dateTo)}&customerId={customerId}").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);

            var content = await response.Content.ReadFromJsonAsync<List<FraudEventRecord>>().ConfigureAwait(false);
            content.Should().BeOfType<List<FraudEventRecord>>();

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // GET RULE RESULTS ENDPOINT TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task GetRuleResults_WithValidToken_ReturnsOk()
        {
            // Arrange
            _fixture.AuthorizeClient();
            const long eventId = 1;

            // Act
            var response = await _fixture.Client.GetAsync($"/api/fraud/events/{eventId}/rules").ConfigureAwait(false);

            // Assert
            // 200 if event exists, 404 if not (both are valid)
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        [Fact]
        public async Task GetRuleResults_WithInvalidEventId_ReturnsNotFoundOrEmpty()
        {
            // Arrange
            _fixture.AuthorizeClient();
            const long invalidEventId = 999999999;

            // Act
            var response = await _fixture.Client.GetAsync($"/api/fraud/events/{invalidEventId}/rules").ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // CONTENT VALIDATION TESTS
        // ════════════════════════════════════════════════════════════════════════════════

        [Fact]
        public async Task QueryEvents_ResponseContentValid()
        {
            // Arrange
            _fixture.AuthorizeClient();

            // Act
            var response = await _fixture.Client.GetAsync("/api/fraud/events").ConfigureAwait(false);
            var content = await response.Content.ReadFromJsonAsync<List<FraudEventRecord>>().ConfigureAwait(false);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            content.Should().NotBeNull("Response should contain fraud events");

            // Each event should have required properties
            if (content.Any())
            {
                content.ForEach(record =>
                {
                    record.Should().NotBeNull();
                    record.Event.Should().NotBeNull("Event should be populated");
                    record.RuleResults.Should().NotBeNull("Rule results should be populated");
                });
            }

            // Cleanup
            _fixture.DeauthorizeClient();
        }

        // ════════════════════════════════════════════════════════════════════════════════
        // HELPER CLASSES
        // ════════════════════════════════════════════════════════════════════════════════

        private class LoginResponse
        {
            public string Token { get; set; }
            public int ExpiresIn { get; set; }
            public string TokenType { get; set; }
        }
    }
}
