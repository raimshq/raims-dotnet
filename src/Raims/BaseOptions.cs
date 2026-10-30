using System;
using System.Collections;
using System.Collections.Generic;

namespace Raims
{
    /// <summary>Base for typed request parameters.</summary>
    public abstract class BaseOptions
    {
        private readonly Dictionary<string, object?> values = new Dictionary<string, object?>(StringComparer.Ordinal);
        /// <summary>Reads an optional request field.</summary>
        protected Optional<T> Get<T>(string name) => values.TryGetValue(name, out var value) ? new Optional<T>((T)value!) : default;
        /// <summary>Sets or omits an optional request field.</summary>
        protected void Set<T>(string name, Optional<T> value)
        {
            if (value.IsSet) values[name] = value.Value;
            else values.Remove(name);
        }
        internal Dictionary<string, object?> ToParameters()
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var pair in values) result.Add(pair.Key, Snapshot(pair.Value));
            return result;
        }
        private static object? Snapshot(object? value)
        {
            if (value == null || value is string || value is bool || value is byte || value is sbyte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is decimal) return value;
            if (value is double d && !double.IsNaN(d) && !double.IsInfinity(d)) return value;
            if (value is float f && !float.IsNaN(f) && !float.IsInfinity(f)) return value;
            if (value is BaseOptions options) return options.ToParameters();
            if (value is IDictionary dictionary)
            {
                var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
                foreach (DictionaryEntry pair in dictionary)
                {
                    if (!(pair.Key is string key)) throw new ArgumentException("Request map keys must be strings.");
                    copy.Add(key, Snapshot(pair.Value));
                }
                return copy;
            }
            if (value is IEnumerable sequence)
            {
                var copy = new List<object?>();
                foreach (var item in sequence) copy.Add(Snapshot(item));
                return copy;
            }
            throw new ArgumentException("Request values must be JSON-compatible values.");
        }
        /// <summary>Returns the request type without exposing its values.</summary>
        public override string ToString() => GetType().Name;
    }
}
