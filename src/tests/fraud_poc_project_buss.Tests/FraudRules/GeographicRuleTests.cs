using FluentAssertions;
using fraud_poc_project_buss.Fraud.Rules;
using fraud_poc_project_buss.Models;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules;

/// <summary>
/// Tests for GeographicRule - detects impossible travel patterns.
/// Transactions from distant locations in unrealistic time windows are flagged.
/// </summary>
public class GeographicRuleTests
{
    private readonly GeographicRule _rule = new();
    private const string TestCustomerId = "CUST-GEO-001";

    [Fact]
    public void Evaluate_WithSameCountryTransactions_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithCountry("ZA")
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().WithCustomerId(TestCustomerId).WithCountry("ZA").Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void Evaluate_WithImpossibleTravel_ReturnsTrue()
    {
        // Arrange - Transaction from SA to UK in 5 minutes (impossible)
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithCountry("GB")
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithCountry("ZA")
                .WithTimestamp(now.AddMinutes(-5))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeTrue("Travel from ZA to GB in 5 minutes is impossible");
    }

    [Fact]
    public void Evaluate_WithReasonableTravelTime_ReturnsFalse()
    {
        // Arrange - Transaction from SA to UK with reasonable travel time (24+ hours)
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithCountry("GB")
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithCountry("ZA")
                .WithTimestamp(now.AddHours(-25))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse("25 hours is sufficient for travel from ZA to GB");
    }

    [Fact]
    public void Evaluate_WithNoRecentTransactions_ReturnsFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithCountry("US")
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>();

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse("No previous transaction to compare against");
    }

    [Fact]
    public void Evaluate_WithMultipleCountriesReasonableTime_ReturnsFalse()
    {
        // Arrange - Realistic business travel
        var now = DateTime.UtcNow;
        var transaction = new FraudTransactionEventBuilder()
            .WithCustomerId(TestCustomerId)
            .WithCountry("US")
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)
                .WithCountry("ZA")
                .WithTimestamp(now.AddDays(-1))
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
            .WithCustomerId("CUST-GEO-002")
            .WithCountry("GB")
            .WithTimestamp(now)
            .Build();

        var recentTransactions = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder()
                .WithCustomerId(TestCustomerId)  // Different customer
                .WithCountry("ZA")
                .WithTimestamp(now.AddMinutes(-5))
                .Build()
        };

        // Act
        var result = _rule.Evaluate(transaction, recentTransactions);

        // Assert
        result.Should().BeFalse("Different customers should not be compared");
    }
}
