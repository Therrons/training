using FluentAssertions;
using fraud_poc_project_buss.Fraud.Rules;
using fraud_poc_project_buss.Models;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules;

/// <summary>
/// Tests for AmountRule - detects unusually high transaction amounts.
/// Transactions significantly above customer's typical spending are flagged.
/// </summary>
public class AmountRuleTests
{
    private readonly AmountRule _rule = new();
    private const string TestCustomerId = "CUST-AMOUNT-001";

    [Fact]
    public void Evaluate_WithNormalAmount_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(100.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(95.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(105.00m).Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithExcessivelyHighAmount_ReturnsTrue()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(5000.00m)  // Very high
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(50.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(75.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(100.00m).Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Evaluate_WithZeroAmount_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(0m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>();

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithNegativeAmount_ReturnsFalse()
    {
        // Arrange - Refunds should typically not be flagged as fraud
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(-100.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>();

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithNoTransactionHistory_ReturnsFalse()
    {
        // Arrange - First transaction should give benefit of doubt
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId("NEW-CUSTOMER")
            .WithAmount(500.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>();

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse("New customers should not be immediately flagged");
    }

    [Fact]
    public void Evaluate_WithAmountAtThreshold_ReturnsTrue()
    {
        // Arrange - Test boundary condition
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(1000.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(100.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(150.00m).Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeTrue("Amount 10x typical should be flagged");
    }

    [Fact]
    public void Evaluate_WithSmallConsistentAmounts_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithAmount(25.00m)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(20.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(30.00m).Build(),
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithAmount(25.00m).Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }
}
