using fraud_poc_project_buss;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Service;
using fraud_poc_project_buss.Tests;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Xunit;
using Xunit.Abstractions;

namespace fraud_poc_project.Tests.LoadTests
{
    /// <summary>
    /// Load tests for fraud evaluation service to measure performance under sustained load.
    /// Tests throughput, latency percentiles, and memory usage.
    /// </summary>
    public class FraudEvaluationLoadTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly FraudEvaluationService _evaluationService;
        private readonly List<TransactionEvent> _testTransactions;

        public FraudEvaluationLoadTest(ITestOutputHelper output)
        {
            _output = output;

            // Set up fraud rules
            var fraudRulesSettings = new FraudRulesSettings
            {
                HighAmount = new HighAmountRuleSettings { Threshold = 50000, ScoreContribution = 40, Enabled = true },
                ForeignCnp = new ForeignCnpRuleSettings { HomeCountry = "ZA", ScoreContribution = 35, Enabled = true },
                AtmWithdrawalLimit = new AtmWithdrawalLimitRuleSettings { Limit = 5000, ScoreContribution = 30, Enabled = true },
                RoundAmount = new RoundAmountRuleSettings { MinimumAmount = 1000, ScoreContribution = 10, Enabled = true },
                HighRiskMerchantCategory = new HighRiskMerchantCategoryRuleSettings
                {
                    HighRiskCategories = "gambling,casino,crypto",
                    ScoreContribution = 25,
                    Enabled = true
                },
                FlagThreshold = 40,
                MaxScore = 100
            };

            var options = Options.Create(fraudRulesSettings);
            var rules = new List<IFraudRule>
            {
                new HighAmountRule(options),
                new ForeignCnpRule(options),
                new AtmWithdrawalLimitRule(options),
                new RoundAmountRule(options),
                new HighRiskMerchantCategoryRule(options)
            };

            _evaluationService = new FraudEvaluationService(rules, options);
            _testTransactions = GenerateTestTransactions(1000);
        }

        [Fact(DisplayName = "Evaluate 10,000 transactions - measure throughput and latency")]
        public void LoadTest_EvaluateTenThousandTransactions()
        {
            const int transactionCount = 10000;
            var stopwatch = Stopwatch.StartNew();
            var latencies = new List<long>();

            for (int i = 0; i < transactionCount; i++)
            {
                var transaction = _testTransactions[i % _testTransactions.Count];
                var txStopwatch = Stopwatch.StartNew();
                var result = _evaluationService.Evaluate(transaction);
                txStopwatch.Stop();
                latencies.Add(txStopwatch.ElapsedMilliseconds);
            }

            stopwatch.Stop();

            var throughput = (double)transactionCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            var p50 = GetPercentile(latencies, 0.50);
            var p95 = GetPercentile(latencies, 0.95);
            var p99 = GetPercentile(latencies, 0.99);
            var avgLatency = latencies.Average();

            _output.WriteLine($"Load Test Results (10,000 transactions):");
            _output.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Throughput: {throughput:F2} tx/sec");
            _output.WriteLine($"  Average Latency: {avgLatency:F2}ms");
            _output.WriteLine($"  P50 Latency: {p50}ms");
            _output.WriteLine($"  P95 Latency: {p95}ms");
            _output.WriteLine($"  P99 Latency: {p99}ms");

            // Assertions - adjust thresholds based on your performance requirements
            Assert.True(throughput > 1000, $"Throughput too low: {throughput:F2} tx/sec (expected > 1000)");
            Assert.True(p99 < 100, $"P99 latency too high: {p99}ms (expected < 100ms)");
            Assert.True(avgLatency < 10, $"Average latency too high: {avgLatency:F2}ms (expected < 10ms)");
        }

