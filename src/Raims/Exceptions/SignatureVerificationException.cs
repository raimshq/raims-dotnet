namespace Raims
{
    /// <summary>The webhook signing headers, timestamp, secret or body could not be authenticated.</summary>
    public sealed class SignatureVerificationException : RaimsException
    {
        public SignatureVerificationException(string message) : base(message) { }
    }
}
