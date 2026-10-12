# Fraud Rules Configuration Guide

## Overview

All fraud rule thresholds and scoring parameters are now configurable via `appsettings.json` instead of hardcoded values. This allows you to adjust fraud detection rules without code changes or redeployment.

## Configuration Structure

The fraud rules are configured under the `FraudRules` section in `appsettings.json`:

```json
{
  "FraudRules": {
    "HighAmount": { ... },
    "ForeignCnp": { ... },
    "AtmWithdrawalLimit": { ... },
    "RoundAmount": { ... },
    "HighRiskMerchantCategory": { ... },
    "FlagThreshold": 40,
    "MaxScore": 100
  }
}
```

## Global Settings

### FlagThreshold
**Type:** Decimal (0-100)  
**Default:** 40  
**Description:** Transactions with a fraud score >= this threshold are flagged as fraudulent.

**Example:**
```json
"FlagThreshold": 35
```

### MaxScore
**Type:** Decimal (1-1000)  
**Default:** 100  
**Description:** Maximum possible fraud score. All rule contributions are capped at this value.

**Example:**
```json
"MaxScore": 150
```

---

## Individual Rule Configuration

### 1. High Amount Rule

Flags transactions whose absolute amount exceeds the configured threshold.

**Configuration:**
```json
"HighAmount": {
  "Threshold": 50000,
  "ScoreContribution": 40,
  "Enabled": true
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Threshold` | Decimal | 50000 | Transaction amount threshold (in base currency) |
| `ScoreContribution` | Decimal (0-100) | 40 | Points added to fraud score when triggered |
| `Enabled` | Boolean | true | Enable/disable this rule without code changes |

**Examples:**

For stricter detection (development):
```json
"HighAmount": {
  "Threshold": 30000,
  "ScoreContribution": 50,
  "Enabled": true
}
```

For more lenient detection (production):
```json
"HighAmount": {
  "Threshold": 100000,
  "ScoreContribution": 30,
  "Enabled": true
}
```

To disable the rule:
```json
"HighAmount": {
  "Enabled": false
}
```

---

### 2. Foreign CNP Rule

Flags card-not-present (CNP) transactions from countries other than the home country.

**Configuration:**
```json
"ForeignCnp": {
  "HomeCountry": "ZA",
  "ScoreContribution": 35,
  "Enabled": true
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HomeCountry` | String | "ZA" | Home country code (ISO 3166-1 alpha-2) |
| `ScoreContribution` | Decimal (0-100) | 35 | Points added to fraud score when triggered |
| `Enabled` | Boolean | true | Enable/disable this rule |

**Examples:**

For a different home country:
```json
"ForeignCnp": {
  "HomeCountry": "US",
  "ScoreContribution": 35,
  "Enabled": true
}
```

---

### 3. ATM Withdrawal Limit Rule

Flags ATM cash withdrawals exceeding the single-transaction limit.

**Configuration:**
```json
"AtmWithdrawalLimit": {
  "Limit": 5000,
  "ScoreContribution": 30,
  "Enabled": true
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Limit` | Decimal | 5000 | Single transaction ATM withdrawal limit |
| `ScoreContribution` | Decimal (0-100) | 30 | Points added to fraud score when triggered |
| `Enabled` | Boolean | true | Enable/disable this rule |

**Examples:**

For stricter ATM limits:
```json
"AtmWithdrawalLimit": {
  "Limit": 3000,
  "ScoreContribution": 40,
  "Enabled": true
}
```

---

### 4. Round Amount Rule

Flags large transactions with round-number amounts (common fraud indicator).

