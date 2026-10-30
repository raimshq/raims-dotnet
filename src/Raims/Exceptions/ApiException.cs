using System.Text.Json;

namespace Raims
{
    /// <summary>An HTTP failure outside the more specific categories.</summary>
    public sealed class ApiException : ApiErrorException
    {
        internal ApiException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
