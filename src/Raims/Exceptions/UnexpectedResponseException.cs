namespace Raims
{
    /// <summary>The server response did not match the expected HTTP or JSON contract.</summary>
    public sealed class UnexpectedResponseException : RaimsException
    {
        /// <summary>Creates a protocol failure with optional response metadata.</summary>
        public UnexpectedResponseException(string message, ApiResponse? response = null) : base(message) { Response = response; }
        /// <summary>HTTP response metadata when received. Its body may contain sensitive data.</summary>
        /// <remarks>Body is empty when invalid UTF-8 prevents decoding; no replacement text is interpreted.</remarks>
        public ApiResponse? Response { get; }
    }
}
