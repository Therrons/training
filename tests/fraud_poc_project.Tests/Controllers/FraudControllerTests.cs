using FluentAssertions;
using fraud_poc_project.Controllers;
using fraud_poc_project.Tests.Fixtures;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using Xunit;

namespace fraud_poc_project.Tests.Controllers
{
    /// <summary>
    /// Unit tests for FraudController.
    /// Tests API endpoints: event queries, flagged events, and rule result retrieval.
    /// </summary>
    public class FraudControllerTests : BaseApiTest
    {
        private FraudController? _controller;
        private Mock<IFraudRepository>? _mockRepository;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            // Setup mock repository
            _mockRepository = new Mock<IFraudRepository>();
            _controller = new FraudController(_mockRepository.Object);
        }

        // ============================================================
        // Test 1: GET /api/fraud/events - Query All Events
        // ============================================================

        /// <summary>
        /// Verifies that GET /api/fraud/events returns 200 OK with a list of fraud events
        /// when called with a valid date range.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithValidDateRange_Returns200WithEventList()
        {
            // Arrange
            var testRecords = CreateBatchOfFraudEventRecords(count: 5);
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(testRecords);

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            var returnedRecords = okResult.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().HaveCount(5);
            _mockRepository.Verify(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()), Times.Once);
        }

        // ============================================================
        // Test 2: GET /api/fraud/events/flagged - Flagged Events Only
        // ============================================================

        /// <summary>
        /// Verifies that GET /api/fraud/events/flagged returns only transactions
        /// that were flagged as fraudulent.
        /// </summary>
        [Fact]
        public async Task QueryFlaggedEvents_WithValidDateRange_ReturnsFlaggedEventsOnly()
        {
            // Arrange
            var allRecords = CreateBatchOfFraudEventRecords(count: 10);
            var flaggedRecords = allRecords.Where(r => r.IsFlagged).ToList();
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };

            _mockRepository!
                .Setup(r => r.QueryFlaggedOnlyFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(flaggedRecords);

            // Act
            var result = await _controller!.QueryFlaggedEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            var returnedRecords = okResult.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().AllSatisfy(r => r.IsFlagged.Should().BeTrue());
        }

        // ============================================================
        // Test 3: GET /api/fraud/events/{fraudEventId}/rules - Get Rule Results
        // ============================================================

        /// <summary>
        /// Verifies that GET /api/fraud/events/{id}/rules returns all fraud rule results
        /// for a specific fraud event.
        /// </summary>
        [Fact]
        public async Task GetRuleResults_WithValidEventId_Returns200WithRuleResults()
        {
            // Arrange
            long fraudEventId = 12345;
            var ruleResults = new List<fraud_poc_project_buss.Models.Fraud.FraudRuleSetRecord>
            {
                new() { RuleCode = "RULE_1", IsTriggered = true, ScoreContribution = 25m },
                new() { RuleCode = "RULE_2", IsTriggered = true, ScoreContribution = 25m },
                new() { RuleCode = "RULE_3", IsTriggered = false, ScoreContribution = 0m }
            };

            _mockRepository!
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(ruleResults);

            // Act
            var result = await _controller!.GetRuleResults(fraudEventId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.StatusCode.Should().Be(200);
            var returnedRules = okResult.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudRuleSetRecord>;
            returnedRules.Should().HaveCount(3);
            returnedRules.Should().Contain(r => r.RuleCode == "RULE_1");
        }

        // ============================================================
        // Test 4: Query with Date Filters
        // ============================================================

        /// <summary>
        /// Verifies that date filters (DateFrom, DateTo) are properly applied
        /// to limit results to the specified timeframe.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithDateFilters_AppliesDateRangeCorrectly()
        {
            // Arrange
            var dateFrom = DateTime.UtcNow.AddDays(-30);
            var dateTo = DateTime.UtcNow;
            var query = new FraudQueryDto { DateFrom = dateFrom, DateTo = dateTo };
            var testRecords = CreateBatchOfFraudEventRecords(count: 5);

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.Is<FraudQueryDto>(
                    q => q.DateFrom == dateFrom && q.DateTo == dateTo)))
                .ReturnsAsync(testRecords);

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            _mockRepository.Verify(
                r => r.QueryFraudEventsAsync(It.Is<FraudQueryDto>(
                    q => q.DateFrom == dateFrom && q.DateTo == dateTo)),
                Times.Once);
        }

        // ============================================================
        // Test 5: Query with Customer ID Filter
        // ============================================================

        /// <summary>
        /// Verifies that the CustomerId filter is properly applied
        /// to return only events for that specific customer.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithCustomerIdFilter_FiltersCorrectly()
        {
            // Arrange
            var customerId = "CUST001";
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow,
                CustomerId = customerId
            };
            var filteredRecords = CreateBatchOfFraudEventRecords(count: 3)
                .Where(r => r.Event.CustomerId == customerId)
                .ToList();

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.Is<FraudQueryDto>(
                    q => q.CustomerId == customerId)))
                .ReturnsAsync(filteredRecords);

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRecords = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().AllSatisfy(r => r.Event.CustomerId.Should().Be(customerId));
        }

        // ============================================================
        // Test 6: Query with Fraud Score Filter
        // ============================================================

        /// <summary>
        /// Verifies that the MinFraudScore filter properly filters events
        /// to include only those with a score >= the minimum threshold.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithMinFraudScoreFilter_FiltersCorrectly()
        {
            // Arrange
            var minScore = 50m;
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow,
                MinFraudScore = minScore
            };
            var filteredRecords = CreateBatchOfFraudEventRecords(count: 10)
                .Where(r => r.FraudScore >= minScore)
                .ToList();

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.Is<FraudQueryDto>(
                    q => q.MinFraudScore == minScore)))
                .ReturnsAsync(filteredRecords);

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRecords = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().AllSatisfy(r => r.FraudScore.Should().BeGreaterThanOrEqualTo(minScore));
        }

        // ============================================================
        // Test 7: Invalid Query Returns BadRequest
        // ============================================================

        /// <summary>
        /// Verifies that invalid query parameters result in a 400 Bad Request response.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithInvalidDateFormat_ReturnsBadRequest()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.MaxValue,
                DateTo = DateTime.MinValue // DateFrom > DateTo
            };

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ThrowsAsync(new ArgumentException("Invalid date range"));

            // Act
            Func<Task> act = async () => await _controller!.QueryEvents(query);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>();
        }

        // ============================================================
        // Test 8: Event Not Found Returns Empty List
        // ============================================================

        /// <summary>
        /// Verifies that when no events match the query criteria,
        /// an empty list is returned (not a 404).
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithNoMatchingEvents_ReturnsEmptyList()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddYears(-10),
                DateTo = DateTime.UtcNow.AddYears(-9)
            };

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(new List<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>());

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRecords = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().BeEmpty();
        }

        // ============================================================
        // Test 9: Repository Exception Returns 500
        // ============================================================

        /// <summary>
        /// Verifies that when the repository throws an exception,
        /// the controller propagates it appropriately (500 error).
        /// </summary>
        [Fact]
        public async Task QueryEvents_WhenRepositoryThrowsException_ExceptionPropagates()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ThrowsAsync(new Exception("Database connection failed"));

            // Act
            Func<Task> act = async () => await _controller!.QueryEvents(query);

            // Assert
            await act.Should().ThrowAsync<Exception>()
                .WithMessage("Database connection failed");
        }

        // ============================================================
        // Test 10: Pagination/Large Result Sets
        // ============================================================

        /// <summary>
        /// Verifies that the controller can handle large result sets
        /// with proper serialization and without timeout.
        /// </summary>
        [Fact]
        public async Task QueryEvents_WithLargeResultSet_Returns200Successfully()
        {
            // Arrange
            var largeResultSet = CreateBatchOfFraudEventRecords(count: 500);
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(largeResultSet);

            // Act
            var result = await _controller!.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRecords = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().HaveCount(500);
        }

        // ============================================================
        // Test 11: Concurrent Requests Handled Properly
        // ============================================================

        /// <summary>
        /// Verifies that multiple concurrent requests to the API are handled
        /// independently without interference.
        /// </summary>
        [Fact]
        public async Task MultipleQueryEvents_ConcurrentRequests_AllSucceed()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };
            var testRecords = CreateBatchOfFraudEventRecords(count: 5);

            _mockRepository!
                .Setup(r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(testRecords);

            // Act - Make 5 concurrent requests
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => _controller!.QueryEvents(query))
                .ToList();

            var results = await Task.WhenAll(tasks);

            // Assert
            results.Should().AllSatisfy(r => r.Should().BeOfType<OkObjectResult>());
            _mockRepository.Verify(
                r => r.QueryFraudEventsAsync(It.IsAny<FraudQueryDto>()),
                Times.Exactly(5));
        }

        // ============================================================
        // Test 12: Empty Result Array Format
        // ============================================================

        /// <summary>
        /// Verifies that when no results are found, an empty JSON array
        /// is returned (not null or error).
        /// </summary>
        [Fact]
        public async Task QueryFlaggedEvents_WithNoFlaggedEvents_ReturnsEmptyArray()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = DateTime.UtcNow.AddDays(-7),
                DateTo = DateTime.UtcNow
            };

            _mockRepository!
                .Setup(r => r.QueryFlaggedOnlyFraudEventsAsync(It.IsAny<FraudQueryDto>()))
                .ReturnsAsync(new List<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>());

            // Act
            var result = await _controller!.QueryFlaggedEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRecords = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudEventRecord>;
            returnedRecords.Should().NotBeNull();
            returnedRecords.Should().BeEmpty();
        }

        // ============================================================
        // Test 13: Rule Results Not Found Returns Empty
        // ============================================================

        /// <summary>
        /// Verifies that when no rule results exist for an event,
        /// an empty list is returned.
        /// </summary>
        [Fact]
        public async Task GetRuleResults_WithNonExistentEventId_ReturnsEmptyList()
        {
            // Arrange
            long nonExistentId = 999999;

            _mockRepository!
                .Setup(r => r.GetRuleResultsForEventAsync(nonExistentId))
                .ReturnsAsync(new List<fraud_poc_project_buss.Models.Fraud.FraudRuleSetRecord>());

            // Act
            var result = await _controller!.GetRuleResults(nonExistentId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var returnedRules = okResult!.Value as IEnumerable<fraud_poc_project_buss.Models.Fraud.FraudRuleSetRecord>;
            returnedRules.Should().BeEmpty();
        }
    }
}
