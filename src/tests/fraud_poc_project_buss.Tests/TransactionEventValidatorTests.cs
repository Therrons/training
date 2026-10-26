using FluentAssertions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Helper;
using Xunit;

namespace fraud_poc_project_buss.Tests
{
    public class TransactionEventValidatorTests
    {
        private static readonly DateTime FixedNow = new(2026, 10, 26, 12, 0, 0, DateTimeKind.Utc);

        private static TransactionEvent ValidEvent() => new TransactionEventBuilder()
            .WithTransactionTime(FixedNow.AddMinutes(-1))
            .Build();

        [Fact]
        public void Validate_ValidEvent_ReturnsNoErrors()
        {
            var errors = TransactionEventValidator.Validate(ValidEvent(), FixedNow);

            errors.Should().BeEmpty();
        }

        [Fact]
        public void Validate_EmptyTransactionId_ReturnsError()
        {
            var tx = ValidEvent();
            tx.TransactionId = Guid.Empty;

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.Contains("TransactionId"));
        }

        [Fact]
        public void Validate_MissingCustomerOrAccount_ReturnsErrors()
        {
            var tx = new TransactionEventBuilder()
                .WithCustomerId("  ")
                .WithAccountId("")
                .WithTransactionTime(FixedNow.AddMinutes(-1))
                .Build();

            var errors = TransactionEventValidator.Validate(tx, FixedNow);

            errors.Should().Contain(e => e.StartsWith("CustomerId"));
            errors.Should().Contain(e => e.StartsWith("AccountId"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(2_000_000_000_000)]
        public void Validate_AmountOutOfRange_ReturnsError(decimal amount)
        {
            var tx = new TransactionEventBuilder()
                .WithAmount(amount)
                .WithTransactionTime(FixedNow.AddMinutes(-1))
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.StartsWith("Amount"));
        }

        [Fact]
        public void Validate_NegativeAmount_IsAllowed()
        {
            var tx = new TransactionEventBuilder()
                .WithAmount(-250m)
                .WithTransactionTime(FixedNow.AddMinutes(-1))
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().BeEmpty();
        }

        [Theory]
        [InlineData("zar")]
        [InlineData("ZARR")]
        [InlineData("")]
        public void Validate_InvalidCurrency_ReturnsError(string currency)
        {
            var tx = new TransactionEventBuilder()
                .WithCurrency(currency)
                .WithTransactionTime(FixedNow.AddMinutes(-1))
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.StartsWith("Currency"));
        }

        [Fact]
        public void Validate_UnsupportedTransactionType_ReturnsError()
        {
            var tx = new TransactionEventBuilder()
                .WithTransactionType("CRYPTO_DRAIN")
                .WithTransactionTime(FixedNow.AddMinutes(-1))
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.Contains("CRYPTO_DRAIN"));
        }

        [Fact]
        public void Validate_TransactionTimeInFuture_ReturnsError()
        {
            var tx = new TransactionEventBuilder()
                .WithTransactionTime(FixedNow.AddHours(1))
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.Contains("future"));
        }

        [Fact]
        public void Validate_DefaultTransactionTime_ReturnsError()
        {
            var tx = new TransactionEventBuilder()
                .WithTransactionTime(default)
                .Build();

            TransactionEventValidator.Validate(tx, FixedNow).Should().Contain(e => e.StartsWith("TransactionTime"));
        }

        [Fact]
        public void NormalizeTransactionTime_UnspecifiedKind_BecomesUtc()
        {
            var tx = new TransactionEventBuilder()
                .WithTransactionTime(new DateTime(2026, 10, 26, 10, 0, 0, DateTimeKind.Unspecified))
                .Build();

            TransactionEventValidator.NormalizeTransactionTime(tx);

            tx.TransactionTime.Kind.Should().Be(DateTimeKind.Utc);
            tx.TransactionTime.Hour.Should().Be(10);
        }
    }
}
