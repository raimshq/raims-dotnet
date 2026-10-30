using System;

namespace Raims
{
    /// <summary>A request value that distinguishes omission from an explicit value, including null.</summary>
    public readonly struct Optional<T>
    {
        private readonly T value;
        /// <summary>Creates an explicitly supplied request value.</summary>
        public Optional(T value) { this.value = value; IsSet = true; }
        /// <summary>Whether this value was explicitly supplied.</summary>
        public bool IsSet { get; }
        /// <summary>The supplied value.</summary>
        public T Value => IsSet ? value : throw new InvalidOperationException("The optional value was not supplied.");
        /// <summary>Converts a value into an explicitly supplied request value.</summary>
        public static implicit operator Optional<T>(T value) => new Optional<T>(value);
        /// <summary>Returns presence metadata without exposing the supplied value.</summary>
        public override string ToString() => IsSet ? "[supplied]" : "[omitted]";
    }
}
