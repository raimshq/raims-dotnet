using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Raims
{
    /// <summary>Base of resource services sharing an isolated API client.</summary>
    public abstract class BaseService
    {
        private readonly ApiRequestor requestor;
        internal BaseService(ApiRequestor requestor) { this.requestor = requestor; }
        private protected Task<T> RequestAsync<T>(string operationId, Dictionary<string, string>? pathParameters, BaseOptions? options, RequestOptions? requestOptions, CancellationToken cancellationToken) where T : RaimsEntity => requestor.RequestAsync<T>(operationId, pathParameters, options, requestOptions, cancellationToken);
    }
}
