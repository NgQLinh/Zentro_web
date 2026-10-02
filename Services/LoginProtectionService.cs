using System.Collections.Concurrent;

namespace Zentro.Services
{
    public class LoginProtectionService
    {
        private const int MaxFailures = 3;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(10);
        private readonly ConcurrentDictionary<string, AttemptState> attempts = new();

        public bool IsLocked(string clientKey)
        {
            return attempts.TryGetValue(clientKey, out var state)
                && state.LockedUntil > DateTimeOffset.UtcNow;
        }

        public TimeSpan GetRemainingLockout(string clientKey)
        {
            if (!attempts.TryGetValue(clientKey, out var state) || state.LockedUntil is null)
            {
                return TimeSpan.Zero;
            }

            var remaining = state.LockedUntil.Value - DateTimeOffset.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        public void RegisterFailure(string clientKey)
        {
            attempts.AddOrUpdate(clientKey,
                _ => new AttemptState { FailureCount = 1 },
                (_, state) =>
                {
                    if (state.LockedUntil > DateTimeOffset.UtcNow)
                    {
                        return state;
                    }

                    state.FailureCount++;
                    if (state.FailureCount >= MaxFailures)
                    {
                        state.FailureCount = 0;
                        state.LockedUntil = DateTimeOffset.UtcNow.Add(LockoutDuration);
                    }

                    return state;
                });
        }

        public void Reset(string clientKey)
        {
            attempts.TryRemove(clientKey, out _);
        }

        private sealed class AttemptState
        {
            public int FailureCount { get; set; }
            public DateTimeOffset? LockedUntil { get; set; }
        }
    }
}
