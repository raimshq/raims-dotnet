using System;
using System.Net;

namespace Raims
{
    internal static class ClientValidation
    {
        internal static void ApiKey(string? value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("An API key is required.");
            foreach (var character in value!) if (character < 33 || character > 126) throw new ArgumentException("The API key must contain visible ASCII characters without spaces.");
        }
        internal static void IdempotencyKey(string? value)
        {
            if (value == null) return;
            if (value.Length < 1 || value.Length > 255) throw new ArgumentException("The idempotency key must contain 1 to 255 visible ASCII characters.");
            foreach (var character in value) if (character < 33 || character > 126) throw new ArgumentException("The idempotency key must contain 1 to 255 visible ASCII characters.");
        }
        internal static void Timeout(TimeSpan value, string parameter)
        {
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue) throw new ArgumentOutOfRangeException(parameter, "The timeout must be positive and within the supported timer range.");
        }
        internal static void Retries(int value)
        {
            if (value < 0 || value > 5) throw new ArgumentOutOfRangeException(nameof(value), "Additional read attempts must be between zero and five.");
        }
        internal static Uri BaseUrl(string? value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentException("The API base URL must be a trusted HTTPS origin.");
            foreach (var character in value!) if (character <= 32 || character == 127) throw new ArgumentException("The API base URL must not contain spaces or control characters.");
            var authorityStart = value!.IndexOf("://", StringComparison.Ordinal);
            var pathStart = authorityStart < 0 ? -1 : value.IndexOf('/', authorityStart + 3);
            if (authorityStart < 0 || pathStart >= 0 && pathStart != value.Length - 1 || value.IndexOf('?') >= 0 || value.IndexOf('#') >= 0) throw new ArgumentException("The API base URL must be an origin without a path, query or fragment.");
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.UserInfo.Length != 0 || uri.AbsolutePath != "/" || uri.Query.Length != 0 || uri.Fragment.Length != 0 || value!.IndexOf('\\') >= 0) throw new ArgumentException("The API base URL must be an origin without credentials, a path, query or fragment.");
            var host = uri.Host.Trim('[', ']');
            var loopback = host.Equals("localhost", StringComparison.OrdinalIgnoreCase) || IPAddress.TryParse(host, out var address) && (address.Equals(IPAddress.Loopback) || address.Equals(IPAddress.IPv6Loopback));
            if (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && loopback)) throw new ArgumentException("The API base URL requires HTTPS; HTTP is allowed only on loopback.");
            return uri;
        }
    }
}
