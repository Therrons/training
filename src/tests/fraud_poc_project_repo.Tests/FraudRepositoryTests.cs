using FluentAssertions;
using fraud_poc_project_buss.Models;
using fraud_poc_project_buss.Repositories;
using Moq;
using Xunit;

namespace fraud_poc_project_repo.Tests;

/// <summary>
/// Tests for IFraudRepository interface implementation.
/// These tests focus on repository contracts and behaviors.
/// Integration tests with actual database should be in separate file.
/// </summary>
public class FraudRepositoryTests
{
    private readonly Mock<IFraudRepository> _mockRepository;

    public FraudRepositoryTests()
    {
        _mockRepository = new Mock<IFraudRepository>();
    }

    [Fact]
    public void SaveFraudEvent_WithValidEvent_ShouldSucceed()
    {
        // Arrange
        var fraudEvent = new FraudEvaluationResult
        {
            TransactionId = "TXN-001",
            CustomerId = "CUST-001",
            RiskScore = 0.8m,
            IsFraud = true,
            FlaggedRules = new List<string> { "VelocityRule", "AmountRule" },
            EvaluatedAt = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(fraudEvent))
            .ReturnsAsync(true);

        // Act
        var result = _mockRepository.Object.SaveFraudEventAsync(fraudEvent);

        // Assert
        result.Result.Should().BeTrue();
        _mockRepository.Verify(r => r.SaveFraudEventAsync(fraudEvent), Times.Once);
    }

    [Fact]
    public void SaveFraudEvent_WithNullEvent_ShouldThrow()
    {
        // Arrange
        _mockRepository
            .Setup(r => r.SaveFraudEventAsync(null!))
            .ThrowsAsync(new ArgumentNullException(nameof(FraudEvaluationResult)));

        // Act & Assert
        _mockRepository.Object.Invoking(r => r.SaveFraudEventAsync(null!)).Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetCustomerTransactionHistory_WithValidCustomerId_ReturnsTransactions()
    {
        // Arrange
        const string customerId = "CUST-001";
        var transactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEvent
            {
                CustomerId = customerId,
                TransactionId = "TXN-001",
                Amount = 100m,
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new FraudTransactionEvent
            {
                CustomerId = customerId,
                TransactionId = "TXN-002",
                Amount = 150m,
                Timestamp = DateTime.UtcNow.AddHours(-2)
            }
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(customerId, It.IsAny<int>()))
            .ReturnsAsync(transactions);

        // Act
        var result = _mockRepository.Object.GetCustomerTransactionHistoryAsync(customerId, 100).Result;

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t => t.CustomerId.Should().Be(customerId));
    }

    [Fact]
    public void GetCustomerTransactionHistory_WithNoTransactions_ReturnsEmptyList()
    {
        // Arrange
        const string customerId = "NEW-CUSTOMER";
        var emptyTransactions = new List<FraudTransactionEvent>();

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(customerId, It.IsAny<int>()))
            .ReturnsAsync(emptyTransactions);

        // Act
        var result = _mockRepository.Object.GetCustomerTransactionHistoryAsync(customerId, 100).Result;

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetCustomerTransactionHistory_WithTimeLimit_ReturnsRecentTransactionsOnly()
    {
        // Arrange
        const string customerId = "CUST-001";
        var now = DateTime.UtcNow;
        var transactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-001", Timestamp = now.AddHours(-1) },
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-002", Timestamp = now.AddHours(-2) }
        };

        _mockRepository
            .Setup(r => r.GetCustomerTransactionHistoryAsync(customerId, It.IsAny<int>()))
            .ReturnsAsync((string cid, int limit) =>
                transactions.Where(t => (now - t.Timestamp).TotalHours <= 24).ToList());

        // Act
        var result = _mockRepository.Object.GetCustomerTransactionHistoryAsync(customerId, 24).Result;

        // Assert
        result.Should().HaveCount(2);
    }

    [Fact]
    public void SaveTransactionEvent_WithValidEvent_ShouldSucceed()
    {
        // Arrange
        var transaction = new FraudTransactionEvent
        {
            CustomerId = "CUST-001",
            TransactionId = "TXN-001",
            Amount = 100m,
            Country = "ZA",
            MerchantId = "MERCH-001",
            Timestamp = DateTime.UtcNow
        };

        _mockRepository
            .Setup(r => r.SaveTransactionEventAsync(transaction))
            .ReturnsAsync(true);

        // Act
        var result = _mockRepository.Object.SaveTransactionEventAsync(transaction).Result;

        // Assert
        result.Should().BeTrue();
        _mockRepository.Verify(r => r.SaveTransactionEventAsync(transaction), Times.Once);
    }

    [Fact]
    public void GetRecentTransactionsForCustomer_WithSpecificTimeWindow_ReturnsFilteredResults()
    {
        // Arrange
        const string customerId = "CUST-001";
        var now = DateTime.UtcNow;
        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-001", Timestamp = now.AddMinutes(-5) },
            new FraudTransactionEvent { CustomerId = customerId, TransactionId = "TXN-002", Timestamp = now.AddMinutes(-10) }
        };

        _mockRepository
            .Setup(r => r.GetRecentTransactionsAsync(customerId, It.IsAny<TimeSpan>()))
            .ReturnsAsync(recentTransactions);

        // Act
        var result = _mockRepository.Object.GetRecentTransactionsAsync(customerId, TimeSpan.FromMinutes(15)).Result;

        // Assert
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(t =>
        {
            t.CustomerId.Should().Be(customerId);
            (now - t.Timestamp).Should().BeLessThan(TimeSpan.FromMinutes(15));
        });
    }
}
