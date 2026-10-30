using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Raims
{
    internal sealed class ApiRequestor : IDisposable
    {
        private readonly string apiKey;
        private readonly Uri baseUrl;
        private readonly TimeSpan timeout;
        private readonly int maxRetries;
        private readonly IRaimsHttpClient httpClient;
        private readonly bool ownsHttpClient;
        private readonly Action<RequestDiagnostics>? observer;
        private readonly AsyncLocal<bool> notifyingObserver = new AsyncLocal<bool>();
        private bool disposed;

        internal ApiRequestor(RaimsClientOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            ClientValidation.ApiKey(options.ApiKey);
            ClientValidation.Timeout(options.Timeout, nameof(options.Timeout));
            ClientValidation.Timeout(options.ConnectTimeout, nameof(options.ConnectTimeout));
            ClientValidation.Retries(options.MaxNetworkRetries);
            apiKey = options.ApiKey;
            baseUrl = ClientValidation.BaseUrl(options.BaseUrl);
            timeout = options.Timeout;
            maxRetries = options.MaxNetworkRetries;
            observer = options.RequestObserver;
            ownsHttpClient = options.HttpClient == null;
            httpClient = options.HttpClient ?? new SystemNetHttpClient(options.ConnectTimeout);
        }

        internal Task<T> RequestAsync<T>(string operationId, Dictionary<string, string>? pathParameters, BaseOptions? parameters, RequestOptions? requestOptions, CancellationToken cancellationToken) where T : RaimsEntity
        {
            var paths = pathParameters == null ? new Dictionary<string, string>(StringComparer.Ordinal) : new Dictionary<string, string>(pathParameters, StringComparer.Ordinal);
            return RequestSnapshotAsync<T>(operationId, paths, parameters?.ToParameters() ?? new Dictionary<string, object?>(StringComparer.Ordinal), (requestOptions ?? new RequestOptions()).Snapshot(), cancellationToken);
        }

        private async Task<T> RequestSnapshotAsync<T>(string operationId, Dictionary<string, string> pathParameters, Dictionary<string, object?> parameters, RequestOptions requestOptions, CancellationToken cancellationToken) where T : RaimsEntity
        {
            if (disposed) throw new ObjectDisposedException(nameof(RaimsClient));
            cancellationToken.ThrowIfCancellationRequested();
            var operation = ContractMetadata.Operation(operationId);
            var credential = requestOptions.ApiKey ?? apiKey;
            ClientValidation.ApiKey(credential);
            ClientValidation.IdempotencyKey(requestOptions.IdempotencyKey);
            var attemptTimeout = requestOptions.Timeout ?? timeout;
            var retries = requestOptions.MaxNetworkRetries ?? maxRetries;
            ClientValidation.Timeout(attemptTimeout, nameof(requestOptions.Timeout));
            ClientValidation.Retries(retries);
            if (requestOptions.IdempotencyKey != null && !operation.GetProperty("idempotency").GetBoolean()) throw new ArgumentException("This endpoint does not accept an idempotency key.");
            var method = operation.GetProperty("method").GetString()!;
            var template = operation.GetProperty("path").GetString()!;
            var path = BuildPath(template, pathParameters);
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["Authorization"] = "Bearer " + credential, ["Accept"] = "application/json", ["User-Agent"] = "RaimsDotnet/" + RaimsClient.Version };
            if (requestOptions.IdempotencyKey != null) headers["Idempotency-Key"] = requestOptions.IdempotencyKey;
            string? body = null;
            try
            {
                var bodySchema = operation.GetProperty("body");
                if (bodySchema.ValueKind == JsonValueKind.String)
                {
                    body = JsonSerializer.Serialize(parameters, JsonDefaults.Options);
                    using (var parsed = JsonDocument.Parse(body)) if (!ContractMetadata.IsValid(parsed.RootElement, ContractMetadata.Schema(bodySchema.GetString()!))) throw new ArgumentException("Request body fields do not match the published JSON structure.");
                    headers["Content-Type"] = "application/json";
                }
                else
                {
                    ValidateQuery(parameters, operation.GetProperty("query"));
                    var query = EncodeQuery(parameters);
                    if (query.Length != 0) path += "?" + query;
                }
            }
            catch (JsonException) { throw new ArgumentException("Request parameters must contain valid JSON values."); }
            var request = new RaimsHttpRequest(method, new Uri(baseUrl, path), headers, body, attemptTimeout);
            if (method != "GET" && method != "HEAD") retries = 0;
            var response = await SendAsync(operationId, template, request, retries, cancellationToken).ConfigureAwait(false);
            var resource = InterpretResponse<T>(operation, response);
            if (resource is IPagedCollection collection)
            {
                collection.AttachPageLoader(async (nextParameters, token) => await RequestSnapshotAsync<T>(operationId, pathParameters, nextParameters, requestOptions, token).ConfigureAwait(false), parameters);
            }
            return resource;
        }

        private async Task<ApiResponse> SendAsync(string operationId, string path, RaimsHttpRequest request, int retries, CancellationToken cancellationToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clock = Stopwatch.StartNew();
                ApiResponse response;
                try { response = await httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false); }
                catch (ApiConnectionException error)
                {
                    var delay = attempt < retries ? RetryPolicy.Delay(attempt, null) : null;
                    Notify(new RequestDiagnostics(operationId, request.Method, path, attempt + 1, clock.Elapsed.TotalMilliseconds, null, null, error.TransportErrorCode, delay));
                    if (!delay.HasValue) throw;
                    await Task.Delay(delay.Value, cancellationToken).ConfigureAwait(false);
                    continue;
                }
                catch (UnexpectedResponseException error)
                {
                    Notify(new RequestDiagnostics(operationId, request.Method, path, attempt + 1, clock.Elapsed.TotalMilliseconds, error.Response?.StatusCode, error.Response?.RequestId, null, null));
                    throw;
                }
                if (response == null) throw new UnexpectedResponseException("The HTTP transport returned no response.");
                var responseDelay = attempt < retries && RetryPolicy.RetryStatus(response.StatusCode) ? RetryPolicy.Delay(attempt, response) : null;
                Notify(new RequestDiagnostics(operationId, request.Method, path, attempt + 1, clock.Elapsed.TotalMilliseconds, response.StatusCode, response.RequestId, null, responseDelay));
                if (!responseDelay.HasValue) return response;
                await Task.Delay(responseDelay.Value, cancellationToken).ConfigureAwait(false);
            }
        }

        private void Notify(RequestDiagnostics diagnostics)
        {
            if (observer == null || notifyingObserver.Value) return;
            notifyingObserver.Value = true;
            try { observer(diagnostics); }
            catch (Exception) { }
            finally { notifyingObserver.Value = false; }
        }

        private static T InterpretResponse<T>(JsonElement operation, ApiResponse response) where T : RaimsEntity
        {
            if (response.StatusCode >= 300 && response.StatusCode < 400) throw new UnexpectedResponseException("The API returned a redirect. Redirects are not followed.", response);
            if (response.StatusCode < 200 || response.StatusCode >= 300) throw BuildApiError(response);
            if (!operation.GetProperty("responses").TryGetProperty(response.StatusCode.ToString(CultureInfo.InvariantCulture), out var schema)) throw new UnexpectedResponseException("The API returned an unexpected success status.", response);
            if (schema.ValueKind == JsonValueKind.Null)
            {
                if (!string.IsNullOrWhiteSpace(response.Body) || typeof(T) != typeof(EmptyResponse)) throw new UnexpectedResponseException("The API returned an unexpected response body.", response);
                return (T)(RaimsEntity)new EmptyResponse { LastResponse = response };
            }
            var contentType = response.GetHeader("Content-Type");
            if (contentType != null && !Regex.IsMatch(contentType, @"^application/(?:[a-z0-9.+-]+\+)?json(?:\s*;|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)) throw new UnexpectedResponseException("The API returned a non-JSON success response.", response);
            try
            {
                using (var parsed = JsonDocument.Parse(response.Body))
                {
                    if (!ContractMetadata.IsValid(parsed.RootElement, ContractMetadata.Schema(schema.GetString()!))) throw new UnexpectedResponseException("The API response does not match the published JSON structure.", response);
                }
                var resource = JsonSerializer.Deserialize<T>(response.Body, JsonDefaults.Options);
                if (resource == null) throw new UnexpectedResponseException("The API returned an empty resource.", response);
                resource.LastResponse = response;
                return resource;
            }
            catch (JsonException) { throw new UnexpectedResponseException("The API returned invalid JSON or an unsupported field value.", response); }
        }

        private static ApiErrorException BuildApiError(ApiResponse response)
        {
            JsonElement? error = null;
            try
            {
                using (var parsed = JsonDocument.Parse(response.Body)) if (parsed.RootElement.ValueKind == JsonValueKind.Object && parsed.RootElement.TryGetProperty("error", out var value) && value.ValueKind == JsonValueKind.Object) error = value.Clone();
            }
            catch (JsonException) { }
            switch (response.StatusCode)
            {
                case 400: case 405: case 413: case 415: case 422: return new InvalidRequestException(response, error);
                case 401: return new AuthenticationException(response, error);
                case 403: return new PermissionException(response, error);
                case 404: return new NotFoundException(response, error);
                case 409: return new ConflictException(response, error);
                case 429: return new RateLimitException(response, error);
                default: return new ApiException(response, error);
            }
        }

        private static string BuildPath(string template, Dictionary<string, string> parameters)
        {
            var expected = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(template, "\\{([^}]+)\\}"))
            {
                var name = match.Groups[1].Value;
                expected.Add(name);
                if (!parameters.TryGetValue(name, out var value) || string.IsNullOrEmpty(value) || value == "." || value == "..") throw new ArgumentException("Each path parameter must be a non-empty resource identifier.");
                foreach (var character in value) if (character < 32 || character == 127) throw new ArgumentException("Resource identifiers must not contain control characters.");
                template = template.Replace("{" + name + "}", Uri.EscapeDataString(value));
            }
            foreach (var name in parameters.Keys) if (!expected.Contains(name)) throw new ArgumentException("An unexpected path parameter was supplied.");
            return template;
        }

        private static void ValidateQuery(Dictionary<string, object?> values, JsonElement query)
        {
            foreach (var pair in values)
            {
                if (!query.TryGetProperty(pair.Key, out var schema)) throw new ArgumentException("This endpoint received an unsupported query parameter.");
                if (pair.Value == null) continue;
                using (var parsed = JsonDocument.Parse(JsonSerializer.Serialize(pair.Value, JsonDefaults.Options))) if (!ContractMetadata.IsValid(parsed.RootElement, schema)) throw new ArgumentException("Query parameters do not match the published structure.");
            }
        }
        private static string EncodeQuery(Dictionary<string, object?> values)
        {
            var pairs = new List<string>();
            foreach (var pair in values)
            {
                if (pair.Value == null) continue;
                if (pair.Value is IEnumerable sequence && !(pair.Value is string)) foreach (var item in sequence) pairs.Add(Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(QueryScalar(item)));
                else pairs.Add(Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(QueryScalar(pair.Value)));
            }
            return string.Join("&", pairs);
        }
        private static string QueryScalar(object? value)
        {
            if (value is string text) return text;
            if (value is bool boolean) return boolean ? "true" : "false";
            if (value is IFormattable numeric && !(value is DateTime) && !(value is DateTimeOffset)) return numeric.ToString(null, CultureInfo.InvariantCulture);
            throw new ArgumentException("Query values must be scalar values or lists of scalar values.");
        }
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (ownsHttpClient && httpClient is IDisposable disposable) disposable.Dispose();
        }
    }
}
