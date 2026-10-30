using System;

namespace Raims
{
    /// <summary>Metadata about one HTTP attempt, without request parameters or credentials.</summary>
    public sealed class RequestDiagnostics
    {
        internal RequestDiagnostics(string operationId, string method, string path, int attempt, double durationMs, int? httpStatus, string? requestId, string? transportErrorCode, TimeSpan? retryDelay)
        { OperationId = operationId; Method = method; Path = path; Attempt = attempt; DurationMs = durationMs; HttpStatus = httpStatus; RequestId = requestId; TransportErrorCode = transportErrorCode; WillRetry = retryDelay.HasValue; RetryDelayMs = retryDelay?.TotalMilliseconds; }
        /// <summary>Published operation identifier.</summary>
        public string OperationId { get; }
        /// <summary>HTTP method.</summary>
        public string Method { get; }
        /// <summary>Path template, without resource identifiers or query parameters.</summary>
        public string Path { get; }
        /// <summary>Attempt number, starting at one.</summary>
        public int Attempt { get; }
        /// <summary>Attempt duration, excluding retry delays.</summary>
        public double DurationMs { get; }
        /// <summary>HTTP response status when available.</summary>
        public int? HttpStatus { get; }
        /// <summary>Request-Id returned by the server.</summary>
        public string? RequestId { get; }
        /// <summary>Transport failure category when no response was received.</summary>
        public string? TransportErrorCode { get; }
        /// <summary>Whether another automatic read attempt was scheduled.</summary>
        public bool WillRetry { get; }
        /// <summary>Scheduled delay before the next attempt.</summary>
        public double? RetryDelayMs { get; }
    }
}
