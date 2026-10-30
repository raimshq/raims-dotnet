using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Raims
{
    internal sealed class RaimsCollectionConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert) => FindBase(typeToConvert) != null;
        public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            var itemType = FindBase(typeToConvert)!.GetGenericArguments()[0];
            return (JsonConverter)Activator.CreateInstance(typeof(CollectionConverter<,>).MakeGenericType(typeToConvert, itemType))!;
        }
        private static Type? FindBase(Type type)
        {
            for (var current = type; current != null; current = current.BaseType) if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(RaimsCollection<>)) return current;
            return null;
        }
        private sealed class CollectionConverter<TCollection, TItem> : JsonConverter<TCollection> where TCollection : RaimsCollection<TItem>, new() where TItem : RaimsEntity
        {
            public override TCollection Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                using (var document = JsonDocument.ParseValue(ref reader))
                {
                    var root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("list", out var items)) throw new JsonException("A resource page was expected.");
                    var result = new TCollection { List = JsonSerializer.Deserialize<List<TItem>>(items.GetRawText(), options) ?? throw new JsonException("A resource list was expected.") };
                    if (root.TryGetProperty("page", out var page)) result.Page = JsonSerializer.Deserialize<Pagination>(page.GetRawText(), options);
                    foreach (var property in root.EnumerateObject())
                    {
                        if (property.Name == "list" || property.Name == "page") continue;
                        if (result.ExtraFields == null) result.ExtraFields = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                        result.ExtraFields[property.Name] = property.Value.Clone();
                    }
                    return result;
                }
            }
            public override void Write(Utf8JsonWriter writer, TCollection value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("list");
                JsonSerializer.Serialize(writer, value.List, options);
                if (value.Page != null) { writer.WritePropertyName("page"); JsonSerializer.Serialize(writer, value.Page, options); }
                if (value.ExtraFields != null) foreach (var property in value.ExtraFields) { if (property.Key == "list" || property.Key == "page") continue; writer.WritePropertyName(property.Key); property.Value.WriteTo(writer); }
                writer.WriteEndObject();
            }
        }
    }
}
