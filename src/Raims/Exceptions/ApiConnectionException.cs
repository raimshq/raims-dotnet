namespace Raims
{
    /// <summary>The HTTP response was not received. A write may already have been processed.</summary>
    public sealed class ApiConnectionException : RaimsException
    {
        /// <summary>Creates a sanitized transport failure.</summary>
        public ApiConnectionException(string message = "The API response was not received.", string? transportErrorCode = null) : base(message) { TransportErrorCode = transportErrorCode; }
        /// <summary>Transport failure category, when available.</summary>
        public string? TransportErrorCode { get; }
    }
}
