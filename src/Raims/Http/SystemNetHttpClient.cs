using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Raims
{
    /// <summary>Reusable system HTTP transport with redirects and cookies disabled.</summary>
    public sealed class SystemNetHttpClient : IRaimsHttpClient, IDisposable
    {
        private readonly HttpClient client;
        /// <summary>Creates a reusable connection pool. Independent connection timeout is available on modern .NET.</summary>
        public SystemNetHttpClient(TimeSpan? connectTimeout = null)
        {
            var connectionTimeout = connectTimeout ?? TimeSpan.FromSeconds(10);
            ClientValidation.Timeout(connectionTimeout, nameof(connectTimeout));
#if NET10_0_OR_GREATER
            var handler = new SocketsHttpHandler { AllowAutoRedirect = false, UseCookies = false, ConnectTimeout = connectionTimeout, PooledConnectionLifetime = TimeSpan.FromMinutes(5) };
#else
            var handler = new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false };
#endif
            client = new HttpClient(handler, true) { Timeout = System.Threading.Timeout.InfiniteTimeSpan };
        }
        /// <summary>Sends one attempt with caller cancellation and a per-attempt timeout.</summary>
        public async Task<ApiResponse> SendAsync(RaimsHttpRequest request, CancellationToken cancellationToken = default)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            ClientValidation.Timeout(request.Timeout, nameof(request));
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            using (var message = new HttpRequestMessage(new HttpMethod(request.Method), request.Url))
            {
                timeout.CancelAfter(request.Timeout);
                if (request.Body != null) message.Content = new StringContent(request.Body, new UTF8Encoding(false), "application/json");
                foreach (var header in request.Headers)
                {
                    if (header.Key.Equals("Content-Type", StringComparison.OrdinalIgnoreCase)) continue;
                    message.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
                try
                {
                    using (var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, timeout.Token).ConfigureAwait(false))
                    {
                        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        foreach (var header in response.Headers) headers[header.Key] = string.Join(", ", header.Value);
                        if (response.Content != null) foreach (var header in response.Content.Headers) headers[header.Key] = string.Join(", ", header.Value);
                        var body = string.Empty;
                        if (response.Content != null)
                        {
                            using (var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                            using (var buffer = new MemoryStream())
                            {
                                var chunk = new byte[8192];
                                int count;
                                while ((count = await stream.ReadAsync(chunk, 0, chunk.Length, timeout.Token).ConfigureAwait(false)) != 0) await buffer.WriteAsync(chunk, 0, count, timeout.Token).ConfigureAwait(false);
                                timeout.Token.ThrowIfCancellationRequested();
                                try { body = new UTF8Encoding(false, true).GetString(buffer.ToArray()); }
                                catch (DecoderFallbackException)
                                {
                                    var metadata = new ApiResponse((int)response.StatusCode, headers, string.Empty);
                                    throw new UnexpectedResponseException("The API returned a response that is not valid UTF-8.", metadata);
                                }
                            }
                        }
                        return new ApiResponse((int)response.StatusCode, headers, body);
                    }
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { throw new ApiConnectionException("The API attempt exceeded its timeout.", "timeout"); }
                catch (HttpRequestException) { throw new ApiConnectionException("The API response was not received.", "http_request"); }
                catch (IOException) { throw new ApiConnectionException("The API response body was not received.", "io"); }
            }
        }
        /// <summary>Closes this transport and its connection pool.</summary>
        public void Dispose() => client.Dispose();
    }
}
