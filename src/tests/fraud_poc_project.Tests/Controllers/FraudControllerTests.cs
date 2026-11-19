using FluentAssertions;
using fraud_poc_project.Controllers;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Interfaces;
using fraud_poc_project_buss.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace fraud_poc_project.Tests.Controllers
{
    /// <summary>
    /// Tests for FraudController - API endpoint tests.
    /// The controller only translates service results into HTTP responses, so these tests mock the services.
    /// </summary>
    public class FraudControllerTests
    {
        private readonly Mock<IFraudQueryService> _mockQueryService;
        private readonly Mock<ILoginService> _mockLoginService;
        private readonly Mock<ILogger<FraudController>> _mockLogger;
        private readonly FraudController _controller;

        public FraudControllerTests()
        {
            _mockQueryService = new Mock<IFraudQueryService>();
            _mockLoginService = new Mock<ILoginService>();
            _mockLogger = new Mock<ILogger<FraudController>>();

            _controller = new FraudController(
                _mockLoginService.Object,
                _mockQueryService.Object,
                _mockLogger.Object)
            {
                // Gives the controller a Response object so headers can be set in tests
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
            };
        }

        [Fact]
        public async Task QueryEvents_WithValidQuery_ReturnsOkResultWithEvents()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                CustomerId = "CUST-001"
            };

            var expectedRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder().WithCustomerId("CUST-001").Build())
                    .WithIsFlagged(true)
                    .WithFraudScore(50m)
                    .Build(),
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder().WithCustomerId("CUST-001").Build())
                    .WithIsFlagged(false)
                    .WithFraudScore(20m)
                    .Build()
            };

            _mockQueryService
                .Setup(s => s.QueryEventsAsync(query))
                .ReturnsAsync(expectedRecords);

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().Be(expectedRecords);
            _mockQueryService.Verify(s => s.QueryEventsAsync(query), Times.Once);
        }

        [Fact]
        public async Task QueryEvents_WithEmptyResult_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                CustomerId = "NON-EXISTENT"
            };

            _mockQueryService
                .Setup(s => s.QueryEventsAsync(query))
                .ReturnsAsync(new List<FraudEventRecord>());

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedList = okResult?.Value as IEnumerable<FraudEventRecord>;
            returnedList.Should().BeEmpty();
        }

        [Fact]
        public async Task QueryEvents_CallsServiceWithProvidedQuery()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                IsFlaggedOnly = true
            };

            _mockQueryService
                .Setup(s => s.QueryEventsAsync(query))
                .ReturnsAsync(new List<FraudEventRecord>());

            // Act
            await _controller.QueryEvents(query);

            // Assert
            _mockQueryService.Verify(s => s.QueryEventsAsync(query), Times.Once);
        }

        [Fact]
        public async Task GetRuleResults_WithValidEventId_ReturnsOkResultWithRuleResults()
        {
            // Arrange
            const long fraudEventId = 123;

            var expectedRuleResults = new List<FraudRuleSetRecord>
            {
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "HIGH_AMOUNT",
                    RuleDescription = "High Amount Rule",
                    IsTriggered = true,
                    ScoreContribution = 40m
                },
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "FOREIGN_CNP",
                    RuleDescription = "Foreign CNP Rule",
                    IsTriggered = false,
                    ScoreContribution = 0m
                }
            };

            _mockQueryService
                .Setup(s => s.GetRuleResultsAsync(fraudEventId))
                .ReturnsAsync(expectedRuleResults);

            // Act
            var result = await _controller.GetRuleResults(fraudEventId);

            // Assert
            result.Result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().BeEquivalentTo(expectedRuleResults);
            _mockQueryService.Verify(s => s.GetRuleResultsAsync(fraudEventId), Times.Once);
        }

        [Fact]
        public async Task GetRuleResults_WhenServiceReturnsNull_ReturnsNotFound()
        {
            // Arrange: the service returns null when the fraud event does not exist
            const long fraudEventId = 999;

            _mockQueryService
                .Setup(s => s.GetRuleResultsAsync(fraudEventId))
                .ReturnsAsync((IReadOnlyList<FraudRuleSetRecord>?)null);

            // Act
            var result = await _controller.GetRuleResults(fraudEventId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task GetRuleResults_ReturnsBothTriggeredAndNonTriggeredRules()
        {
            // Arrange
            const long fraudEventId = 100;

            var ruleResults = new List<FraudRuleSetRecord>
            {
                new FraudRuleSetRecord { FraudEventId = fraudEventId, RuleCode = "HIGH_AMOUNT", IsTriggered = true, ScoreContribution = 40m },
                new FraudRuleSetRecord { FraudEventId = fraudEventId, RuleCode = "ATM_WITHDRAWAL_LIMIT", IsTriggered = false, ScoreContribution = 0m },
                new FraudRuleSetRecord { FraudEventId = fraudEventId, RuleCode = "FOREIGN_CNP", IsTriggered = true, ScoreContribution = 35m }
            };

            _mockQueryService
                .Setup(s => s.GetRuleResultsAsync(fraudEventId))
                .ReturnsAsync(ruleResults);

            // Act
            var result = await _controller.GetRuleResults(fraudEventId);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var returnedRules = okResult?.Value as IEnumerable<FraudRuleSetRecord>;
            returnedRules.Should().HaveCount(3);
            returnedRules?.Count(r => r.IsTriggered).Should().Be(2);
            returnedRules?.Count(r => !r.IsTriggered).Should().Be(1);
        }

        [Fact]
        public async Task QueryEvents_PreservesTransactionEventDetails()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59"
            };

            var customerId = "CUST-SPECIAL";
            var accountId = "ACC-SPECIAL";
            var amount = 12345.67m;

            var records = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder()
                        .WithCustomerId(customerId)
                        .WithAccountId(accountId)
                        .WithAmount(amount)
                        .Build())
                    .Build()
            };

            _mockQueryService
                .Setup(s => s.QueryEventsAsync(query))
                .ReturnsAsync(records);

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            var okResult = result.Result as OkObjectResult;
            var returnedRecords = okResult?.Value as IEnumerable<FraudEventRecord>;
            var firstRecord = returnedRecords?.First();
            firstRecord?.Event.CustomerId.Should().Be(customerId);
            firstRecord?.Event.AccountId.Should().Be(accountId);
            firstRecord?.Event.Amount.Should().Be(amount);
        }

        [Fact]
        public void Login_WhenServiceReportsMissingCredentials_ReturnsBadRequest()
        {
            _mockLoginService
                .Setup(s => s.Login(null, null))
                .Returns(new LoginResult { Outcome = LoginOutcome.MissingCredentials });

            var result = _controller.Login(new LoginRequest());

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public void Login_WhenInvalidCredentials_ReturnsUnauthorized()
        {
            _mockLoginService
                .Setup(s => s.Login("user", "wrong"))
                .Returns(new LoginResult { Outcome = LoginOutcome.InvalidCredentials });

            var result = _controller.Login(new LoginRequest { Username = "user", Password = "wrong" });

            result.Should().BeOfType<UnauthorizedObjectResult>();
        }

        [Fact]
        public void Login_WhenLockedOut_ReturnsTooManyRequestsWithRetryAfter()
        {
            _mockLoginService
                .Setup(s => s.Login("user", "pw"))
                .Returns(new LoginResult { Outcome = LoginOutcome.LockedOut, RetryAfterSeconds = 120 });

            var result = _controller.Login(new LoginRequest { Username = "user", Password = "pw" });

            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(429);
            _controller.Response.Headers["Retry-After"].ToString().Should().Be("120");
        }

        [Fact]
        public void Login_WhenSucceeded_ReturnsOkWithToken()
        {
            var validFrom = new DateTime(2026, 11, 18, 10, 0, 0, DateTimeKind.Local);
            _mockLoginService
                .Setup(s => s.Login("user", "pw"))
                .Returns(new LoginResult
                {
                    Outcome = LoginOutcome.Succeeded,
                    Token = "token-value",
                    ValidFrom = validFrom,
                    ValidTo = validFrom.AddMinutes(60),
                    ExpirationMinutes = 60
                });

            var result = _controller.Login(new LoginRequest { Username = "user", Password = "pw" });

            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
            okResult.Value!.ToString().Should().Contain("token-value");
        }
    }
}
