using fraud_poc_project_buss.Models.Fraud;
using Xunit;

namespace fraud_poc_project.Tests.Fixtures
{
    /// <summary>
    /// Base test class for API/Controller tests.
    /// Provides setup for WebApplicationFactory, HTTP clients, and common test data.
    /// </summary>
    public abstract class BaseApiTest : IAsyncLifetime
    {
        protected WebApplicationFactory? _factory;
        protected HttpClient? _client;

        public virtual async Task InitializeAsync()
        {
            // Initialize WebApplicationFactory
            // TODO: Setup custom factory that overrides services with test doubles
            // _factory = new WebApplicationFactory<Program>()
            //     .WithWebHostBuilder(builder =>
            //     {
            //         builder.ConfigureServices(services =>
            //         {
            //             // Remove real dependencies, add test doubles
            //         });
            //     });

            // _client = _factory.CreateClient();

            await Task.CompletedTask;
        }

        public virtual async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            await Task.CompletedTask;
        }

        // ============================================================
        // Test Data Factory Methods
        // ============================================================

        /// <summary>
        /// Creates a minimal valid TransactionEvent for API testing.
        /// </summary>
        protected TransactionEvent CreateValidTransactionEvent(
            Guid? transactionId = null,
            string customerId = "CUST001",
            decimal amount = 100m)
        {
            return new TransactionEvent
            {
                TransactionId = transactionId ?? Guid.NewGuid(),
                CustomerId = customerId,
                AccountId = "ACC001",
                Amount = amount,
                Currency = "ZAR",
                MerchantName = "Test Merchant",
                MerchantCategory = "5411",
                TransactionType = "POS",
                Channel = "CARD",
                CountryCode = "ZA",
                TransactionTime = DateTime.UtcNow,
                KafkaTopic = "transaction.events",
                CorrelationId = Guid.NewGuid(),
                PublishedAt = DateTime.UtcNow,
                Version = "1.0"
            };
        }

        /// <summary>
        /// Creates a FraudEventRecord for API response verification.
        /// </summary>
        protected FraudEventRecord CreateFraudEventRecord(
            Guid? transactionId = null,
            bool isFlagged = true,
            decimal fraudScore = 50m)
        {
            return new FraudEventRecord
            {
                Event = CreateValidTransactionEvent(transactionId: transactionId),
                IsFlagged = isFlagged,
                FraudScore = fraudScore,
                FlaggedReason = isFlagged ? "HIGH_AMOUNT, UNUSUAL_TIME" : null,
                RuleResults = new List<FraudRuleSetRecord>
                {
                    new FraudRuleSetRecord
                    {
                        RuleCode = "HIGH_AMOUNT",
                        IsTriggered = true,
                        ScoreContribution = 25m
                    },
                    new FraudRuleSetRecord
                    {
                        RuleCode = "UNUSUAL_TIME",
                        IsTriggered = true,
                        ScoreContribution = 25m
                    }
                }
            };
        }

        /// <summary>
        /// Creates a batch of FraudEventRecords for API list response verification.
        /// </summary>
        protected List<FraudEventRecord> CreateBatchOfFraudEventRecords(int count = 10)
        {
            var records = new List<FraudEventRecord>();

            for (int i = 0; i < count; i++)
            {
                bool isFlagged = i % 3 == 0; // 1 in 3 flagged

                records.Add(new FraudEventRecord
                {
                    Event = CreateValidTransactionEvent(
                        transactionId: Guid.NewGuid(),
                        customerId: $"CUST{i:000}",
                        amount: 100m + i * 10),
                    IsFlagged = isFlagged,
                    FraudScore = isFlagged ? 50m : 20m,
                    FlaggedReason = isFlagged ? "RULE_TRIGGERED" : null,
                    RuleResults = new List<FraudRuleSetRecord>()
                });
            }

            return records;
        }
    }
}
