using fraud_poc_project_buss.Models.Fraud;
using System;

namespace fraud_poc_project_buss.Tests
{
    /// <summary>
    /// Test data builder for creating TransactionEvent objects with sensible defaults.
    /// Reduces boilerplate in test setup and makes tests more readable.
    /// </summary>
    public class TransactionEventBuilder
    {
        private long _id = 1;
        private string _kafkaTopic = "transactions";
        private Guid _transactionId = Guid.NewGuid();
        private string _customerId = "CUST-12345";
        private string _accountId = "ACC-98765";
        private decimal _amount = 1000m;
        private string _currency = "ZAR";
        private string? _merchantName = "Test Merchant";
        private string? _merchantCategory = null;
        private string _transactionType = "POS";
        private string? _channel = null;
        private string? _countryCode = "ZA";
        private DateTime _transactionTime = DateTime.UtcNow;

        public TransactionEventBuilder WithId(long id)
        {
            _id = id;
            return this;
        }

        public TransactionEventBuilder WithKafkaTopic(string kafkaTopic)
        {
            _kafkaTopic = kafkaTopic;
            return this;
        }

        public TransactionEventBuilder WithTransactionId(Guid transactionId)
        {
            _transactionId = transactionId;
            return this;
        }

        public TransactionEventBuilder WithCustomerId(string customerId)
        {
            _customerId = customerId;
            return this;
        }

        public TransactionEventBuilder WithAccountId(string accountId)
        {
            _accountId = accountId;
            return this;
        }

        public TransactionEventBuilder WithAmount(decimal amount)
        {
            _amount = amount;
            return this;
        }

        public TransactionEventBuilder WithCurrency(string currency)
        {
            _currency = currency;
            return this;
        }

        public TransactionEventBuilder WithMerchantName(string? merchantName)
        {
            _merchantName = merchantName;
            return this;
        }

        public TransactionEventBuilder WithMerchantCategory(string? merchantCategory)
        {
            _merchantCategory = merchantCategory;
            return this;
        }

        public TransactionEventBuilder WithTransactionType(string transactionType)
        {
            _transactionType = transactionType;
            return this;
        }

        public TransactionEventBuilder WithChannel(string? channel)
        {
            _channel = channel;
            return this;
        }

        public TransactionEventBuilder WithCountryCode(string? countryCode)
        {
            _countryCode = countryCode;
            return this;
        }

        public TransactionEventBuilder WithTransactionTime(DateTime transactionTime)
        {
            _transactionTime = transactionTime;
            return this;
        }

        public TransactionEvent Build()
        {
            return new TransactionEvent
            {
                Id = _id,
                KafkaTopic = _kafkaTopic,
                TransactionId = _transactionId,
                CustomerId = _customerId,
                AccountId = _accountId,
                Amount = _amount,
                Currency = _currency,
                MerchantName = _merchantName,
                MerchantCategory = _merchantCategory,
                TransactionType = _transactionType,
                Channel = _channel,
                CountryCode = _countryCode,
                TransactionTime = _transactionTime,
                PublishedAt = DateTime.UtcNow,
                Version = "1.0",
                CorrelationId = Guid.NewGuid()
            };
        }
    }

    /// <summary>
    /// Test data builder for fraud evaluation results.
    /// </summary>
    public class FraudEventRecordBuilder
    {
        private TransactionEvent _event = new TransactionEventBuilder().Build();
        private List<FraudRuleSetRecord> _ruleResults = new();
        private bool _isFlagged = false;
        private decimal _fraudScore = 0m;
        private string? _flaggedReason = null;

        public FraudEventRecordBuilder WithEvent(TransactionEvent transactionEvent)
        {
            _event = transactionEvent;
            return this;
        }

        public FraudEventRecordBuilder WithRuleResults(List<FraudRuleSetRecord> ruleResults)
        {
            _ruleResults = ruleResults;
            return this;
        }

        public FraudEventRecordBuilder WithIsFlagged(bool isFlagged)
        {
            _isFlagged = isFlagged;
            return this;
        }

        public FraudEventRecordBuilder WithFraudScore(decimal fraudScore)
        {
            _fraudScore = fraudScore;
            return this;
        }

        public FraudEventRecordBuilder WithFlaggedReason(string? flaggedReason)
        {
            _flaggedReason = flaggedReason;
            return this;
        }

        public FraudEventRecord Build()
        {
            return new FraudEventRecord
            {
                Event = _event,
                RuleResults = _ruleResults,
                IsFlagged = _isFlagged,
                FraudScore = _fraudScore,
                FlaggedReason = _flaggedReason
            };
        }
    }

    /// <summary>
    /// Test data builder for fraud rule results.
    /// </summary>
    public class FraudRuleSetRecordBuilder
    {
        private long _fraudEventId = 1;
        private string _ruleCode = "TEST_RULE";
        private string _ruleDescription = "Test rule description";
        private bool _isTriggered = false;
        private decimal _scoreContribution = 0m;

        public FraudRuleSetRecordBuilder WithFraudEventId(long fraudEventId)
        {
            _fraudEventId = fraudEventId;
            return this;
        }

        public FraudRuleSetRecordBuilder WithRuleCode(string ruleCode)
        {
            _ruleCode = ruleCode;
            return this;
        }

        public FraudRuleSetRecordBuilder WithRuleDescription(string ruleDescription)
        {
            _ruleDescription = ruleDescription;
            return this;
        }

        public FraudRuleSetRecordBuilder WithIsTriggered(bool isTriggered)
        {
            _isTriggered = isTriggered;
            return this;
        }

        public FraudRuleSetRecordBuilder WithScoreContribution(decimal scoreContribution)
        {
            _scoreContribution = scoreContribution;
            return this;
        }

        public FraudRuleSetRecord Build()
        {
            return new FraudRuleSetRecord
            {
                FraudEventId = _fraudEventId,
                RuleCode = _ruleCode,
                RuleDescription = _ruleDescription,
                IsTriggered = _isTriggered,
                ScoreContribution = _scoreContribution
            };
        }
    }
}
