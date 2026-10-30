using System.Text.Json;

namespace Raims
{
    /// <summary>The requested resource was not found.</summary>
    public sealed class NotFoundException : ApiErrorException
    {
        internal NotFoundException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
