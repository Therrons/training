using FluentAssertions;
using fraud_poc_project_buss.Models.Fraud;
using fraud_poc_project_buss.Service;
using fraud_poc_project_buss.Tests.Fixtures;
using fraud_poc_project_buss.Tests.Helpers;
using Moq;
using Xunit;

namespace fraud_poc_project_buss.Tests.Services
{
    /// <summary>
    /// Unit tests for FraudEvaluationService.
    /// Tests the core fraud evaluation logic: rule execution, score calculation, and flagging.
    /// </summary>
    public class FraudEvaluationServiceTests : BaseBusinessLogicTest
    {
        private FraudEvaluationService? _service;

        // ============================================================
        // Test 1: Null Input Handling
        // ============================================================

        /// <summary>
        /// Verifies that Evaluate throws ArgumentNullException when passed a null transaction event.
        /// This ensures defensive programming against invalid inputs.
        /// </summary>
        [Fact]
        public void Evaluate_WithNullEvent_ThrowsArgumentNullException()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder().Build();
            _service = new FraudEvaluationService(rules);

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => _service.Evaluate(null!));
        }

        // ============================================================
        // Test 2: No Rules Triggered
        // ============================================================

        /// <summary>
        /// Verifies that when no fraud rules trigger, the transaction is not flagged
        /// and the score remains zero. Tests the nominal case where legitimate transactions pass through.
        /// </summary>
        [Fact]
        public void Evaluate_WithNoRulesTriggered_ReturnsUnflaggedRecord()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddNonTriggeredRule("LOW_AMOUNT", "Amount below threshold")
                .AddNonTriggeredRule("DOMESTIC", "Transaction is domestic")
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse();
            result.FraudScore.Should().Be(0m);
            result.FlaggedReason.Should().BeNull();
            result.RuleResults.Should().HaveCount(2);
            result.RuleResults.All(r => !r.IsTriggered).Should().BeTrue();
        }

        // ============================================================
        // Test 3: Single Rule Triggered Below Threshold
        // ============================================================

        /// <summary>
        /// Verifies that a single triggered rule that scores below the 40-point threshold
        /// does not flag the transaction, but does record the score contribution.
        /// Tests partial fraud indication without flagging.
        /// </summary>
        [Fact]
        public void Evaluate_WithSingleRuleTriggeredBelowThreshold_ReturnsUnflaggedWithScore()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("LOW_AMOUNT", "Amount exceeds daily limit", scoreContribution: 15m)
                .AddNonTriggeredRule("HIGH_RISK_MERCHANT")
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent(amount: 1000m);

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse("score 15 is below the 40-point threshold");
            result.FraudScore.Should().Be(15m);
            result.FlaggedReason.Should().BeNull();
        }

        // ============================================================
        // Test 4: Multiple Rules Triggered - Score Reaches Threshold
        // ============================================================

        /// <summary>
        /// Verifies that when multiple rules trigger and their combined score reaches or exceeds 40,
        /// the transaction is flagged and all triggered rule codes are included in the reason.
        /// Tests compound fraud detection.
        /// </summary>
        [Fact]
        public void Evaluate_WithMultipleRulesTriggeredAboveThreshold_ReturnsFlaggedRecord()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("HIGH_AMOUNT", "High amount detected", scoreContribution: 25m)
                .AddTriggeredRule("UNUSUAL_TIME", "Transaction at unusual time", scoreContribution: 20m)
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateHighValueTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(45m);
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
            result.FlaggedReason.Should().Contain("UNUSUAL_TIME");
            result.RuleResults.Should().HaveCount(2);
            result.RuleResults.Count(r => r.IsTriggered).Should().Be(2);
        }

        // ============================================================
        // Test 5: Score Capping at Maximum
        // ============================================================

        /// <summary>
        /// Verifies that when multiple triggered rules would combine to exceed 100 points,
        /// the final score is capped at 100. Tests the maximum score boundary.
        /// </summary>
        [Fact]
        public void Evaluate_WithScoreThatExceedsMaximum_CapsScoreAt100()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("RULE_1", "Rule 1", scoreContribution: 30m)
                .AddTriggeredRule("RULE_2", "Rule 2", scoreContribution: 35m)
                .AddTriggeredRule("RULE_3", "Rule 3", scoreContribution: 40m)
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.FraudScore.Should().Be(100m, "score should be capped at maximum even if 30+35+40=105");
            result.IsFlagged.Should().BeTrue();
        }

        // ============================================================
        // Test 6: Async Evaluation
        // ============================================================

        /// <summary>
        /// Verifies that the async evaluation method properly delegates to the synchronous
        /// evaluation and completes successfully. Tests async/await integration.
        /// </summary>
        [Fact]
        public async Task EvaluateAsync_WithValidEvent_ReturnsTaskCompletingSuccessfully()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("TEST_RULE", "Test", scoreContribution: 45m)
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = await _service.EvaluateAsync(transaction);

            // Assert
            result.Should().NotBeNull();
            result.IsFlagged.Should().BeTrue();
            result.FraudScore.Should().Be(45m);
        }

        // ============================================================
        // Test 7: Flagged Reason Message Format
        // ============================================================

        /// <summary>
        /// Verifies that when multiple rules trigger and flag a transaction,
        /// the flagged reason is built as a comma-separated list of rule codes.
        /// Tests the specific formatting of the reason string.
        /// </summary>
        [Fact]
        public void Evaluate_WithMultipleTriggeredRules_BuildsCorrectFlaggedReasonString()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("HIGH_AMOUNT", "High amount", scoreContribution: 25m)
                .AddTriggeredRule("FOREIGN_TXN", "Foreign transaction", scoreContribution: 20m)
                .AddTriggeredRule("ODD_TIME", "Odd time", scoreContribution: 15m)
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.FlaggedReason.Should().NotBeNullOrEmpty();
            result.FlaggedReason.Should().Contain("HIGH_AMOUNT");
            result.FlaggedReason.Should().Contain("FOREIGN_TXN");
            result.FlaggedReason.Should().Contain("ODD_TIME");
        }

        // ============================================================
        // Test 8: Empty Rules Collection
        // ============================================================

        /// <summary>
        /// Verifies that when the service is initialized with no fraud rules,
        /// any transaction passes through unflagged with a score of zero.
        /// Tests graceful handling of empty rule sets.
        /// </summary>
        [Fact]
        public void Evaluate_WithNoRules_ReturnsUnflaggedRecordWithZeroScore()
        {
            // Arrange
            _service = new FraudEvaluationService(Array.Empty<IFraudRule>());
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeFalse();
            result.FraudScore.Should().Be(0m);
            result.RuleResults.Should().BeEmpty();
        }

        // ============================================================
        // Test 9: Boundary Score at Exactly Threshold
        // ============================================================

        /// <summary>
        /// Verifies that when the fraud score equals exactly 40 (the threshold),
        /// the transaction IS flagged. Tests the boundary condition of the threshold.
        /// </summary>
        [Fact]
        public void Evaluate_WithScoreExactlyAtThreshold_ReturnsFlaggedRecord()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder()
                .AddTriggeredRule("RULE_1", "Rule 1", scoreContribution: 20m)
                .AddTriggeredRule("RULE_2", "Rule 2", scoreContribution: 20m)
                .Build();

            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.IsFlagged.Should().BeTrue("score 40 >= threshold 40");
            result.FraudScore.Should().Be(40m);
        }

        // ============================================================
        // Test 10: Transaction Event Included in Result
        // ============================================================

        /// <summary>
        /// Verifies that the original transaction event is preserved and returned
        /// in the fraud evaluation result for traceability and reference.
        /// Tests that evaluation does not lose the original transaction data.
        /// </summary>
        [Fact]
        public void Evaluate_WithValidTransaction_IncludesTransactionEventInResult()
        {
            // Arrange
            var rules = new MockFraudRuleBuilder().Build();
            _service = new FraudEvaluationService(rules);
            var transaction = CreateValidTransactionEvent();

            // Act
            var result = _service.Evaluate(transaction);

            // Assert
            result.Event.Should().NotBeNull();
            result.Event.TransactionId.Should().Be(transaction.TransactionId);
            result.Event.CustomerId.Should().Be(transaction.CustomerId);
            result.Event.Amount.Should().Be(transaction.Amount);
        }
    }
}
