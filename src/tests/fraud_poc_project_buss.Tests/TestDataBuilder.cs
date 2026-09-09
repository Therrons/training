using fraud_poc_project_buss.Models;
using System;
using System.Collections.Generic;

namespace fraud_poc_project_buss.Tests;

/// <summary>
/// Test data builder for creating fraud transaction events with sensible defaults.
/// Reduces boilerplate in test setup and makes tests more readable.
/// </summary>
public class FraudTransactionEventBuilder
{
    private string _customerId = "CUST-12345";
    private string _transactionId = "TXN-98765";
    private decimal _amount = 100.00m;
    private string _merchantId = "MERCH-001";
    private string _country = "ZA";
    private DateTime _timestamp = DateTime.UtcNow;
    private string _description = "Test transaction";

    public FraudTransactionEventBuilder WithCustomerId(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public FraudTransactionEventBuilder WithAmount(decimal amount)
    {
        _amount = amount;
        return this;
    }

    public FraudTransactionEventBuilder WithMerchantId(string merchantId)
    {
        _merchantId = merchantId;
        return this;
    }

    public FraudTransactionEventBuilder WithCountry(string country)
    {
        _country = country;
        return this;
    }

    public FraudTransactionEventBuilder WithTimestamp(DateTime timestamp)
    {
        _timestamp = timestamp;
        return this;
    }

    public FraudTransactionEventBuilder WithDescription(string description)
    {
        _description = description;
        return this;
    }

    public FraudTransactionEvent Build()
    {
        return new FraudTransactionEvent
        {
            CustomerId = _customerId,
            TransactionId = _transactionId,
            Amount = _amount,
            MerchantId = _merchantId,
            Country = _country,
            Timestamp = _timestamp,
            Description = _description,
            ProcessedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Test data builder for fraud evaluation results.
/// </summary>
public class FraudEvaluationResultBuilder
{
    private string _transactionId = "TXN-98765";
    private string _customerId = "CUST-12345";
    private decimal _riskScore = 0.5m;
    private bool _isFraud = false;
    private List<string> _flaggedRules = new();
    private DateTime _evaluatedAt = DateTime.UtcNow;

    public FraudEvaluationResultBuilder WithTransactionId(string transactionId)
    {
        _transactionId = transactionId;
        return this;
    }

    public FraudEvaluationResultBuilder WithCustomerId(string customerId)
    {
        _customerId = customerId;
        return this;
    }

    public FraudEvaluationResultBuilder WithRiskScore(decimal riskScore)
    {
        _riskScore = riskScore;
        return this;
    }

    public FraudEvaluationResultBuilder WithIsFraud(bool isFraud)
    {
        _isFraud = isFraud;
        return this;
    }

    public FraudEvaluationResultBuilder WithFlaggedRules(params string[] rules)
    {
        _flaggedRules = new List<string>(rules);
        return this;
    }

    public FraudEvaluationResult Build()
    {
        return new FraudEvaluationResult
        {
            TransactionId = _transactionId,
            CustomerId = _customerId,
            RiskScore = _riskScore,
            IsFraud = _isFraud,
            FlaggedRules = _flaggedRules,
            EvaluatedAt = _evaluatedAt
        };
    }
}
