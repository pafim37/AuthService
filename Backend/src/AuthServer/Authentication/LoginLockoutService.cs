using AuthServer.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;

namespace AuthServer.Authentication
{
    [Component(typeof(LoginLockoutService))]
    public class LoginLockoutService(IMemoryCache cache)
    {
        private const int MaxFailedAttempts = 5;
        private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(3);
        private static readonly TimeSpan AttemptWindow = TimeSpan.FromMinutes(3);

        public bool IsLockedOut(string login, string? ipAddress)
        {
            LoginAttemptState state = GetState(login, ipAddress);
            return state.LockedOutUntilUtc > DateTimeOffset.UtcNow;
        }

        public void RecordFailedAttempt(string login, string? ipAddress)
        {
            string key = GetKey(login, ipAddress);
            LoginAttemptState state = GetState(login, ipAddress);
            state.FailedAttempts++;

            if (state.FailedAttempts >= MaxFailedAttempts)
            {
                state.LockedOutUntilUtc = DateTimeOffset.UtcNow.Add(LockoutDuration);
            }

            cache.Set(key, state, AttemptWindow);
        }

        public void Reset(string login, string? ipAddress)
        {
            cache.Remove(GetKey(login, ipAddress));
        }

        private LoginAttemptState GetState(string login, string? ipAddress)
        {
            return cache.GetOrCreate(GetKey(login, ipAddress), entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = AttemptWindow;
                return new LoginAttemptState();
            })!;
        }

        private static string GetKey(string login, string? ipAddress)
        {
            return $"login-lockout:{login.Trim().ToUpperInvariant()}:{ipAddress ?? "unknown"}";
        }

        private sealed class LoginAttemptState
        {
            public int FailedAttempts { get; set; }
            public DateTimeOffset? LockedOutUntilUtc { get; set; }
        }
    }
}
