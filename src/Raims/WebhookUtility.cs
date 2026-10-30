using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Raims
{
    /// <summary>Authenticates a webhook delivery using its original HTTP body.</summary>
    public static class WebhookUtility
    {
        private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(false, true);

        /// <summary>Checks the signature and returns an independent JSON object.</summary>
        /// <remarks>The caller must durably deduplicate the event ID and verify its environment.</remarks>
        public static JsonElement ConstructEvent(byte[] payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, long toleranceSeconds = 300, DateTimeOffset? now = null)
        {
            VerifySignature(payload, headers, secret, toleranceSeconds, now);
            try
            {
                using (var document = JsonDocument.Parse(payload))
                {
                    if (document.RootElement.ValueKind != JsonValueKind.Object)
                    {
                        throw new SignatureVerificationException("The webhook body must be a JSON object.");
                    }

                    return document.RootElement.Clone();
                }
            }
            catch (JsonException)
            {
                throw new SignatureVerificationException("The webhook body is not valid JSON.");
            }
        }

        /// <summary>Checks the signature before parsing an original UTF-8 JSON string.</summary>
        public static JsonElement ConstructEvent(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, long toleranceSeconds = 300, DateTimeOffset? now = null)
        {
            return ConstructEvent(Encode(payload), headers, secret, toleranceSeconds, now);
        }

        /// <summary>Authenticates the exact body bytes without interpreting their JSON.</summary>
        /// <remarks>Zero tolerance requires the same second; it does not disable timestamp verification.</remarks>
        public static void VerifySignature(byte[] payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, long toleranceSeconds = 300, DateTimeOffset? now = null)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            long current = (now ?? DateTimeOffset.UtcNow).ToUnixTimeSeconds();
            if (toleranceSeconds < 0) throw new ArgumentOutOfRangeException(nameof(toleranceSeconds));
            if (current < 0) throw new ArgumentOutOfRangeException(nameof(now));

            var signingHeaders = ReadHeaders(headers);
            string id = signingHeaders[0];
            string timestamp = signingHeaders[1];
            string signatures = signingHeaders[2];
            if (!Regex.IsMatch(id, "\\A[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\\z", RegexOptions.CultureInvariant))
            {
                throw new SignatureVerificationException("The webhook delivery ID is invalid.");
            }

            if (!Regex.IsMatch(timestamp, "\\A[0-9]{1,12}\\z", RegexOptions.CultureInvariant) ||
                !long.TryParse(timestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long signedAt))
            {
                throw new SignatureVerificationException("The webhook timestamp is invalid.");
            }

            long difference = signedAt > current ? signedAt - current : current - signedAt;
            if (difference > toleranceSeconds)
            {
                throw new SignatureVerificationException("The webhook timestamp is outside the allowed tolerance.");
            }

            byte[] key = DecodeSecret(secret);
            byte[] prefix = Encoding.UTF8.GetBytes(id + "." + timestamp + ".");
            byte[] signed = new byte[checked(prefix.Length + payload.Length)];
            Buffer.BlockCopy(prefix, 0, signed, 0, prefix.Length);
            Buffer.BlockCopy(payload, 0, signed, prefix.Length, payload.Length);
            byte[] expected;
            try
            {
                using (var hmac = new HMACSHA256(key))
                {
                    expected = hmac.ComputeHash(signed);
                }
            }
            finally
            {
                Array.Clear(key, 0, key.Length);
                Array.Clear(signed, 0, signed.Length);
            }

            bool matches = false;
            foreach (string entry in signatures.Split(','))
            {
                string candidate = entry.Trim(' ', '\t');
                if (candidate.Length != 67 || !candidate.StartsWith("v1=", StringComparison.OrdinalIgnoreCase)) continue;
                var received = new byte[32];
                bool valid = true;
                for (int i = 0; i < received.Length; i++)
                {
                    int high = Hex(candidate[3 + i * 2]);
                    int low = Hex(candidate[4 + i * 2]);
                    if (high < 0 || low < 0) { valid = false; break; }
                    received[i] = (byte)((high << 4) | low);
                }

                if (!valid) continue;
                int differenceBytes = 0;
                for (int i = 0; i < expected.Length; i++) differenceBytes |= expected[i] ^ received[i];
                matches |= differenceBytes == 0;
            }

            if (!matches) throw new SignatureVerificationException("No webhook signature matches the configured secret.");
        }

        /// <summary>Authenticates an original UTF-8 body string without interpreting its JSON.</summary>
        public static void VerifySignature(string payload, IEnumerable<KeyValuePair<string, string>> headers, string secret, long toleranceSeconds = 300, DateTimeOffset? now = null)
        {
            VerifySignature(Encode(payload), headers, secret, toleranceSeconds, now);
        }

        private static byte[] Encode(string payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload));
            try { return StrictUtf8.GetBytes(payload); }
            catch (EncoderFallbackException) { throw new SignatureVerificationException("The webhook body is not valid UTF-8."); }
        }

        private static string[] ReadHeaders(IEnumerable<KeyValuePair<string, string>> headers)
        {
            var names = new[] { "webhook-id", "webhook-timestamp", "webhook-signature" };
            var values = new string[3];
            foreach (var header in headers)
            {
                for (int i = 0; i < names.Length; i++)
                {
                    if (!string.Equals(header.Key, names[i], StringComparison.OrdinalIgnoreCase)) continue;
                    if (values[i] != null) throw new SignatureVerificationException("A webhook signing header occurs more than once.");
                    if (string.IsNullOrEmpty(header.Value)) throw new SignatureVerificationException("A required webhook signing header is missing or invalid.");
                    foreach (char c in header.Value)
                    {
                        if ((c < 32 && c != '\t') || c == 127) throw new SignatureVerificationException("A required webhook signing header is missing or invalid.");
                    }

                    values[i] = header.Value;
                }
            }

            foreach (string value in values)
            {
                if (value == null) throw new SignatureVerificationException("A required webhook signing header is missing or invalid.");
            }

            return values;
        }

        private static byte[] DecodeSecret(string secret)
        {
            if (secret == null || !Regex.IsMatch(secret, "\\Awhsec_[0-9A-Za-z]{1,43}\\z", RegexOptions.CultureInvariant))
                throw new SignatureVerificationException("The webhook signing secret is invalid.");
            string encoded = secret.Substring(6);
            if (encoded.Length > 1 && encoded[0] == '0') throw new SignatureVerificationException("The webhook signing secret is invalid.");
            var bytes = new byte[32];
            foreach (char c in encoded)
            {
                int carry = Alphabet.IndexOf(c);
                for (int i = 0; i < bytes.Length; i++)
                {
                    carry += bytes[i] * 62;
                    bytes[i] = (byte)(carry & 255);
                    carry >>= 8;
                }

                if (carry != 0) throw new SignatureVerificationException("The webhook signing secret is invalid.");
            }

            return bytes;
        }

        private static int Hex(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            return -1;
        }
    }
}
