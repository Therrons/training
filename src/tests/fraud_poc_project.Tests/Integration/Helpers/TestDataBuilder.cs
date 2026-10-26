using fraud_poc_project_buss.Dto;

namespace fraud_poc_project.Tests.Integration.Helpers
{
    /// <summary>
    /// Builder for creating test transaction data for integration tests
    /// Provides fluent API for building realistic transaction scenarios
    /// </summary>
    public class TestDataBuilder
    {
        private Guid _transactionId = Guid.NewGuid();
        private string _customerId = "CUST-TEST-001";
        private decimal _amount = 1000;
        private string _countryCode = "ZA";
        private string _transactionType = "PURCHASE";
        private string _merchantCategory = "RETAIL";
        private DateTime _transactionTime = DateTime.UtcNow;

        /// <summary>
        /// Creates a new test data builder
        /// </summary>
        public static TestDataBuilder Create() => new TestDataBuilder();

        /// <summary>
        /// Sets a custom transaction ID
        /// </summary>
        public TestDataBuilder WithTransactionId(Guid transactionId)
        {
            _transactionId = transactionId;
            return this;
        }

        /// <summary>
        /// Sets a custom customer ID
        /// </summary>
        public TestDataBuilder WithCustomerId(string customerId)
        {
            _customerId = customerId;
            return this;
        }

        /// <summary>
        /// Sets the transaction amount
        /// </summary>
        public TestDataBuilder WithAmount(decimal amount)
        {
            _amount = amount;
            return this;
        }

        /// <summary>
        /// Sets the country code
        /// </summary>
        public TestDataBuilder WithCountryCode(string countryCode)
        {
            _countryCode = countryCode;
            return this;
        }

        /// <summary>
        /// Sets the transaction type (e.g., PURCHASE, ATM_WITHDRAWAL)
        /// </summary>
        public TestDataBuilder WithTransactionType(string transactionType)
        {
            _transactionType = transactionType;
            return this;
        }

        /// <summary>
        /// Sets the merchant category
        /// </summary>
        public TestDataBuilder WithMerchantCategory(string merchantCategory)
        {
            _merchantCategory = merchantCategory;
            return this;
        }

        /// <summary>
        /// Sets the transaction time
        /// </summary>
        public TestDataBuilder WithTransactionTime(DateTime transactionTime)
        {
            _transactionTime = transactionTime;
            return this;
        }

        /// <summary>
        /// Builds a high-value transaction (designed to trigger fraud rules)
        /// </summary>
        public static TestDataBuilder BuildHighValueTransaction(decimal amount = 5000)
        {
            return Create()
                .WithAmount(amount);
        }

        /// <summary>
        /// Builds an international transaction (designed to trigger fraud rules)
        /// </summary>
        public static TestDataBuilder BuildInternationalTransaction(
            string fromCountry = "US",
            string toCountry = "ZA",
            decimal amount = 2000)
        {
            return Create()
                .WithCountryCode(fromCountry)
                .WithAmount(amount);
        }

        /// <summary>
        /// Builds an ATM withdrawal transaction
        /// </summary>
        public static TestDataBuilder BuildAtmWithdrawal(decimal amount = 5000)
        {
            return Create()
                .WithTransactionType("ATM_WITHDRAWAL")
                .WithAmount(amount)
                .WithMerchantCategory("ATM");
        }

        /// <summary>
        /// Builds a high-risk scenario transaction (multiple fraud triggers)
        /// </summary>
        public static TestDataBuilder BuildHighRiskScenario()
        {
            return Create()
                .WithAmount(50000)
                .WithCountryCode("NG") // High-risk country
                .WithTransactionType("TRANSFER")
                .WithMerchantCategory("GAMBLING"); // High-risk merchant
        }

        /// <summary>
        /// Builds a normal, low-risk transaction
        /// </summary>
        public static TestDataBuilder BuildNormalTransaction(decimal amount = 100)
        {
            return Create()
                .WithAmount(amount)
                .WithCountryCode("ZA")
                .WithTransactionType("PURCHASE")
                .WithMerchantCategory("RETAIL");
        }

        /// <summary>
        /// Builds the transaction event
        /// </summary>
        public TransactionEvent Build()
        {
            return new TransactionEvent
            {
                TransactionId = _transactionId,
                CustomerId = _customerId,
                Amount = _amount,
                CountryCode = _countryCode,
                TransactionType = _transactionType,
                MerchantCategory = _merchantCategory,
                TransactionTime = _transactionTime
            };
        }
    }

    /// <summary>
    /// Builder for test scenarios with multiple transactions
    /// </summary>
    public class TestScenarioBuilder
    {
        private List<TransactionEvent> _transactions = new List<TransactionEvent>();
        private string _scenarioName = "Test Scenario";

        /// <summary>
        /// Creates a new scenario builder
        /// </summary>
        public static TestScenarioBuilder Create(string name = "Test Scenario")
        {
            return new TestScenarioBuilder { _scenarioName = name };
        }

        /// <summary>
        /// Adds a transaction from a builder
        /// </summary>
        public TestScenarioBuilder AddTransaction(TestDataBuilder builder)
        {
            _transactions.Add(builder.Build());
            return this;
        }
    }
}
