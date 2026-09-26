using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project.Controllers;
using fraud_poc_project_repo.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace fraud_poc_project.Tests.Controllers
{
    /// <summary>
    /// Tests for FraudController - API endpoint tests.
    /// Verifies controller logic, response handling, and integration with repository.
    /// </summary>
    public class FraudControllerTests
    {
        private readonly Mock<IFraudRepository> _mockRepository;
        private readonly FraudController _controller;

        public FraudControllerTests()
        {
            _mockRepository = new Mock<IFraudRepository>();
            _controller = new FraudController(_mockRepository.Object);
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

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(expectedRecords);

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().Be(expectedRecords);
            _mockRepository.Verify(r => r.QueryFraudEventsAsync(query), Times.Once);
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

            var emptyRecords = new List<FraudEventRecord>();

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(emptyRecords);

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedList = okResult?.Value as IEnumerable<FraudEventRecord>;
            returnedList.Should().BeEmpty();
        }

        [Fact]
        public async Task QueryEvents_CallsRepositoryWithProvidedQuery()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                IsFlaggedOnly = true
            };

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(new List<FraudEventRecord>());

            // Act
            await _controller.QueryEvents(query);

            // Assert
            _mockRepository.Verify(r => r.QueryFraudEventsAsync(query), Times.Once);
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

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(expectedRuleResults);

            // Act
            var result = await _controller.GetRuleResults(fraudEventId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            okResult?.Value.Should().Be(expectedRuleResults);
            _mockRepository.Verify(r => r.GetRuleResultsForEventAsync(fraudEventId), Times.Once);
        }

        [Fact]
        public async Task GetRuleResults_WithNoRuleResults_ReturnsOkResultWithEmptyList()
        {
            // Arrange
            const long fraudEventId = 999;
            var emptyResults = new List<FraudRuleSetRecord>();

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(emptyResults);

            // Act
            var result = await _controller.GetRuleResults(fraudEventId);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedList = okResult?.Value as IEnumerable<FraudRuleSetRecord>;
            returnedList.Should().BeEmpty();
        }

        [Fact]
        public async Task GetRuleResults_CallsRepositoryWithProvidedEventId()
        {
            // Arrange
            const long fraudEventId = 456;

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
                .ReturnsAsync(new List<FraudRuleSetRecord>());

            // Act
            await _controller.GetRuleResults(fraudEventId);

            // Assert
            _mockRepository.Verify(r => r.GetRuleResultsForEventAsync(fraudEventId), Times.Once);
        }

        [Fact]
        public async Task QueryEvents_WithMultipleFilters_ReturnsFilteredResults()
        {
            // Arrange
            var query = new FraudQueryDto
            {
                DateFrom = "2024-01-01 00:00:00",
                DateTo = "2024-01-31 23:59:59",
                CustomerId = "CUST-001",
                TransactionType = "CNP",
                MinFraudScore = 40m,
                IsFlaggedOnly = true
            };

            var filteredRecords = new List<FraudEventRecord>
            {
                new FraudEventRecordBuilder()
                    .WithEvent(new TransactionEventBuilder()
                        .WithCustomerId("CUST-001")
                        .WithTransactionType("CNP")
                        .Build())
                    .WithIsFlagged(true)
                    .WithFraudScore(50m)
                    .Build()
            };

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
                .ReturnsAsync(filteredRecords);

            // Act
            var result = await _controller.QueryEvents(query);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result.Result as OkObjectResult;
            var returnedList = okResult?.Value as IEnumerable<FraudEventRecord>;
            returnedList.Should().HaveCount(1);
            returnedList?.First().IsFlagged.Should().BeTrue();
        }

        [Fact]
        public async Task GetRuleResults_ReturnsBothTriggeredAndNonTriggeredRules()
        {
            // Arrange
            const long fraudEventId = 100;

            var ruleResults = new List<FraudRuleSetRecord>
            {
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "HIGH_AMOUNT",
                    IsTriggered = true,
                    ScoreContribution = 40m
                },
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "ATM_WITHDRAWAL_LIMIT",
                    IsTriggered = false,
                    ScoreContribution = 0m
                },
                new FraudRuleSetRecord
                {
                    FraudEventId = fraudEventId,
                    RuleCode = "FOREIGN_CNP",
                    IsTriggered = true,
                    ScoreContribution = 35m
                }
            };

            _mockRepository
                .Setup(r => r.GetRuleResultsForEventAsync(fraudEventId))
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

            _mockRepository
                .Setup(r => r.QueryFraudEventsAsync(query))
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
    }
}
