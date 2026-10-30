using System.Text.Json;

namespace Raims
{
    /// <summary>The API rejected request parameters or content.</summary>
    public sealed class InvalidRequestException : ApiErrorException
    {
        internal InvalidRequestException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
