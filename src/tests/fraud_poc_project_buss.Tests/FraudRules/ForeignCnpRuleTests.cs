using FluentAssertions;
using fraud_poc_project_buss;
using Xunit;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for ForeignCnpRule - flags card-not-present transactions from foreign countries.
    /// </summary>
    public class ForeignCnpRuleTests
    {
        [Fact]
        public void Evaluate_WithDomesticCnp_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode("ZA")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Domestic CNP should not trigger");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithForeignCnp_Triggers()
        {
            // Arrange
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Foreign CNP should trigger");
            result.ScoreContribution.Should().Be(35m);
        }

        [Fact]
        public void Evaluate_WithForeignNonCnp_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("POS")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Non-CNP transaction should not trigger regardless of country");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithOnlineChannelAndForeignCountry_Triggers()
        {
            // Arrange - Online transactions are treated as CNP
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithChannel("Online")
                .WithCountryCode("GB")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Online + foreign = CNP + foreign = trigger");
            result.ScoreContribution.Should().Be(35m);
        }

        [Fact]
        public void Evaluate_WithOnlineChannelAndDomesticCountry_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithChannel("Online")
                .WithCountryCode("ZA")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Domestic online should not trigger");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithNullCountryCode_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(homeCountry: "ZA");
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode(null)
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("No country code means it's not foreign");
            result.ScoreContribution.Should().Be(0m);
        }

        [Fact]
        public void Evaluate_WithCaseSensitivities_TriggersCorrectly()
        {
            // Arrange - Test case-insensitivity
            var rule = new ForeignCnpRule(homeCountry: "za");
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("cnp")
                .WithCountryCode("us")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Rule should be case-insensitive");
            result.ScoreContribution.Should().Be(35m);
        }

        [Fact]
        public void Evaluate_WithDefaultHomeCountry_UsesZa()
        {
            // Arrange
            var rule = new ForeignCnpRule();
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeTrue("Default home country is ZA");
        }

        [Fact]
        public void Evaluate_ReturnsCorrectRuleMetadata()
        {
            // Arrange
            var rule = new ForeignCnpRule();
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.RuleCode.Should().Be("FOREIGN_CNP");
            result.RuleDescription.Should().Contain("Card-not-present");
        }
    }
}
