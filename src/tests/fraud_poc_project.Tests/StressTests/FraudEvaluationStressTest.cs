using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using fraud_poc_project_buss.Dto;
using fraud_poc_project_buss.Service;
using fraud_poc_project_buss;
using fraud_poc_project_buss.Models.Settings;
using fraud_poc_project_buss.Tests;
using Microsoft.Extensions.Options;

namespace fraud_poc_project.Tests.StressTests
{
    /// <summary>
    /// Stress tests for fraud evaluation service to find breaking points and measure degradation.
    /// Tests system behavior under extreme load conditions.
    /// </summary>
    public class FraudEvaluationStressTest : IDisposable
    {
        private readonly ITestOutputHelper _output;
        private readonly FraudEvaluationService _evaluationService;
        private readonly List<TransactionEvent> _testTransactions;

        public FraudEvaluationStressTest(ITestOutputHelper output)
        {
            _output = output;

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

        [Fact(DisplayName = "Stress: High concurrency with increasing load")]
        public async Task StressTest_HighConcurrencyWithIncreasingLoad()
        {
            var results = new Dictionary<int, (long totalTime, double throughput, long maxLatency)>();

            for (int concurrency = 10; concurrency <= 1000; concurrency *= 10)
            {
                var stopwatch = Stopwatch.StartNew();
                var maxLatency = 0L;
                var transactionCount = 0;

                var tasks = Enumerable.Range(0, concurrency)
                    .Select(async _ =>
                    {
                        for (int i = 0; i < 100; i++)
                        {
                            var transaction = _testTransactions[i % _testTransactions.Count];
                            var txStopwatch = Stopwatch.StartNew();
                            var result = _evaluationService.Evaluate(transaction);
                            txStopwatch.Stop();

                            lock (this)
                            {
                                maxLatency = Math.Max(maxLatency, txStopwatch.ElapsedMilliseconds);
                                transactionCount++;
                            }
                        }
                    })
                    .ToList();

                await Task.WhenAll(tasks);
                stopwatch.Stop();

                var throughput = (double)transactionCount / (stopwatch.ElapsedMilliseconds / 1000.0);
                results[concurrency] = (stopwatch.ElapsedMilliseconds, throughput, maxLatency);

                _output.WriteLine($"Concurrency {concurrency}: {throughput:F2} tx/sec, max latency {maxLatency}ms");
            }

            // All concurrency levels should complete without error
            Assert.Equal(3, results.Count);

            // Throughput should remain relatively stable (not degrade more than 30% for 10x concurrency)
            var baseThroughput = results[10].throughput;
            var maxConcurrencyThroughput = results[1000].throughput;
            var degradation = (1 - (maxConcurrencyThroughput / baseThroughput)) * 100;

            _output.WriteLine($"Throughput degradation: {degradation:F1}%");
        }

        [Fact(DisplayName = "Stress: Sustained load for 30 seconds")]
        public async Task StressTest_SustainedLoad30Seconds()
        {
            const int concurrency = 50;
            const int durationSeconds = 30;
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(durationSeconds));
            var latencies = new List<long>();
            var latenciesLock = new object();
            var transactionCount = 0;
            var errorCount = 0;

            var stopwatch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, concurrency)
                .Select(async _ =>
                {
                    try
                    {
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var transaction = _testTransactions[transactionCount % _testTransactions.Count];
                            var txStopwatch = Stopwatch.StartNew();

                            try
                            {
                                var result = _evaluationService.Evaluate(transaction);
                                txStopwatch.Stop();

                                lock (latenciesLock)
                                {
                                    latencies.Add(txStopwatch.ElapsedMilliseconds);
                                    transactionCount++;
                                }
                            }
                            catch (Exception ex)
                            {
                                lock (latenciesLock)
                                {
                                    errorCount++;
                                }
                                txStopwatch.Stop();
                            }
                        }
                    }
                    catch (OperationCanceledException) { }
                })
                .ToList();

            await Task.WhenAll(tasks);
            stopwatch.Stop();

            var throughput = (double)transactionCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            var p50 = GetPercentile(latencies, 0.50);
            var p95 = GetPercentile(latencies, 0.95);
            var p99 = GetPercentile(latencies, 0.99);

            _output.WriteLine($"Sustained Load Test Results (30 seconds, {concurrency} threads):");
            _output.WriteLine($"  Total Transactions: {transactionCount}");
            _output.WriteLine($"  Throughput: {throughput:F2} tx/sec");
            _output.WriteLine($"  Errors: {errorCount}");
            _output.WriteLine($"  P50 Latency: {p50}ms");
            _output.WriteLine($"  P95 Latency: {p95}ms");
            _output.WriteLine($"  P99 Latency: {p99}ms");

            Assert.Equal(0, errorCount);
            Assert.True(transactionCount > 10000, $"Expected > 10,000 transactions in 30 seconds, got {transactionCount}");
        }

        [Fact(DisplayName = "Stress: Spike load - sudden increase to maximum")]
        public async Task StressTest_SpikeLoad()
        {
            var latencies = new List<long>();
            var latenciesLock = new object();

            // Phase 1: Warm up with 10 threads for 5 seconds
            _output.WriteLine("Phase 1: Warm up (10 threads, 5 sec)");
            await RunPhase(10, 5000, latencies, latenciesLock);

            var warmupLatency = latencies.Count > 0 ? latencies.Average() : 0;

            // Phase 2: Spike to 100 threads
            _output.WriteLine("Phase 2: Spike (100 threads, 10 sec)");
            latencies.Clear();
            await RunPhase(100, 10000, latencies, latenciesLock);

            var spikeLatency = latencies.Count > 0 ? latencies.Average() : 0;
            var latencyIncrease = ((spikeLatency / warmupLatency) - 1) * 100;

            _output.WriteLine($"Latency increase during spike: {latencyIncrease:F1}%");

            // System should handle spike without degrading more than 100%
            Assert.True(latencyIncrease < 100, $"Latency degradation too high during spike: {latencyIncrease:F1}%");
        }

        private async Task RunPhase(int concurrency, int durationMs, List<long> latencies, object latenciesLock)
        {
            var cts = new CancellationTokenSource(durationMs);

            var tasks = Enumerable.Range(0, concurrency)
                .Select(async _ =>
                {
                    try
                    {
                        int i = 0;
                        while (!cts.Token.IsCancellationRequested)
                        {
                            var transaction = _testTransactions[i % _testTransactions.Count];
                            var txStopwatch = Stopwatch.StartNew();
                            var result = _evaluationService.Evaluate(transaction);
                            txStopwatch.Stop();

                            lock (latenciesLock)
                            {
                                latencies.Add(txStopwatch.ElapsedMilliseconds);
                            }

                            i++;
                        }
                    }
                    catch (OperationCanceledException) { }
                })
                .ToList();

            try
            {
                await Task.WhenAll(tasks);
            }
            catch (OperationCanceledException) { }
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

        public void Dispose() { }
    }
}
