using System.Text.Json;

namespace Raims
{
    /// <summary>The credential lacks permission for the operation.</summary>
    public sealed class PermissionException : ApiErrorException
    {
        internal PermissionException(ApiResponse response, JsonElement? error) : base(response, error) { }
    }
}
