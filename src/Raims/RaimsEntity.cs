using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raims
{
    /// <summary>Base of typed API responses, preserving fields added after this SDK version.</summary>
    public abstract class RaimsEntity
    {
        /// <summary>Original HTTP response for the root resource.</summary>
        [JsonIgnore]
        public ApiResponse? LastResponse { get; internal set; }
        /// <summary>Fields unknown to the pinned contract. Values may contain sensitive data.</summary>
        [JsonInclude]
        [JsonExtensionData]
        public Dictionary<string, JsonElement>? ExtraFields { get; internal set; }
        /// <summary>Serializes the resource using public API field names.</summary>
        public string ToJson() => JsonSerializer.Serialize(this, GetType(), JsonDefaults.Options);
        /// <summary>Returns the resource type without exposing field values.</summary>
        public override string ToString() => GetType().Name;
    }
}
