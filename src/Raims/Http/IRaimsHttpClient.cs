using System.Threading;
using System.Threading.Tasks;

namespace Raims
{
    /// <summary>HTTP transport for authenticated API calls. Implementations must not follow redirects.</summary>
    public interface IRaimsHttpClient
    {
        /// <summary>Sends one attempt. Throw ApiConnectionException only when no response was received.</summary>
        Task<ApiResponse> SendAsync(RaimsHttpRequest request, CancellationToken cancellationToken = default);
    }
}
