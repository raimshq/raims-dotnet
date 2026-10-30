using System;
using System.Globalization;

namespace Raims
{
    internal static class RetryPolicy
    {
        private static readonly Random random = new Random();
        internal static bool RetryStatus(int status) => status == 408 || status == 429 || status == 500 || status == 502 || status == 503 || status == 504;
        internal static TimeSpan? Delay(int attempt, ApiResponse? response)
        {
            var retryAfter = ParseRetryAfter(response?.GetHeader("Retry-After"), DateTimeOffset.UtcNow);
            if (retryAfter.HasValue && retryAfter.Value > TimeSpan.FromSeconds(30)) return null;
            double jitter;
            lock (random) jitter = 0.5 + random.NextDouble() * 0.5;
            var delay = TimeSpan.FromMilliseconds(Math.Min(2000, 500 * Math.Pow(2, attempt)) * jitter);
            return retryAfter.HasValue && retryAfter.Value > delay ? retryAfter : delay;
        }
        internal static TimeSpan? ParseRetryAfter(string? value, DateTimeOffset now)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            value = value!.Trim();
            if (long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
            {
                if (seconds > TimeSpan.MaxValue.TotalSeconds) return TimeSpan.MaxValue;
                return TimeSpan.FromSeconds(seconds);
            }
            var digitsOnly = true;
            foreach (var character in value) if (character < '0' || character > '9') { digitsOnly = false; break; }
            if (digitsOnly) return TimeSpan.MaxValue;
            if (DateTimeOffset.TryParseExact(value, "r", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date)) return date <= now ? TimeSpan.Zero : date - now;
            return null;
        }
    }
}
