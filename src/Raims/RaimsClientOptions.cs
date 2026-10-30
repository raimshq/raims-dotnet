using System;
using System.Text.Json.Serialization;

namespace Raims
{
    /// <summary>Configuration for an isolated API client.</summary>
    public sealed class RaimsClientOptions
    {
        /// <summary>Bearer API key. The key determines the account and environment.</summary>
        [JsonIgnore]
        public string ApiKey { get; set; } = string.Empty;
        /// <summary>Trusted HTTPS origin receiving the API credential.</summary>
        public string BaseUrl { get; set; } = "https://api.raims.com";
        /// <summary>Maximum duration of each HTTP attempt, including response-body reads.</summary>
        public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(80);
        /// <summary>Maximum connection establishment duration for the default transport.</summary>
        public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);
        /// <summary>Additional attempts for reads only, from zero to five.</summary>
        public int MaxNetworkRetries { get; set; }
        /// <summary>Optional caller-owned transport. It must not follow redirects or expose credentials.</summary>
        [JsonIgnore]
        public IRaimsHttpClient? HttpClient { get; set; }
        /// <summary>Optional observer receiving request metadata without query strings or bodies.</summary>
        [JsonIgnore]
        public Action<RequestDiagnostics>? RequestObserver { get; set; }
        /// <summary>Returns configuration metadata without exposing credentials.</summary>
        public override string ToString() => nameof(RaimsClientOptions);
    }
}
