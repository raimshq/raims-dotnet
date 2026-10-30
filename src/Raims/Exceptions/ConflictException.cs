using System.Text.Json;

namespace Raims
{
    /// <summary>The request conflicts with the resource state or operation identity.</summary>
    public sealed class ConflictException : ApiErrorException
    {
        internal ConflictException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
