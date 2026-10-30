using System;
using System.Text.Json.Serialization;

namespace Raims
{
    /// <summary>Overrides applying to a single API operation.</summary>
    public sealed class RequestOptions
    {
        /// <summary>Overrides the client's API key for this request.</summary>
        [JsonIgnore]
        public string? ApiKey { get; set; }
        /// <summary>Previously persisted key identifying this business intent.</summary>
        [JsonIgnore]
        public string? IdempotencyKey { get; set; }
        /// <summary>Overrides the maximum duration of each HTTP attempt.</summary>
        public TimeSpan? Timeout { get; set; }
        /// <summary>Overrides additional attempts for reads, from zero to five.</summary>
        public int? MaxNetworkRetries { get; set; }
        internal RequestOptions Snapshot() => new RequestOptions { ApiKey = ApiKey, IdempotencyKey = IdempotencyKey, Timeout = Timeout, MaxNetworkRetries = MaxNetworkRetries };
        /// <summary>Returns the options type without exposing credentials or idempotency keys.</summary>
        public override string ToString() => nameof(RequestOptions);
    }
}