        [Fact(DisplayName = "Concurrent evaluation - 100 parallel transactions")]
        public async Task LoadTest_ConcurrentEvaluation()
        {
            const int concurrency = 100;
            const int transactionsPerThread = 100;
            var stopwatch = Stopwatch.StartNew();
            var allLatencies = new List<long>();

            var tasks = Enumerable.Range(0, concurrency)
                .Select(async _ =>
                {
                    var latencies = new List<long>();
                    for (int i = 0; i < transactionsPerThread; i++)
                    {
                        var transaction = _testTransactions[i % _testTransactions.Count];
                        var txStopwatch = Stopwatch.StartNew();
                        var result = _evaluationService.Evaluate(transaction);
                        txStopwatch.Stop();
                        latencies.Add(txStopwatch.ElapsedMilliseconds);
                    }
                    return latencies;
                })
                .ToList();

            var results = await Task.WhenAll(tasks);
            foreach (var latencies in results)
            {
                allLatencies.AddRange(latencies);
            }

            stopwatch.Stop();

            var totalTransactions = concurrency * transactionsPerThread;
            var throughput = (double)totalTransactions / (stopwatch.ElapsedMilliseconds / 1000.0);
            var p95 = GetPercentile(allLatencies, 0.95);
            var p99 = GetPercentile(allLatencies, 0.99);

            _output.WriteLine($"Concurrent Load Test Results ({concurrency} parallel threads, {transactionsPerThread} tx each):");
            _output.WriteLine($"  Total Transactions: {totalTransactions}");
            _output.WriteLine($"  Total Time: {stopwatch.ElapsedMilliseconds}ms");
            _output.WriteLine($"  Throughput: {throughput:F2} tx/sec");
            _output.WriteLine($"  P95 Latency: {p95}ms");
            _output.WriteLine($"  P99 Latency: {p99}ms");

            Assert.True(throughput > 5000, $"Concurrent throughput too low: {throughput:F2} tx/sec (expected > 5000)");
            Assert.True(p99 < 500, $"P99 latency too high under concurrency: {p99}ms (expected < 500ms)");
        }

        [Fact(DisplayName = "Memory stability - check for memory leaks over 50,000 evaluations")]
        public void LoadTest_MemoryStability()
        {
            const int evaluationCount = 50000;

            // Force garbage collection before test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var startMemory = GC.GetTotalMemory(false);

            var stopwatch = Stopwatch.StartNew();
            for (int i = 0; i < evaluationCount; i++)
            {
                var transaction = _testTransactions[i % _testTransactions.Count];
                var result = _evaluationService.Evaluate(transaction);
            }
            stopwatch.Stop();

            // Force garbage collection after test
            GC.Collect();
            GC.WaitForPendingFinalizers();
            var endMemory = GC.GetTotalMemory(false);

            var memoryGrowth = (endMemory - startMemory) / (1024.0 * 1024.0); // MB
            var memoryPerTransaction = memoryGrowth / evaluationCount * 1000000; // bytes per 1M transactions

            _output.WriteLine($"Memory Stability Test Results ({evaluationCount} evaluations):");
            _output.WriteLine($"  Start Memory: {startMemory / (1024.0 * 1024.0):F2} MB");
            _output.WriteLine($"  End Memory: {endMemory / (1024.0 * 1024.0):F2} MB");
            _output.WriteLine($"  Memory Growth: {memoryGrowth:F2} MB");
            _output.WriteLine($"  Memory per 1M tx: {memoryPerTransaction:F2} MB");
            _output.WriteLine($"  Throughput: {(double)evaluationCount / (stopwatch.ElapsedMilliseconds / 1000.0):F2} tx/sec");

            // Memory growth should be minimal - typical objects should be collected
            Assert.True(memoryPerTransaction < 10, $"Memory growth per transaction too high: {memoryPerTransaction:F2} MB per 1M tx");
        }

        private List<TransactionEvent> GenerateTestTransactions(int count)
        {
            var transactions = new List<TransactionEvent>();
            var random = new Random(42);

            for (int i = 0; i < count; i++)
            {
                transactions.Add(new TransactionEventBuilder()
                    .WithCustomerId($"CUST-{i % 100:D4}")
                    .WithAccountId($"ACC-{i % 50:D4}")
                    .WithAmount((decimal)(random.NextDouble() * 100000))
                    .WithMerchantName($"Merchant-{i % 200}")
                    .WithMerchantCategory(new[] { "gambling", "crypto", "retail", "food", "travel" }[i % 5])
                    .WithTransactionType(new[] { "POS", "ATM", "EFT", "CNP" }[i % 4])
                    .WithChannel(new[] { "InStore", "Online", "ATM", "Phone" }[i % 4])
                    .WithCountryCode(random.NextDouble() > 0.8 ? "US" : "ZA")
                    .WithTransactionTime(DateTime.UtcNow.AddMinutes(-random.Next(1440)))
                    .Build());
            }

            return transactions;
        }

        private static long GetPercentile(List<long> values, double percentile)
        {
            if (values.Count == 0) return 0;

            var sorted = values.OrderBy(x => x).ToList();
            var index = (int)Math.Ceiling(percentile * sorted.Count) - 1;
            return sorted[Math.Max(0, Math.Min(index, sorted.Count - 1))];
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
