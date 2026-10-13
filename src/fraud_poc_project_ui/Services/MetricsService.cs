using fraud_poc_project_buss.Helper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace fraud_poc_project.Services
{
    public interface IMetricsService
    {
        void RecordTransactionProcessed(bool isFlagged, decimal fraudScore, long elapsedMilliseconds);
        void RecordRuleExecution(string ruleCode, bool triggered, long elapsedMilliseconds);
        void RecordAuthenticationAttempt(bool successful);
        void RecordDatabaseOperation(string operationType, long elapsedMilliseconds);
        void RecordKafkaEvent(string eventType, long elapsedMilliseconds);

        MetricsSnapshot GetSnapshot();
        void Reset();
    }

    public class MetricsService : IMetricsService
    {
        private readonly ILogger<MetricsService> _logger;
        private readonly object _lockObject = new object();

        // Transaction metrics
        private long _totalTransactions;
        private long _flaggedTransactions;
        private long _totalFraudScore;
        private List<long> _transactionProcessingTimes = new();

        // Rule metrics
        private Dictionary<string, RuleMetrics> _ruleMetrics = new();

        // Authentication metrics
        private long _totalLoginAttempts;
        private long _successfulLogins;

        // Database metrics
        private Dictionary<string, OperationMetrics> _databaseOperations = new();

        // Kafka metrics
        private Dictionary<string, OperationMetrics> _kafkaOperations = new();

        public MetricsService(ILogger<MetricsService> logger)
        {
            _logger = logger;
        }

        public void RecordTransactionProcessed(bool isFlagged, decimal fraudScore, long elapsedMilliseconds)
        {
            lock (_lockObject)
            {
                _totalTransactions++;
                if (isFlagged)
                    _flaggedTransactions++;

                _totalFraudScore += (long)fraudScore;
                _transactionProcessingTimes.Add(elapsedMilliseconds);

                // Retain only recent measurements to bound memory
                if (_transactionProcessingTimes.Count > 1000)
                    _transactionProcessingTimes = _transactionProcessingTimes.TakeLast(1000).ToList();

                _logger.LogInformationOnly(
                    "Transaction processed - Flagged: {Flagged}, Score: {Score}, Time: {ElapsedMs}ms",
                    isFlagged, fraudScore, elapsedMilliseconds);
            }
        }

        public void RecordRuleExecution(string ruleCode, bool triggered, long elapsedMilliseconds)
        {
            lock (_lockObject)
            {
                if (!_ruleMetrics.ContainsKey(ruleCode))
                {
                    _ruleMetrics[ruleCode] = new RuleMetrics { RuleCode = ruleCode };
                }

                var metrics = _ruleMetrics[ruleCode];
                metrics.ExecutionCount++;
                if (triggered)
                    metrics.TriggeredCount++;

                metrics.TotalExecutionTime += elapsedMilliseconds;
                metrics.ExecutionTimes.Add(elapsedMilliseconds);

                if (metrics.ExecutionTimes.Count > 500)
                    metrics.ExecutionTimes = metrics.ExecutionTimes.TakeLast(500).ToList();
            }
        }

        public void RecordAuthenticationAttempt(bool successful)
        {
            lock (_lockObject)
            {
                _totalLoginAttempts++;
                if (successful)
                    _successfulLogins++;

                _logger.LogInformationOnly(
                    "Authentication attempt - Successful: {Successful}, Total: {Total}, Success Rate: {SuccessRate:P}",
                    successful, _totalLoginAttempts, GetAuthenticationSuccessRate());
            }
        }

        public void RecordDatabaseOperation(string operationType, long elapsedMilliseconds)
        {
            lock (_lockObject)
            {
                if (!_databaseOperations.ContainsKey(operationType))
                {
                    _databaseOperations[operationType] = new OperationMetrics { OperationType = operationType };
                }

                var metrics = _databaseOperations[operationType];
                metrics.Count++;
                metrics.TotalTime += elapsedMilliseconds;
                metrics.Times.Add(elapsedMilliseconds);

                if (metrics.Times.Count > 500)
                    metrics.Times = metrics.Times.TakeLast(500).ToList();
            }
        }

        public void RecordKafkaEvent(string eventType, long elapsedMilliseconds)
        {
            lock (_lockObject)
            {
                if (!_kafkaOperations.ContainsKey(eventType))
                {
                    _kafkaOperations[eventType] = new OperationMetrics { OperationType = eventType };
                }

                var metrics = _kafkaOperations[eventType];
                metrics.Count++;
                metrics.TotalTime += elapsedMilliseconds;
                metrics.Times.Add(elapsedMilliseconds);

                if (metrics.Times.Count > 500)
                    metrics.Times = metrics.Times.TakeLast(500).ToList();
            }
        }

        public MetricsSnapshot GetSnapshot()
        {
            lock (_lockObject)
            {
                return new MetricsSnapshot
                {
                    Timestamp = DateTime.UtcNow,
                    TotalTransactions = _totalTransactions,
                    FlaggedTransactions = _flaggedTransactions,
                    FraudRate = _totalTransactions > 0 ? (decimal)_flaggedTransactions / _totalTransactions : 0,
                    AvgFraudScore = _totalTransactions > 0 ? _totalFraudScore / (decimal)_totalTransactions : 0,
                    AvgProcessingTime = _transactionProcessingTimes.Any() ?
                        (long)_transactionProcessingTimes.Average() : 0,
                    MaxProcessingTime = _transactionProcessingTimes.Any() ?
                        _transactionProcessingTimes.Max() : 0,
                    MinProcessingTime = _transactionProcessingTimes.Any() ?
                        _transactionProcessingTimes.Min() : 0,
                    P95ProcessingTime = CalculatePercentile(_transactionProcessingTimes, 95),
                    P99ProcessingTime = CalculatePercentile(_transactionProcessingTimes, 99),
                    TotalAuthenticationAttempts = _totalLoginAttempts,
                    SuccessfulAuthentications = _successfulLogins,
                    AuthenticationSuccessRate = GetAuthenticationSuccessRate(),
                    RuleMetrics = _ruleMetrics.Values.ToList(),
                    DatabaseOperationMetrics = _databaseOperations.Values.ToList(),
                    KafkaOperationMetrics = _kafkaOperations.Values.ToList()
                };
            }
        }

        public void Reset()
        {
            lock (_lockObject)
            {
                _totalTransactions = 0;
                _flaggedTransactions = 0;
                _totalFraudScore = 0;
                _transactionProcessingTimes.Clear();
                _ruleMetrics.Clear();
                _totalLoginAttempts = 0;
                _successfulLogins = 0;
                _databaseOperations.Clear();
                _kafkaOperations.Clear();

                _logger.LogInformationOnly("Metrics reset");
            }
        }

        private decimal GetAuthenticationSuccessRate()
        {
            return _totalLoginAttempts > 0 ?
                (decimal)_successfulLogins / _totalLoginAttempts : 0;
        }

        private long CalculatePercentile(List<long> values, int percentile)
        {
            if (!values.Any())
                return 0;

            var sorted = values.OrderBy(v => v).ToList();
            int index = (int)Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

    public class MetricsSnapshot
    {
        public DateTime Timestamp { get; set; }
        public long TotalTransactions { get; set; }
        public long FlaggedTransactions { get; set; }
        public decimal FraudRate { get; set; }
        public decimal AvgFraudScore { get; set; }
        public long AvgProcessingTime { get; set; }
        public long MaxProcessingTime { get; set; }
        public long MinProcessingTime { get; set; }
        public long P95ProcessingTime { get; set; }
        public long P99ProcessingTime { get; set; }
        public long TotalAuthenticationAttempts { get; set; }
        public long SuccessfulAuthentications { get; set; }
        public decimal AuthenticationSuccessRate { get; set; }
        public List<RuleMetrics> RuleMetrics { get; set; } = new();
        public List<OperationMetrics> DatabaseOperationMetrics { get; set; } = new();
        public List<OperationMetrics> KafkaOperationMetrics { get; set; } = new();

        public override string ToString()
        {
            return $@"
════════════════════════════════════════════════════════════════
                    FRAUD DETECTION METRICS
════════════════════════════════════════════════════════════════
Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss}

TRANSACTION METRICS:
  Total Transactions:       {TotalTransactions:N0}
  Flagged Transactions:     {FlaggedTransactions:N0}
  Fraud Rate:               {FraudRate:P2}
  Average Fraud Score:      {AvgFraudScore:F2}

PROCESSING TIME (ms):
  Average:                  {AvgProcessingTime}ms
  Min:                      {MinProcessingTime}ms
  Max:                      {MaxProcessingTime}ms
  P95:                      {P95ProcessingTime}ms
  P99:                      {P99ProcessingTime}ms

AUTHENTICATION METRICS:
  Total Attempts:           {TotalAuthenticationAttempts:N0}
  Successful:               {SuccessfulAuthentications:N0}
  Success Rate:             {AuthenticationSuccessRate:P2}

RULE EXECUTION METRICS:
{FormatRuleMetrics()}

DATABASE OPERATIONS:
{FormatDatabaseMetrics()}

KAFKA OPERATIONS:
{FormatKafkaMetrics()}
════════════════════════════════════════════════════════════════
";
        }

        private string FormatRuleMetrics()
        {
            if (!RuleMetrics.Any())
                return "  (No data)";

            return string.Join("\n", RuleMetrics.Select(r =>
                $"  {r.RuleCode}:\n" +
                $"    Executions: {r.ExecutionCount}\n" +
                $"    Triggered: {r.TriggeredCount} ({(r.ExecutionCount > 0 ? (decimal)r.TriggeredCount / r.ExecutionCount : 0):P})\n" +
                $"    Avg Time: {(r.ExecutionCount > 0 ? r.TotalExecutionTime / r.ExecutionCount : 0)}ms"));
        }

        private string FormatDatabaseMetrics()
        {
            if (!DatabaseOperationMetrics.Any())
                return "  (No data)";

            return string.Join("\n", DatabaseOperationMetrics.Select(m =>
                $"  {m.OperationType}:\n" +
                $"    Count: {m.Count}\n" +
                $"    Total Time: {m.TotalTime}ms\n" +
                $"    Avg Time: {(m.Count > 0 ? m.TotalTime / m.Count : 0)}ms"));
        }

        private string FormatKafkaMetrics()
        {
            if (!KafkaOperationMetrics.Any())
                return "  (No data)";

            return string.Join("\n", KafkaOperationMetrics.Select(m =>
                $"  {m.OperationType}:\n" +
                $"    Count: {m.Count}\n" +
                $"    Total Time: {m.TotalTime}ms\n" +
                $"    Avg Time: {(m.Count > 0 ? m.TotalTime / m.Count : 0)}ms"));
        }
    }

    public class RuleMetrics
    {
        public string RuleCode { get; set; }
        public long ExecutionCount { get; set; }
        public long TriggeredCount { get; set; }
        public long TotalExecutionTime { get; set; }
        public List<long> ExecutionTimes { get; set; } = new();

        public decimal TriggeredRate => ExecutionCount > 0 ?
            (decimal)TriggeredCount / ExecutionCount : 0;
        public long AvgExecutionTime => ExecutionCount > 0 ?
            TotalExecutionTime / ExecutionCount : 0;
    }

    public class OperationMetrics
    {
        public string OperationType { get; set; }
        public long Count { get; set; }
        public long TotalTime { get; set; }
        public List<long> Times { get; set; } = new();

        public long AvgTime => Count > 0 ? TotalTime / Count : 0;
    }
}
