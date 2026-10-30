using System.Text.Json.Serialization;

namespace Raims
{
    /// <summary>Cursor navigation returned with a page of resources.</summary>
    public sealed class Pagination : RaimsEntity
    {
        /// <summary>Applied page size.</summary>
        [JsonInclude, JsonPropertyName("page_size")]
        public int PageSize { get; internal set; }
        /// <summary>Whether a following page exists.</summary>
        [JsonInclude, JsonPropertyName("has_next")]
        public bool HasNext { get; internal set; }
        /// <summary>Whether a preceding page exists.</summary>
        [JsonInclude, JsonPropertyName("has_previous")]
        public bool HasPrevious { get; internal set; }
        /// <summary>Opaque cursor for the following page.</summary>
        [JsonInclude, JsonPropertyName("next_after")]
        public string? NextAfter { get; internal set; }
        /// <summary>Opaque cursor for the preceding page.</summary>
        [JsonInclude, JsonPropertyName("previous_before")]
        public string? PreviousBefore { get; internal set; }
    }
}
