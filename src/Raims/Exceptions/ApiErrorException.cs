using System;
using System.Text.Json;

namespace Raims
{
    /// <summary>Base exception for unsuccessful HTTP responses.</summary>
    public class ApiErrorException : RaimsException
    {
        internal ApiErrorException(ApiResponse response, JsonElement? error) : base("The API returned HTTP " + response.StatusCode + ".") { Response = response; Error = error; }
        /// <summary>Original response, including headers and potentially sensitive body.</summary>
        public ApiResponse Response { get; }
        /// <summary>Parsed error object when the API returned an error envelope.</summary>
        public JsonElement? Error { get; }
        /// <summary>HTTP status of this failure.</summary>
        public int HttpStatus => Response.StatusCode;
        /// <summary>Request-Id returned by the API.</summary>
        public string? RequestId => Response.RequestId;
        /// <summary>Stable API error code when present.</summary>
        public string? ErrorCode => ReadString("code");
        /// <summary>Server-provided explanation. It may contain request-specific information.</summary>
        public string? ErrorMessage => ReadString("message");
        /// <summary>Retry-After expressed as a duration, when valid.</summary>
        public TimeSpan? RetryAfter => RetryPolicy.ParseRetryAfter(Response.GetHeader("Retry-After"), DateTimeOffset.UtcNow);
        private string? ReadString(string name) => Error.HasValue && Error.Value.ValueKind == JsonValueKind.Object && Error.Value.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
    }
}
