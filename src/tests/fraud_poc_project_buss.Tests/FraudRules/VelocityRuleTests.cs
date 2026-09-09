using FluentAssertions;
using fraud_poc_project_buss.Fraud.Rules;
using fraud_poc_project_buss.Models;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules;

/// <summary>
/// Tests for VelocityRule - detects multiple transactions in short time windows.
/// High velocity of transactions can indicate fraud or account compromise.
/// </summary>
public class VelocityRuleTests
{
    private readonly VelocityRule _rule = new();
    private const string TestCustomerId = "CUST-VELOCITY-001";

    [Fact]
    public void Evaluate_WithNoRecentTransactions_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(150.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>();

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithSingleRecentTransaction_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(150.00m)
            .WithTimestamp(DateTime.UtcNow)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithTimestamp(DateTime.UtcNow.AddMinutes(-5))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithMultipleTransactionsInShortWindow_ReturnsTrue()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithTimestamp(now.AddMinutes(-1))
                .Build(),
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithTimestamp(now.AddMinutes(-2))
                .Build(),
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithTimestamp(now.AddMinutes(-3))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithTransactionsOutsideWindow_ReturnsFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithTimestamp(now.AddHours(-2))  // Outside typical 1-hour window
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithDifferentCustomer_ReturnsFalse()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId("CUST-VELOCITY-002")
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)  // Different customer
                .WithTimestamp(now.AddMinutes(-1))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithExactlyThresholdTransactions_ReturnsTrue()
    {
        // Arrange - Test at the boundary of the threshold
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithTimestamp(now)
            .Build();

        // Create transactions exactly at the threshold (typically 3+ in 1 hour = fraud)
        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithTimestamp(now.AddMinutes(-1)).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithTimestamp(now.AddMinutes(-2)).Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeTrue("Three transactions in short window should be flagged");
    }
}