**Configuration:**
```json
"RoundAmount": {
  "MinimumAmount": 1000,
  "RoundnessMultiple": 1000,
  "ScoreContribution": 10,
  "Enabled": true
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `MinimumAmount` | Decimal | 1000 | Minimum transaction amount to check for roundness |
| `RoundnessMultiple` | Decimal | 1000 | Amount must be multiple of this (e.g., 1000 for thousands) |
| `ScoreContribution` | Decimal (0-100) | 10 | Points added to fraud score when triggered |
| `Enabled` | Boolean | true | Enable/disable this rule |

**Examples:**

Detect round hundreds:
```json
"RoundAmount": {
  "MinimumAmount": 500,
  "RoundnessMultiple": 100,
  "ScoreContribution": 15,
  "Enabled": true
}
```

Detect only very round large transactions:
```json
"RoundAmount": {
  "MinimumAmount": 10000,
  "RoundnessMultiple": 5000,
  "ScoreContribution": 20,
  "Enabled": true
}
```

---

### 5. High Risk Merchant Category Rule

Flags transactions in high-risk merchant categories (gambling, crypto, money transfer, etc.).

**Configuration:**
```json
"HighRiskMerchantCategory": {
  "HighRiskCategories": "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer",
  "ScoreContribution": 25,
  "Enabled": true
}
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `HighRiskCategories` | String (comma-separated) | See default | Merchant categories to flag |
| `ScoreContribution` | Decimal (0-100) | 25 | Points added to fraud score when triggered |
| `Enabled` | Boolean | true | Enable/disable this rule |

**Examples:**

Add more categories:
```json
"HighRiskMerchantCategory": {
  "HighRiskCategories": "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer,remittance,payment_processor,forex",
  "ScoreContribution": 30,
  "Enabled": true
}
```

Focus only on specific categories:
```json
"HighRiskMerchantCategory": {
  "HighRiskCategories": "crypto,cryptocurrency",
  "ScoreContribution": 50,
  "Enabled": true
}
```

---

## Environment-Specific Configuration

Different configurations are provided for different deployment environments:

### Development (appsettings.LOC.json)
- More lenient thresholds to avoid false positives during testing
- Lower flag threshold (40)
- Higher transaction limits

### Production (appsettings.RELEASE.json)
- Stricter thresholds for better fraud detection
- Lower flag threshold (35)
- More aggressive scoring
- Additional merchant categories monitored

### Testing (appsettings.Test.json)
- Standard configuration matching development
- Used for automated testing

## Configuration Examples

### Conservative Detection (Minimize False Positives)

```json
"FraudRules": {
  "HighAmount": {
    "Threshold": 100000,
    "ScoreContribution": 30,
    "Enabled": true
  },
  "ForeignCnp": {
    "HomeCountry": "ZA",
    "ScoreContribution": 25,
    "Enabled": true
  },
  "AtmWithdrawalLimit": {
    "Limit": 10000,
    "ScoreContribution": 20,
    "Enabled": true
  },
  "RoundAmount": {
    "MinimumAmount": 5000,
    "RoundnessMultiple": 5000,
    "ScoreContribution": 5,
    "Enabled": true
  },
  "HighRiskMerchantCategory": {
    "HighRiskCategories": "crypto,cryptocurrency",
    "ScoreContribution": 15,
    "Enabled": true
  },
  "FlagThreshold": 50,
  "MaxScore": 100
}
```

### Aggressive Detection (Catch More Fraud)

```json
"FraudRules": {
  "HighAmount": {
    "Threshold": 20000,
    "ScoreContribution": 50,
    "Enabled": true
  },
  "ForeignCnp": {
    "HomeCountry": "ZA",
    "ScoreContribution": 45,
    "Enabled": true
  },
  "AtmWithdrawalLimit": {
    "Limit": 2000,
    "ScoreContribution": 40,
    "Enabled": true
  },
  "RoundAmount": {
    "MinimumAmount": 500,
    "RoundnessMultiple": 500,
    "ScoreContribution": 25,
    "Enabled": true
  },
  "HighRiskMerchantCategory": {
    "HighRiskCategories": "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer,remittance,payment_processor",
    "ScoreContribution": 45,
    "Enabled": true
  },
  "FlagThreshold": 25,
  "MaxScore": 100
}
```

### Disable Specific Rules

