using FluentAssertions;
using fraud_poc_project.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace fraud_poc_project.Tests.Services
{
    /// <summary>
    /// Tests for LoginAttemptTracker - lockout threshold, window, expiry and reset behaviour.
    /// Uses an injected clock so time-based rules are deterministic.
    /// </summary>
    public class LoginAttemptTrackerTests
    {
        private static readonly DateTimeOffset Start = new(2026, 10, 22, 8, 0, 0, TimeSpan.Zero);

        private sealed class FakeClock
        {
            public DateTimeOffset Now { get; set; } = Start;
        }

        private static (LoginAttemptTracker tracker, FakeClock clock) Create(
            int maxFailedAttempts = 3,
            int failureWindowMinutes = 15,
            int lockoutMinutes = 15)
        {
            var clock = new FakeClock();
            var options = Options.Create(new LoginSecuritySettings
            {
                MaxFailedAttempts = maxFailedAttempts,
                FailureWindowMinutes = failureWindowMinutes,
                LockoutMinutes = lockoutMinutes
            });

            return (new LoginAttemptTracker(options, () => clock.Now), clock);
        }

        [Fact]
        public void IsLockedOut_NoFailures_ReturnsFalse()
        {
            var (tracker, _) = Create();

            var locked = tracker.IsLockedOut("alice", out _);

            locked.Should().BeFalse();
        }

        [Fact]
        public void RecordFailure_BelowThreshold_DoesNotLock()
        {
            var (tracker, _) = Create(maxFailedAttempts: 3);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");

            tracker.IsLockedOut("alice", out _).Should().BeFalse();
        }

        [Fact]
        public void RecordFailure_ReachesThreshold_LocksAccount()
        {
            var (tracker, clock) = Create(maxFailedAttempts: 3, lockoutMinutes: 15);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");

            var locked = tracker.IsLockedOut("alice", out var lockedUntil);

            locked.Should().BeTrue();
            lockedUntil.Should().Be(clock.Now.AddMinutes(15));
        }

        [Fact]
        public void RecordSuccess_ClearsFailureHistory()
        {
            var (tracker, _) = Create(maxFailedAttempts: 3);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");
            tracker.RecordSuccess("alice");
            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");

            tracker.IsLockedOut("alice", out _).Should().BeFalse();
        }

        [Fact]
        public void RecordFailure_OutsideWindow_DoesNotAccumulate()
        {
            var (tracker, clock) = Create(maxFailedAttempts: 3, failureWindowMinutes: 15);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");
            clock.Now = clock.Now.AddMinutes(16);
            tracker.RecordFailure("alice");

            tracker.IsLockedOut("alice", out _).Should().BeFalse();
        }

        [Fact]
        public void IsLockedOut_AfterLockoutDuration_ReturnsFalse()
        {
            var (tracker, clock) = Create(maxFailedAttempts: 3, lockoutMinutes: 15);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");

            clock.Now = clock.Now.AddMinutes(14);
            tracker.IsLockedOut("alice", out _).Should().BeTrue();

            clock.Now = clock.Now.AddMinutes(2);
            tracker.IsLockedOut("alice", out _).Should().BeFalse();
        }

        [Fact]
        public void IsLockedOut_UsernameComparisonIsCaseInsensitive()
        {
            var (tracker, _) = Create(maxFailedAttempts: 3);

            tracker.RecordFailure("Alice");
            tracker.RecordFailure("ALICE");
            tracker.RecordFailure("alice");

            tracker.IsLockedOut("aLiCe", out _).Should().BeTrue();
        }

        [Fact]
        public void IsLockedOut_OtherUsernames_AreNotAffected()
        {
            var (tracker, _) = Create(maxFailedAttempts: 3);

            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");
            tracker.RecordFailure("alice");

            tracker.IsLockedOut("bob", out _).Should().BeFalse();
        }
    }
}
