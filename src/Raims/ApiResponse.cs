using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Raims
{
    /// <summary>HTTP metadata and response text when available. Bodies may contain sensitive data.</summary>
    public sealed class ApiResponse
    {
        /// <summary>Creates an HTTP response returned by a custom transport.</summary>
        public ApiResponse(int statusCode, IDictionary<string, string>? headers, string body)
        {
            if (statusCode < 100 || statusCode > 599) throw new ArgumentOutOfRangeException(nameof(statusCode));
            StatusCode = statusCode;
            var copy = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (headers != null) foreach (var pair in headers) copy[pair.Key] = pair.Value;
            Headers = new ReadOnlyDictionary<string, string>(copy);
            Body = body ?? throw new ArgumentNullException(nameof(body));
        }
        /// <summary>HTTP status returned by the server.</summary>
        public int StatusCode { get; }
        /// <summary>Response headers. Avoid recording arbitrary headers in logs.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
        /// <summary>Original response body, which may contain secrets or personal data.</summary>
        /// <remarks>Empty on a protocol exception when invalid UTF-8 prevents decoding the received body.</remarks>
        public string Body { get; }
        /// <summary>Returns a response header without case sensitivity.</summary>
        public string? GetHeader(string name) => Headers.TryGetValue(name, out var value) ? value : null;
        /// <summary>Request-Id, with X-Request-Id as a compatibility alias.</summary>
        public string? RequestId => GetHeader("Request-Id") ?? GetHeader("X-Request-Id");
        /// <summary>Returns status metadata without response bodies or headers.</summary>
        public override string ToString() => "HTTP " + StatusCode;
    }
}
