using FluentAssertions;
using fraud_poc_project_buss.Fraud;
using fraud_poc_project_buss.Models;
using fraud_poc_project_buss.Repositories;
using fraud_poc_project_ui.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace fraud_poc_project.Tests.Controllers;

/// <summary>
/// Tests for FraudController - API endpoint tests.
/// Verifies controller logic, response handling, and integration with services.
/// </summary>
public class FraudControllerTests
{
    private readonly Mock<IFraudEvaluationService> _mockFraudService;
    private readonly Mock<IFraudRepository> _mockRepository;
    private readonly Mock<IFraudProducer> _mockProducer;
    private readonly FraudController _controller;

    public FraudControllerTests()
    {
        _mockFraudService = new Mock<IFraudEvaluationService>();
        _mockRepository = new Mock<IFraudRepository>();
        _mockProducer = new Mock<IFraudProducer>();

        _controller = new FraudController(
            _mockFraudService.Object,
            _mockRepository.Object,
            _mockProducer.Object
        );
    }

    [Fact]
    public async Task EvaluateTransaction_WithValidTransaction_ReturnsOkResult()
    {
        // Arrange
        var transaction = new FraudTransactionEvent
        {
            CustomerId = "CUST-001",
            TransactionId = "TXN-001",
            Amount = 100m,
            Country = "ZA",
            Timestamp = DateTime.UtcNow
        };

        var fraudResult = new FraudEvaluationResult
        {
            TransactionId = "TXN-001",
            CustomerId = "CUST-001",
            IsFraud = false,
            RiskScore = 0.3m,
            FlaggedRules = new List<string>()
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(transaction.CustomerId, It.IsAny<int>()))
            .ReturnsAsync(new List<FraudTransactionEvent>());

        _mockFraudService
            .Setup(s => s.Evaluate(transaction, It.IsAny<List<FraudTransactionEvent>>()))
            .Returns(fraudResult);

        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(It.IsAny<FraudEvaluationResult>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.EvaluateTransaction(transaction);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult?.Value.Should().Be(fraudResult);
    }

    [Fact]
    public async Task EvaluateTransaction_WithNullTransaction_ReturnsBadRequest()
    {
        // Arrange & Act & Assert
        await _controller.Invoking(c => c.EvaluateTransaction(null!))
            .Should()
            .ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task EvaluateTransaction_WithFraudDetected_ReturnsFraudResult()
    {
        // Arrange
        var transaction = new FraudTransactionEvent
        {
            CustomerId = "CUST-001",
            TransactionId = "TXN-001",
            Amount = 5000m,  // Suspicious amount
            Country = "GB",
            Timestamp = DateTime.UtcNow
        };

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEvent
            {
                CustomerId = "CUST-001",
                TransactionId = "TXN-000",
                Amount = 100m,
                Country = "ZA",
                Timestamp = DateTime.UtcNow.AddMinutes(-5)
            }
        };

        var fraudResult = new FraudEvaluationResult
        {
            TransactionId = "TXN-001",
            CustomerId = "CUST-001",
            IsFraud = true,
            RiskScore = 0.9m,
            FlaggedRules = new List<string> { "AmountRule", "GeographicRule" }
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(transaction.CustomerId, It.IsAny<int>()))
            .ReturnsAsync(recentTransactions);

        _mockFraudService
            .Setup(s => s.Evaluate(transaction, recentTransactions))
            .Returns(fraudResult);

        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(fraudResult))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.EvaluateTransaction(transaction);

        // Assert
        var okResult = result as OkObjectResult;
        var returnedResult = okResult?.Value as FraudEvaluationResult;
        returnedResult?.IsFraud.Should().BeTrue();
        returnedResult?.RiskScore.Should().Be(0.9m);
    }

    [Fact]
    public async Task EvaluateTransaction_PublishesToKafka()
    {
        // Arrange
        var transaction = new FraudTransactionEvent
        {
            CustomerId = "CUST-001",
            TransactionId = "TXN-001",
            Amount = 100m,
            Country = "ZA",
            Timestamp = DateTime.UtcNow
        };

        var fraudResult = new FraudEvaluationResult
        {
            TransactionId = "TXN-001",
            CustomerId = "CUST-001",
            IsFraud = false,
            RiskScore = 0.2m,
            FlaggedRules = new List<string>()
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(transaction.CustomerId, It.IsAny<int>()))
            .ReturnsAsync(new List<FraudTransactionEvent>());

        _mockFraudService
            .Setup(s => s.Evaluate(It.IsAny<FraudTransactionEvent>(), It.IsAny<List<FraudTransactionEvent>>()))
            .Returns(fraudResult);

        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(It.IsAny<FraudEvaluationResult>()))
            .ReturnsAsync(true);

        // Act
        await _controller.EvaluateTransaction(transaction);

        // Assert
        _mockProducer.Verify(
            p => p.ProduceAsync(It.IsAny<FraudEvaluationResult>()),
            Times.Once,
            "Producer should be called to publish fraud result");
    }

    [Fact]
    public async Task GetTransactionHistory_WithValidCustomerId_ReturnsTransactions()
    {
        // Arrange
        const string customerId = "CUST-001";
        var transactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-001", Amount = 100m },
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-002", Amount = 150m }
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(customerId, It.IsAny<int>()))
            .ReturnsAsync(transactions);

        // Act
        var result = await _controller.GetTransactionHistory(customerId, 100);

        // Assert
        var okResult = result as OkObjectResult;
        var returnedTransactions = okResult?.Value as List<FraudTransactionEvent>;
        returnedTransactions?.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetTransactionHistory_WithEmptyHistory_ReturnsEmptyList()
    {
        // Arrange
        const string customerId = "NEW-CUSTOMER";

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(customerId, It.IsAny<int>()))
            .ReturnsAsync(new List<FraudTransactionEvent>());

        // Act
        var result = await _controller.GetTransactionHistory(customerId, 100);

        // Assert
        var okResult = result as OkObjectResult;
        var returnedTransactions = okResult?.Value as List<FraudTransactionEvent>;
        returnedTransactions?.Should().BeEmpty();
    }

    [Fact]
    public async Task EvaluateTransaction_SavesResultToRepository()
    {
        // Arrange
        var transaction = new FraudTransactionEvent
        {
            CustomerId = "CUST-001",
            TransactionId = "TXN-001",
            Amount = 100m,
            Country = "ZA",
            Timestamp = DateTime.UtcNow
        };

        var fraudResult = new FraudEvaluationResult
        {
            TransactionId = "TXN-001",
            CustomerId = "CUST-001",
            IsFraud = false,
            RiskScore = 0.2m,
            FlaggedRules = new List<string>()
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(transaction.CustomerId, It.IsAny<int>()))
            .ReturnsAsync(new List<FraudTransactionEvent>());

        _mockFraudService
            .Setup(s => s.Evaluate(transaction, It.IsAny<List<FraudTransactionEvent>>()))
            .Returns(fraudResult);

        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(fraudResult))
            .ReturnsAsync(true);

        // Act
        await _controller.EvaluateTransaction(transaction);

        // Assert
        _mockRepository.Verify(
            r => r.SaveFraudEventAsync(It.IsAny<FraudEvaluationResult>()),
            Times.Once,
            "Repository should save fraud evaluation result");
    }
}
