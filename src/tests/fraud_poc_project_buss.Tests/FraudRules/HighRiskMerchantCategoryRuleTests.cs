using FluentAssertions;
using fraud_poc_project_buss;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for HighRiskMerchantCategoryRule - flags transactions in high-risk merchant categories.
    /// </summary>
    public class HighRiskMerchantCategoryRuleTests
    {
        [Fact]
        public void Evaluate_WithHighRiskCategory_Gambling_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("gambling")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithHighRiskCategory_Casino_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("casino")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithHighRiskCategory_Crypto_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("crypto")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithHighRiskCategory_Cryptocurrency_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("cryptocurrency")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithHighRiskCategory_MoneyTransfer_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("money_transfer")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithHighRiskCategory_WireTransfer_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("wire_transfer")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue();
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithLowRiskCategory_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("grocery")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithNullMerchantCategory_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory(null)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithEmptyMerchantCategory_DoesNotTrigger()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory(string.Empty)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse();
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithCaseInsensitivity_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("GAMBLING")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Rule should be case-insensitive");
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_WithMixedCase_Triggers()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("Money_Transfer")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Rule should handle mixed case");
            result.ScoreContribution.Should().Be(25m);
        }

        [Fact]
        public void Evaluate_ReturnsCorrectRuleMetadata()
        {
            // Arrange
            var rule = new HighRiskMerchantCategoryRule();
            var transaction = new TransactionEventBuilder()
                .WithMerchantCategory("casino")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("HIGH_RISK_MERCHANT");
            result.RuleDescription.Should().Contain("high-risk");
        }
    }
}
