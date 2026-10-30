using System;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raims
{
    internal static class ExactInteger
    {
        internal static bool TryInt64(string raw, out long value)
        {
            value = 0;
            var negative = raw.Length != 0 && raw[0] == '-';
            var unsigned = negative ? raw.Substring(1) : raw;
            var exponentPosition = unsigned.IndexOfAny(new[] { 'e', 'E' });
            var mantissa = exponentPosition < 0 ? unsigned : unsigned.Substring(0, exponentPosition);
            var point = mantissa.IndexOf('.');
            var fractionLength = point < 0 ? 0 : mantissa.Length - point - 1;
            var digits = point < 0 ? mantissa : mantissa.Remove(point, 1);
            digits = digits.TrimStart('0');
            if (digits.Length == 0) return true;
            long exponent = 0;
            if (exponentPosition >= 0 && !long.TryParse(unsigned.Substring(exponentPosition + 1), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out exponent)) return false;
            if (exponent < -100000 || exponent > 100000) return false;
            var scale = exponent - fractionLength;
            if (scale >= 0)
            {
                if (digits.Length + scale > 19) return false;
                digits += new string('0', (int)scale);
            }
            else
            {
                var remove = -scale;
                if (remove >= digits.Length) return false;
                for (var index = digits.Length - (int)remove; index < digits.Length; index++) if (digits[index] != '0') return false;
                digits = digits.Substring(0, digits.Length - (int)remove);
                if (digits.Length > 19) return false;
            }
            return long.TryParse((negative ? "-" : string.Empty) + digits, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
        }
    }

    internal sealed class ExactInt64Converter : JsonConverter<long>
    {
        public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("An integer was expected.");
            using (var document = JsonDocument.ParseValue(ref reader))
            {
                if (!ExactInteger.TryInt64(document.RootElement.GetRawText(), out var value)) throw new JsonException("An exact 64-bit integer was expected.");
                return value;
            }
        }
        public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }

    internal sealed class ExactInt32Converter : JsonConverter<int>
    {
        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.Number) throw new JsonException("An integer was expected.");
            using (var document = JsonDocument.ParseValue(ref reader))
            {
                if (!ExactInteger.TryInt64(document.RootElement.GetRawText(), out var value) || value < int.MinValue || value > int.MaxValue) throw new JsonException("An exact 32-bit integer was expected.");
                return (int)value;
            }
        }
        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) => writer.WriteNumberValue(value);
    }
}
