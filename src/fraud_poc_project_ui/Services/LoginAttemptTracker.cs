using fraud_poc_project.Interfaces;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;

namespace fraud_poc_project.Services
{
    public class LoginSecuritySettings
    {
        public int MaxFailedAttempts { get; set; } = 5;

        public int FailureWindowMinutes { get; set; } = 15;

        public int LockoutMinutes { get; set; } = 15;

    }

    public class LoginAttemptTracker : ILoginAttemptTracker
    {
        private readonly int _maxFailedAttempts;
        private readonly TimeSpan _failureWindow;
        private readonly TimeSpan _lockoutDuration;
        private readonly ConcurrentDictionary<string, AttemptState> _states =
            new(StringComparer.OrdinalIgnoreCase);

        private readonly Func<DateTimeOffset> _clock;

        private sealed class AttemptState
        {
            public int Failures;
            public DateTimeOffset WindowStart;
            public DateTimeOffset LockedUntil;
        }

        public LoginAttemptTracker(IOptions<LoginSecuritySettings> options, Func<DateTimeOffset>? clock = null)
        {
            var settings = options.Value;
            _maxFailedAttempts = Math.Max(1, settings.MaxFailedAttempts);
            _failureWindow = TimeSpan.FromMinutes(Math.Max(1, settings.FailureWindowMinutes));
            _lockoutDuration = TimeSpan.FromMinutes(Math.Max(1, settings.LockoutMinutes));
            _clock = clock ?? (() => DateTimeOffset.UtcNow);
        }

        public bool IsLockedOut(string username, out DateTimeOffset lockedUntil)
        {
            lockedUntil = default;
            if (string.IsNullOrEmpty(username) || !_states.TryGetValue(username, out var state))
                return false;

            var now = _clock();
            lock (state)
            {
                lockedUntil = state.LockedUntil;
                return state.LockedUntil > now;
            }
        }

        public void RecordFailure(string username)
        {
            if (string.IsNullOrEmpty(username))
                return;

            var now = _clock();

            var state = _states.GetOrAdd(username, _ => new AttemptState { WindowStart = now });
            lock (state)
            {
                // Start a new counting window if the previous one has expired.
                if (now - state.WindowStart > _failureWindow)
                {
                    state.Failures = 0;
                    state.WindowStart = now;
                }

                state.Failures++;

                if (state.Failures >= _maxFailedAttempts)
                {
                    state.LockedUntil = now + _lockoutDuration;
                    state.Failures = 0;
                    state.WindowStart = now;
                }
            }
        }

        public void RecordSuccess(string username)
        {
            if (!string.IsNullOrEmpty(username))
                _states.TryRemove(username, out _);
        }
    }
}
