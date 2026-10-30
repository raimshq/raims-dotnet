using System.Text.Json;

namespace Raims
{
    /// <summary>Authentication failed with HTTP 401.</summary>
    public sealed class AuthenticationException : ApiErrorException
    {
        internal AuthenticationException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
