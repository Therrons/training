using FluentAssertions;
using Microsoft.Extensions.Options;
using Xunit;
using fraud_poc_project_buss.Models.Settings;

namespace fraud_poc_project_buss.Tests.FraudRules
{
    /// <summary>
    /// Tests for ForeignCnpRule - flags card-not-present transactions from foreign countries.
    /// </summary>
    public class ForeignCnpRuleTests
    {
        private IOptions<FraudRulesSettings> CreateOptions(string homeCountry = "ZA", bool enabled = true)
        {
            var settings = new FraudRulesSettings
            {
                ForeignCnp = new ForeignCnpRuleSettings
                {
                    HomeCountry = homeCountry,
                    ScoreContribution = 35m,
                    Enabled = enabled
                }
            };
            return Options.Create(settings);
        }

        [Fact]
        public void Evaluate_WithDomesticCnp_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
            var rule = new ForeignCnpRule(CreateOptions("za"));
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
        public void Evaluate_WithDisabledRule_DoesNotTrigger()
        {
            // Arrange
            var rule = new ForeignCnpRule(CreateOptions("ZA", enabled: false));
            var transaction = new TransactionEventBuilder()
                .WithTransactionType("CNP")
                .WithCountryCode("US")
                .Build();

            // Act
            var result = rule.Evaluate(transaction);

            // Assert
            result.IsTriggered.Should().BeFalse("Disabled rule should not trigger");
            result.ScoreContribution.Should().Be(0m);
            result.RuleDescription.Should().Be("Rule disabled");
        }

        [Fact]
        public void Evaluate_ReturnsCorrectRuleMetadata()
        {
            // Arrange
            var rule = new ForeignCnpRule(CreateOptions("ZA"));
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