```json
"FraudRules": {
  "HighAmount": {
    "Enabled": false
  },
  "ForeignCnp": {
    "HomeCountry": "ZA",
    "ScoreContribution": 35,
    "Enabled": true
  },
  "AtmWithdrawalLimit": {
    "Limit": 5000,
    "ScoreContribution": 30,
    "Enabled": true
  },
  "RoundAmount": {
    "MinimumAmount": 1000,
    "RoundnessMultiple": 1000,
    "ScoreContribution": 10,
    "Enabled": true
  },
  "HighRiskMerchantCategory": {
    "HighRiskCategories": "gambling,casino,crypto,cryptocurrency,money_transfer,wire_transfer",
    "ScoreContribution": 25,
    "Enabled": false
  },
  "FlagThreshold": 40,
  "MaxScore": 100
}
```

---

## How Configuration is Loaded

1. **Application Startup**
   - Application reads `appsettings.json` and environment-specific file (e.g., `appsettings.RELEASE.json`)
   - Configuration is loaded into `FraudRulesSettings` class
   - Validation is performed (all required fields present, values in valid ranges)

2. **Dependency Injection**
   - `FraudRulesSettings` is registered as a singleton via `IOptions<FraudRulesSettings>`
   - Each fraud rule receives configuration during construction

3. **Rule Evaluation**
   - Rules use configured values (thresholds, scores) when evaluating transactions
   - Rules respect the `Enabled` flag before evaluation

## Configuration Validation

All configuration values are validated using data annotations:

- **Range validation:** Thresholds must be within acceptable ranges
- **Required validation:** Critical fields cannot be empty
- **String length validation:** Field lengths are limited

If configuration validation fails during startup, the application will log detailed error messages and exit.

---

## Best Practices

1. **Start Conservative:** Begin with higher thresholds (fewer flagged transactions) and lower them based on results.

2. **Monitor Metrics:** Track:
   - % of transactions flagged
   - Distribution of triggered rules
   - False positive rate (transactions incorrectly flagged)
   - False negative rate (fraud that slipped through)

3. **Environment-Specific:** Use different configurations for different environments:
   - Development: More lenient (faster testing)
   - Production: More aggressive (better fraud detection)

4. **Test Before Production:** Always test configuration changes in development/staging first.

5. **Document Changes:** When updating thresholds, document the reasoning and expected impact.

6. **Gradual Rollout:** When making significant changes, consider gradual rollout:
   - Week 1: Monitor without applying stricter rules
   - Week 2: Apply new rules to 10% of transactions
   - Week 3: Roll out to all transactions

---

## Troubleshooting

### Rules Not Triggering as Expected

1. Check if rule is `Enabled: true`
2. Verify threshold values are appropriate for your data
3. Check `FlagThreshold` - if too high, few transactions will be flagged
4. Look at metrics to see which rules are actually triggering

### Configuration Not Applied

1. Verify `appsettings.json` is syntactically valid JSON
2. Check that all required fields are present
3. Verify environment variable `ASPNETCORE_ENVIRONMENT` matches intended configuration
4. Check application logs at startup for validation errors

### False Positives (Too Many Flagged Transactions)

1. Lower the `FlagThreshold` value
2. Reduce individual rule `ScoreContribution` values
3. Increase rule `Threshold` values
4. Disable problematic rules temporarily

### False Negatives (Not Enough Fraud Detected)

1. Raise the `FlagThreshold` value (lower it, actually - higher threshold = fewer flags)
2. Increase individual rule `ScoreContribution` values
3. Decrease rule `Threshold` values
4. Add new high-risk merchant categories

---

## Configuration File Location

| Environment | File |
|-------------|------|
| Default | `fraud_poc_project_ui/appsettings.json` |
| Local Development | `fraud_poc_project_ui/appsettings.LOC.json` |
| Release/Production | `fraud_poc_project_ui/appsettings.RELEASE.json` |
| Testing | `fraud_poc_project_ui/appsettings.Test.json` |

---

## Complete Example

See `appsettings.json`, `appsettings.LOC.json`, and `appsettings.RELEASE.json` for complete working examples.
