using System.Text.Json;

namespace Raims
{
    /// <summary>The API returned HTTP 429. Consult RetryAfter before retrying a read.</summary>
    public sealed class RateLimitException : ApiErrorException
    {
        internal RateLimitException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
