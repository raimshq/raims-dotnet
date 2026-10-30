using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Raims
{
    /// <summary>One authenticated transport attempt. Do not log its headers, URL or body.</summary>
    public sealed class RaimsHttpRequest
    {
        internal RaimsHttpRequest(string method, Uri url, IDictionary<string, string> headers, string? body, TimeSpan timeout)
        { Method = method; Url = url; Headers = new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(headers, StringComparer.OrdinalIgnoreCase)); Body = body; Timeout = timeout; }
        /// <summary>HTTP method.</summary>
        public string Method { get; }
        /// <summary>Authenticated destination. It may contain resource identifiers and query values.</summary>
        public Uri Url { get; }
        /// <summary>Headers including the credential. Never record them in logs.</summary>
        public IReadOnlyDictionary<string, string> Headers { get; }
        /// <summary>JSON request body when required by the operation.</summary>
        public string? Body { get; }
        /// <summary>Maximum duration for this attempt.</summary>
        public TimeSpan Timeout { get; }
        /// <summary>Returns the method without exposing the authenticated destination or content.</summary>
        public override string ToString() => Method + " [API request]";
    }
}
