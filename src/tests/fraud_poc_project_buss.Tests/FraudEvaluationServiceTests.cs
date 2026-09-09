using FluentAssertions;
using fraud_poc_project_buss.Fraud;
using fraud_poc_project_buss.Fraud.Rules;
using fraud_poc_project_buss.Models;
using Moq;
using Xunit;

namespace fraud_poc_project_buss.Tests;

/// <summary>
/// Tests for FraudEvaluationService - orchestrates fraud rule evaluation.
/// Verifies rule aggregation, scoring, and decision logic.
/// </summary>
public class FraudEvaluationServiceTests
{
    private readonly Mock<IFraudRule> _mockRule1;
    private readonly Mock<IFraudRule> _mockRule2;
    private readonly Mock<IFraudRule> _mockRule3;
    private readonly FraudEvaluationService _service;

    public FraudEvaluationServiceTests()
    {
        _mockRule1 = new Mock<IFraudRule>();
        _mockRule2 = new Mock<IFraudRule>();
        _mockRule3 = new Mock<IFraudRule>();

        var rules = new List<IFraudRule> { _mockRule1.Object, _mockRule2.Object, _mockRule3.Object };
        _service = new FraudEvaluationService(rules);
    }

    [Fact]
    public void Evaluate_WithNoRulesFlagged_ReturnsFraudFalse()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.IsFraud.Should().BeFalse();
        result.RiskScore.Should().Be(0m);
        result.FlaggedRules.Should().BeEmpty();
    }

    [Fact]
    public void Evaluate_WithOneRuleFlagged_ReturnsPartialRisk()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule1.Setup(r => r.RuleName).Returns("Rule1");

        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.IsFraud.Should().BeFalse("Single rule flag should not mark as fraud");
        result.RiskScore.Should().BeGreaterThan(0);
        result.FlaggedRules.Should().Contain("Rule1");
    }

    [Fact]
    public void Evaluate_WithMultipleRulesFlagged_ReturnsFraudTrue()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule1.Setup(r => r.RuleName).Returns("VelocityRule");

        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule2.Setup(r => r.RuleName).Returns("AmountRule");

        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.IsFraud.Should().BeTrue("Multiple flagged rules indicate fraud");
        result.RiskScore.Should().BeGreaterThan(0.5m);
        result.FlaggedRules.Should().HaveCount(2);
        result.FlaggedRules.Should().Contain(new[] { "VelocityRule", "AmountRule" });
    }

    [Fact]
    public void Evaluate_WithAllRulesFlagged_ReturnsHighRiskScore()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule1.Setup(r => r.RuleName).Returns("Rule1");

        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule2.Setup(r => r.RuleName).Returns("Rule2");

        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule3.Setup(r => r.RuleName).Returns("Rule3");

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.IsFraud.Should().BeTrue();
        result.RiskScore.Should().Be(1m);  // 3 rules / 3 total = 1.0
        result.FlaggedRules.Should().HaveCount(3);
    }

    [Fact]
    public void Evaluate_WithEmptyRulesList_ReturnsFraudFalse()
    {
        // Arrange
        var serviceWithNoRules = new FraudEvaluationService(new List<IFraudRule>());
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        // Act
        var result = serviceWithNoRules.Evaluate(transaction, history);

        // Assert
        result.IsFraud.Should().BeFalse();
        result.RiskScore.Should().Be(0m);
    }

    [Fact]
    public void Evaluate_PopulatesTransactionAndCustomerIds()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder()
            .WithTransactionId("TXN-123")
            .WithCustomerId("CUST-456")
            .Build();

        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.TransactionId.Should().Be("TXN-123");
        result.CustomerId.Should().Be("CUST-456");
    }

    [Fact]
    public void Evaluate_CallsAllRulesWithCorrectParameters()
    {
        // Arrange
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>
        {
            new FraudTransactionEventBuilder().Build()
        };

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(false);
        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        _service.Evaluate(transaction, history);

        // Assert
        _mockRule1.Verify(r => r.Evaluate(transaction, history), Times.Once);
        _mockRule2.Verify(r => r.Evaluate(transaction, history), Times.Once);
        _mockRule3.Verify(r => r.Evaluate(transaction, history), Times.Once);
    }

    [Fact]
    public void Evaluate_RiskScoreIncrementsCorrectly()
    {
        // Arrange - Test risk score calculation with partial flagging
        var transaction = new FraudTransactionEventBuilder().Build();
        var history = new List<FraudTransactionEvent>();

        _mockRule1.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule1.Setup(r => r.RuleName).Returns("Rule1");

        _mockRule2.Setup(r => r.Evaluate(transaction, history)).Returns(true);
        _mockRule2.Setup(r => r.RuleName).Returns("Rule2");

        _mockRule3.Setup(r => r.Evaluate(transaction, history)).Returns(false);

        // Act
        var result = _service.Evaluate(transaction, history);

        // Assert
        result.RiskScore.Should().Be(0.667m, "2 out of 3 rules = 66.7%");
    }
}
