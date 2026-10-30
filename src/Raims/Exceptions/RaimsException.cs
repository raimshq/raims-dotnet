using System;

namespace Raims
{
    /// <summary>Base exception for intentional SDK failures.</summary>
    public class RaimsException : Exception
    {
        /// <summary>Creates an SDK exception with a diagnostic message.</summary>
        public RaimsException(string message) : base(message) { }
    }
}
