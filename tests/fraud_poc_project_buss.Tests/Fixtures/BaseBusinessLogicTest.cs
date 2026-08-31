using fraud_poc_project_buss.Models.Fraud;
using Xunit;

namespace fraud_poc_project_buss.Tests.Fixtures
{
    /// <summary>
    /// Base test class for business logic tests.
    /// Provides common setup/teardown and test data factories.
    /// </summary>
    public abstract class BaseBusinessLogicTest : IDisposable
    {
        protected bool _disposed = false;

        public BaseBusinessLogicTest()
        {
            // Setup called before each test
            Setup();
        }

        /// <summary>
        /// Called before each test method runs.
        /// Override to add custom initialization logic.
        /// </summary>
        protected virtual void Setup()
        {
            // Base implementation - override in derived classes as needed
        }

        /// <summary>
        /// Called after each test method completes.
        /// Use for cleanup of test resources.
        /// </summary>
        protected virtual void Cleanup()
        {
            // Base implementation - override in derived classes as needed
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Cleanup();
                }
                _disposed = true;
            }
        }

        // ============================================================
        // Test Data Factory Methods
        // ============================================================

        /// <summary>
        /// Creates a minimal valid TransactionEvent for testing.
        /// </summary>
        protected TransactionEvent CreateValidTransactionEvent(
            Guid? transactionId = null,
            string customerId = "CUST001",
            string accountId = "ACC001",
            decimal amount = 100m,
            string currency = "ZAR",
            string? merchantName = "Test Merchant",
            string transactionType = "POS",
            DateTime? transactionTime = null)
        {
            return new TransactionEvent
            {
                TransactionId = transactionId ?? Guid.NewGuid(),
                CustomerId = customerId,
                AccountId = accountId,
                Amount = amount,
                Currency = currency,
                MerchantName = merchantName,
                MerchantCategory = "5411", // Grocery store
                TransactionType = transactionType,
                Channel = "CARD",
                CountryCode = "ZA",
                TransactionTime = transactionTime ?? DateTime.UtcNow,
                KafkaTopic = "transaction.events",
                CorrelationId = Guid.NewGuid(),
                PublishedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        /// <summary>
        /// Creates a high-value transaction that may trigger fraud rules.
        /// </summary>
        protected TransactionEvent CreateHighValueTransactionEvent(
            decimal amount = 50000m,
            string? merchantName = "Luxury Retailer")
        {
            return CreateValidTransactionEvent(
                amount: amount,
                merchantName: merchantName);
        }

        /// <summary>
        /// Creates an unusual geographic transaction (different country).
        /// </summary>
        protected TransactionEvent CreateForeignTransactionEvent(
            string countryCode = "US",
            string? merchantName = "International Merchant")
        {
            var tx = CreateValidTransactionEvent(
                merchantName: merchantName);
            tx.CountryCode = countryCode;
            return tx;
        }

        /// <summary>
        /// Creates a fraud rule result record for testing.
        /// </summary>
        protected FraudRuleSetRecord CreateFraudRuleResult(
            string ruleCode = "HIGH_AMOUNT",
            string ruleDescription = "Transaction amount exceeds threshold",
            bool isTriggered = true,
            decimal scoreContribution = 25m)
        {
            return new FraudRuleSetRecord
            {
                RuleCode = ruleCode,
                RuleDescription = ruleDescription,
                IsTriggered = isTriggered,
                ScoreContribution = scoreContribution
            };
        }

        /// <summary>
        /// Creates a fraud event record with result.
        /// </summary>
        protected FraudEventRecord CreateFraudEventRecord(
            TransactionEvent? transactionEvent = null,
            bool isFlagged = false,
            decimal fraudScore = 0m,
            string? flaggedReason = null)
        {
            return new FraudEventRecord
            {
                Event = transactionEvent ?? CreateValidTransactionEvent(),
                IsFlagged = isFlagged,
                FraudScore = fraudScore,
                FlaggedReason = flaggedReason,
                RuleResults = new List<FraudRuleSetRecord>()
            };
        }
    }
}
