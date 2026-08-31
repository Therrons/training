using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_repo.Connection;
using Moq;
using Xunit;

namespace fraud_poc_project_repo.Tests.Fixtures
{
    /// <summary>
    /// Base test class for repository and data access tests.
    /// Provides setup for database connections, mocking, and common test data.
    /// </summary>
    public abstract class BaseRepositoryTest : IAsyncLifetime
    {
        protected Mock<IDBConnection> _mockDbConnection = null!;
        protected Mock<Microsoft.Extensions.Configuration.IConfiguration> _mockConfiguration = null!;
        protected Mock<Microsoft.Extensions.Logging.ILogger<fraud_poc_project_repo.FraudRepository>> _mockLogger = null!;

        public virtual async Task InitializeAsync()
        {
            // Setup mocks
            _mockDbConnection = new Mock<IDBConnection>();
            _mockConfiguration = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
            _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<fraud_poc_project_repo.FraudRepository>>();

            // Setup default configuration values
            _mockConfiguration
                .Setup(x => x.GetConnectionString("PostgreSQL"))
                .Returns("Host=localhost;Port=5432;Database=fraud_test;Username=user;Password=pass");

            _mockConfiguration
                .Setup(x => x["Database:DBSchema"])
                .Returns("public");

            await Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            // Cleanup after each test
            await Task.CompletedTask;
        }

        // ============================================================
        // Test Data Factory Methods
        // ============================================================

        /// <summary>
        /// Creates a minimal valid TransactionEvent for repository testing.
        /// </summary>
        protected TransactionEvent CreateValidTransactionEvent(
            Guid? transactionId = null,
            string customerId = "CUST001",
            string accountId = "ACC001",
            decimal amount = 100m,
            string kafkaTopic = "transaction.events")
        {
            return new TransactionEvent
            {
                TransactionId = transactionId ?? Guid.NewGuid(),
                CustomerId = customerId,
                AccountId = accountId,
                Amount = amount,
                Currency = "ZAR",
                MerchantName = "Test Merchant",
                MerchantCategory = "5411",
                TransactionType = "POS",
                Channel = "CARD",
                CountryCode = "ZA",
                TransactionTime = DateTime.UtcNow,
                KafkaTopic = kafkaTopic,
                CorrelationId = Guid.NewGuid(),
                PublishedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        /// <summary>
        /// Creates a FraudEventRecord for repository testing.
        /// </summary>
        protected FraudEventRecord CreateFraudEventRecord(
            TransactionEvent? transactionEvent = null,
            bool isFlagged = true,
            decimal fraudScore = 50m,
            string? flaggedReason = "HIGH_AMOUNT, UNUSUAL_TIME")
        {
            return new FraudEventRecord
            {
                Event = transactionEvent ?? CreateValidTransactionEvent(),
                IsFlagged = isFlagged,
                FraudScore = fraudScore,
                FlaggedReason = flaggedReason,
                RuleResults = new List<FraudRuleSetRecord>
                {
                    new FraudRuleSetRecord
                    {
                        RuleCode = "HIGH_AMOUNT",
                        RuleDescription = "Transaction amount exceeds threshold",
                        IsTriggered = true,
                        ScoreContribution = 25m
                    },
                    new FraudRuleSetRecord
                    {
                        RuleCode = "UNUSUAL_TIME",
                        RuleDescription = "Transaction at unusual time",
                        IsTriggered = true,
                        ScoreContribution = 25m
                    }
                }
            };
        }

        /// <summary>
        /// Creates a batch of FraudEventRecords for testing queries.
        /// </summary>
        protected List<FraudEventRecord> CreateBatchOfFraudEventRecords(
            int count = 10,
            bool allFlagged = false,
            bool mixed = false)
        {
            var records = new List<FraudEventRecord>();

            for (int i = 0; i < count; i++)
            {
                bool isFlagged = allFlagged || (mixed && i % 2 == 0);

                records.Add(new FraudEventRecord
                {
                    Event = CreateValidTransactionEvent(
                        transactionId: Guid.NewGuid(),
                        customerId: $"CUST{i:000}",
                        amount: 100m + i * 10),
                    IsFlagged = isFlagged,
                    FraudScore = isFlagged ? 50m + i : 20m,
                    FlaggedReason = isFlagged ? $"RULE_{i}" : null,
                    RuleResults = new List<FraudRuleSetRecord>()
                });
            }

            return records;
        }
    }
}
