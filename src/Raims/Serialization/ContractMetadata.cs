using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;

namespace Raims
{
    internal static class ContractMetadata
    {
        private static readonly JsonElement document = Read();
        internal static JsonElement Operation(string name)
        {
            if (!document.GetProperty("operations").TryGetProperty(name, out var result)) throw new ArgumentException("Unknown public API operation.");
            return result;
        }
        internal static JsonElement Schema(string name) => document.GetProperty("schemas").GetProperty(name);
        private static JsonElement Read()
        {
            using (var stream = typeof(ContractMetadata).GetTypeInfo().Assembly.GetManifestResourceStream("Raims.Generated.contract.json"))
            {
                if (stream == null) throw new InvalidOperationException("The generated public contract is missing.");
                using (var parsed = JsonDocument.Parse(stream)) return parsed.RootElement.Clone();
            }
        }
        internal static bool IsValid(JsonElement value, JsonElement schema, int depth = 0)
        {
            if (depth > 64) return false;
            if (schema.TryGetProperty("$ref", out var reference))
            {
                var path = reference.GetString()!;
                if (!path.StartsWith("#/components/schemas/", StringComparison.Ordinal)) return false;
                return IsValid(value, Schema(path.Substring("#/components/schemas/".Length)), depth + 1);
            }
            if (schema.TryGetProperty("anyOf", out var alternatives))
            {
                foreach (var alternative in alternatives.EnumerateArray()) if (IsValid(value, alternative, depth + 1)) return true;
                return false;
            }
            if (schema.TryGetProperty("type", out var type))
            {
                var matches = false;
                if (type.ValueKind == JsonValueKind.Array) { foreach (var candidate in type.EnumerateArray()) if (MatchesType(value, candidate.GetString()!)) matches = true; }
                else matches = MatchesType(value, type.GetString()!);
                if (!matches) return false;
            }
            if (value.ValueKind == JsonValueKind.Null) return true;
            if (value.ValueKind == JsonValueKind.Number && schema.TryGetProperty("type", out var numberType) && (numberType.ValueKind == JsonValueKind.String && numberType.GetString() == "integer" || numberType.ValueKind == JsonValueKind.Array && ContainsString(numberType, "integer")))
            {
                if (!ExactInteger.TryInt64(value.GetRawText(), out var number)) return false;
                if (schema.TryGetProperty("format", out var format) && format.GetString() == "int32" && (number < int.MinValue || number > int.MaxValue)) return false;
            }
            if (value.ValueKind == JsonValueKind.Object)
            {
                if (schema.TryGetProperty("required", out var required)) foreach (var name in required.EnumerateArray()) if (!value.TryGetProperty(name.GetString()!, out _)) return false;
                schema.TryGetProperty("properties", out var properties);
                schema.TryGetProperty("additionalProperties", out var additional);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in value.EnumerateObject())
                {
                    if (!names.Add(property.Name)) return false;
                    if (properties.ValueKind == JsonValueKind.Object && properties.TryGetProperty(property.Name, out var child)) { if (!IsValid(property.Value, child, depth + 1)) return false; }
                    else if (additional.ValueKind == JsonValueKind.Object && !IsValid(property.Value, additional, depth + 1)) return false;
                }
            }
            if (value.ValueKind == JsonValueKind.Array && schema.TryGetProperty("items", out var items)) foreach (var item in value.EnumerateArray()) if (!IsValid(item, items, depth + 1)) return false;
            return true;
        }
        private static bool ContainsString(JsonElement values, string value) { foreach (var item in values.EnumerateArray()) if (item.GetString() == value) return true; return false; }
        private static bool MatchesType(JsonElement value, string type)
        {
            switch (type)
            {
                case "null": return value.ValueKind == JsonValueKind.Null;
                case "object": return value.ValueKind == JsonValueKind.Object;
                case "array": return value.ValueKind == JsonValueKind.Array;
                case "string": return value.ValueKind == JsonValueKind.String;
                case "boolean": return value.ValueKind == JsonValueKind.True || value.ValueKind == JsonValueKind.False;
                case "number": return value.ValueKind == JsonValueKind.Number;
                case "integer": return value.ValueKind == JsonValueKind.Number && ExactInteger.TryInt64(value.GetRawText(), out _);
                default: return false;
            }
        }
    }
}
