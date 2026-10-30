using System.Text.Json;

namespace Raims
{
    internal static class JsonDefaults
    {
        internal static readonly JsonSerializerOptions Options = Create();
        private static JsonSerializerOptions Create()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = false, MaxDepth = 64 };
            options.Converters.Add(new ExactInt64Converter());
            options.Converters.Add(new ExactInt32Converter());
            options.Converters.Add(new RaimsCollectionConverter());
            return options;
        }
    }
}
