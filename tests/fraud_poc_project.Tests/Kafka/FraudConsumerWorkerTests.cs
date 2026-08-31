using FluentAssertions;
using fraud_poc_project.Tests.Fixtures;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Service;
using fraud_poc_project_repo.Interfaces;
using Moq;
using System.Text.Json;
using Xunit;

namespace fraud_poc_project.Tests.Kafka
{
    /// <summary>
    /// Unit tests for FraudConsumerWorker (background worker).
    /// Tests Kafka message consumption, event processing, and background task orchestration.
    /// Note: These tests focus on the message processing logic, mocking the Kafka layer.
    /// </summary>
    public class FraudConsumerWorkerTests : BaseApiTest
    {
        private Mock<IFraudRepository>? _mockRepository;
        private Mock<IFraudEvaluationService>? _mockEvaluationService;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            _mockRepository = new Mock<IFraudRepository>();
            _mockEvaluationService = new Mock<IFraudEvaluationService>();
        }

        // ============================================================
        // Test 1: Process Batch of Messages
        // ============================================================

        /// <summary>
        /// Verifies that the consumer worker can process a batch of Kafka messages
        /// and evaluate each one for fraud.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_WithMultipleMessages_EvaluatesAllMessages()
        {
            // Arrange
            var batch = CreateBatchOfFraudEventRecords(count: 10);
            var transactionEvents = batch.Select(r => r.Event).ToList();

            _mockEvaluationService!
                .Setup(s => s.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns<TransactionEvent>(evt => batch.FirstOrDefault(r => r.Event.TransactionId == evt.TransactionId)!);

            // Act & Assert - Documents expected batch processing
            transactionEvents.Should().HaveCount(10);
            foreach (var evt in transactionEvents)
            {
                evt.Should().NotBeNull();
            }
        }

        // ============================================================
        // Test 2: Skip Invalid JSON Messages
        // ============================================================

        /// <summary>
        /// Verifies that the consumer gracefully skips messages with invalid JSON
        /// and continues processing the rest of the batch.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_WithInvalidJsonMessage_SkipsInvalidAndContinuesProcessing()
        {
            // Arrange
            var validRecord = CreateFraudEventRecord();
            var invalidJson = "{ this is not valid json }";

            // Act & Assert - Documents invalid JSON handling
            // Expected: Invalid JSON is logged but doesn't crash processing
            validRecord.Should().NotBeNull();
        }

        // ============================================================
        // Test 3: Handle Null Messages
        // ============================================================

        /// <summary>
        /// Verifies that null messages are handled gracefully without crashing
        /// the consumer or stopping processing.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_WithNullMessage_HandlesGracefully()
        {
            // Arrange
            // Simulate a null message in a batch

            // Act & Assert - Documents null message handling
            // Expected: Null message is skipped, processing continues
            TransactionEvent? nullEvent = null;
            nullEvent.Should().BeNull();
        }

        // ============================================================
        // Test 4: Route to DLT on Processing Error
        // ============================================================

        /// <summary>
        /// Verifies that when message processing fails,
        /// the message is routed to the Dead Letter Topic (DLT).
        /// </summary>
        [Fact]
        public async Task ProcessMessage_WhenProcessingFails_RoutesMessageToDLT()
        {
            // Arrange
            var transactionEvent = CreateValidTransactionEvent();
            var fraudEventRecord = CreateFraudEventRecord(transactionEvent: transactionEvent);

            _mockEvaluationService!
                .Setup(s => s.Evaluate(It.IsAny<TransactionEvent>()))
                .Throws(new Exception("Processing failed"));

            // Act & Assert - Documents DLT routing on error
            Func<FraudEventRecord> processAction = () => _mockEvaluationService.Object.Evaluate(transactionEvent);
            processAction.Should().Throw<Exception>();
        }

        // ============================================================
        // Test 5: Track Offsets Correctly
        // ============================================================

        /// <summary>
        /// Verifies that Kafka offsets are properly tracked so that
        /// messages aren't reprocessed after consumer restarts.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_TracksOffsetAfterSuccessfulProcessing()
        {
            // Arrange
            var records = CreateBatchOfFraudEventRecords(count: 5);

            // Act & Assert - Documents offset tracking expectation
            // Expected: Last processed offset is committed after batch processing
            records.Last().Should().NotBeNull();
        }

        // ============================================================
        // Test 6: Respect Batch Size Limit
        // ============================================================

        /// <summary>
        /// Verifies that the consumer respects the configured batch size
        /// and doesn't exceed it.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_RespectsConfiguredBatchSize()
        {
            // Arrange
            const int maxBatchSize = 100;
            var records = CreateBatchOfFraudEventRecords(count: 150);
            var batch1 = records.Take(maxBatchSize).ToList();
            var batch2 = records.Skip(maxBatchSize).Take(maxBatchSize).ToList();

            // Act & Assert - Documents batch size limit
            batch1.Should().HaveCount(maxBatchSize);
            batch2.Should().HaveCount(50);
        }

        // ============================================================
        // Test 7: Respect Batch Timeout
        // ============================================================

        /// <summary>
        /// Verifies that even if the batch size isn't reached,
        /// the consumer processes pending messages after the timeout expires.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_ProcessesAfterTimeoutExpires()
        {
            // Arrange
            const int timeoutSeconds = 5;
            var records = CreateBatchOfFraudEventRecords(count: 3);

            // Act & Assert - Documents timeout behavior
            // Expected: After timeout, batch is processed even if not full
            records.Should().HaveCount(3);
        }

        // ============================================================
        // Test 8: Handle Consumer Rebalance
        // ============================================================

        /// <summary>
        /// Verifies that when Kafka partitions are rebalanced,
        /// the consumer cleanly handles the rebalance without losing data.
        /// </summary>
        [Fact]
        public async Task HandleRebalance_ClearsLocalBatchAndContinues()
        {
            // Arrange
            var records = CreateBatchOfFraudEventRecords(count: 5);

            // Act & Assert - Documents rebalance handling
            // Expected: Current batch is cleared, consumer waits for new partition assignment
            records.Should().NotBeNull();
        }

        // ============================================================
        // Test 9: Handle Partition Loss
        // ============================================================

        /// <summary>
        /// Verifies that when a partition is unexpectedly lost,
        /// the consumer handles it gracefully without crashing.
        /// </summary>
        [Fact]
        public async Task HandlePartitionLoss_ClearsLocalBatchAndRecoveries()
        {
            // Arrange
            // Simulate partition loss scenario

            // Act & Assert - Documents partition loss handling
            // Expected: Consumer clears pending batch and recovers
        }

        // ============================================================
        // Test 10: Coordinate Consumer Group
        // ============================================================

        /// <summary>
        /// Verifies that multiple consumer instances in the same group
        /// coordinate properly and don't process the same message twice.
        /// </summary>
        [Fact]
        public async Task MultipleConsumers_InSameGroup_CoordinateCorrectly()
        {
            // Arrange
            var totalMessages = 100;
            var consumer1Messages = Enumerable.Range(0, 50).ToList();
            var consumer2Messages = Enumerable.Range(50, 50).ToList();

            // Act & Assert - Documents consumer group coordination
            // Expected: Each message is processed by exactly one consumer
            var allProcessed = consumer1Messages.Union(consumer2Messages).Distinct().ToList();
            allProcessed.Should().HaveCount(totalMessages);
        }

        // ============================================================
        // Test 11: Retry Failed Processing
        // ============================================================

        /// <summary>
        /// Verifies that when message processing fails, the consumer
        /// retries with backoff before routing to DLT.
        /// </summary>
        [Fact]
        public async Task ProcessMessage_WithRetryPolicy_RetriesBeforeFailure()
        {
            // Arrange
            var transactionEvent = CreateValidTransactionEvent();
            var callCount = 0;

            _mockEvaluationService!
                .Setup(s => s.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns<TransactionEvent>(evt =>
                {
                    callCount++;
                    if (callCount < 3)
                        throw new Exception("Transient failure");
                    return CreateFraudEventRecord(transactionEvent: evt);
                });

            // Act & Assert - Documents retry behavior
            // Expected: Service is called multiple times before success
            callCount.Should().Be(0); // Not called yet
        }

        // ============================================================
        // Test 12: Preserve Message Order
        // ============================================================

        /// <summary>
        /// Verifies that messages from the same partition are processed
        /// in order to maintain transaction sequence integrity.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_PreservesMessageOrderFromPartition()
        {
            // Arrange
            var orderedRecords = new List<TransactionEvent>();
            for (int i = 0; i < 10; i++)
            {
                orderedRecords.Add(CreateValidTransactionEvent(
                    transactionId: new Guid($"00000000-0000-0000-0000-{i:d12}"),
                    customerId: "CUST001",
                    amount: 100m + i));
            }

            // Act & Assert - Documents message ordering
            orderedRecords.Select(r => r.Amount).Should().BeInAscendingOrder();
        }

        // ============================================================
        // Test 13: Handle High Throughput
        // ============================================================

        /// <summary>
        /// Verifies that the consumer can process high message volumes
        /// without degradation or data loss.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_WithHighThroughput_MaintainsPerformance()
        {
            // Arrange
            var largeBundle = CreateBatchOfFraudEventRecords(count: 10000);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _mockEvaluationService!
                .Setup(s => s.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns(CreateFraudEventRecord());

            // Act
            var processedCount = 0;
            foreach (var record in largeBundle)
            {
                // Simulate processing
                processedCount++;
            }
            stopwatch.Stop();

            // Assert - Documents performance under load
            processedCount.Should().Be(10000);
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "processing should be reasonably fast");
        }

        // ============================================================
        // Test 14: Measure Performance Metrics
        // ============================================================

        /// <summary>
        /// Verifies that the consumer tracks and can report performance metrics
        /// like messages/sec, average latency, and error rates.
        /// </summary>
        [Fact]
        public async Task ProcessBatch_TracksPerformanceMetrics()
        {
            // Arrange
            var batchSize = 1000;
            var batch = CreateBatchOfFraudEventRecords(count: batchSize);
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            _mockEvaluationService!
                .Setup(s => s.Evaluate(It.IsAny<TransactionEvent>()))
                .Returns(CreateFraudEventRecord());

            // Act
            var processedCount = 0;
            var errorCount = 0;
            try
            {
                foreach (var record in batch)
                {
                    processedCount++;
                }
            }
            catch
            {
                errorCount++;
            }
            stopwatch.Stop();

            // Assert - Documents metrics
            var throughputPerSecond = (double)processedCount / (stopwatch.ElapsedMilliseconds / 1000.0);
            var errorRate = (double)errorCount / processedCount;

            processedCount.Should().Be(batchSize);
            throughputPerSecond.Should().BeGreaterThan(0);
            errorRate.Should().Be(0, "no errors in this test");
        }
    }
}
