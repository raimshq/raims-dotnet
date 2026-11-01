# Errors, idempotency, and retries

## Handle failures

Use the HTTP status and structured error code to decide what to do. API messages
are diagnostics in English and may change. Validation details identify the
field, code, and message when available.

HTTP, connection, protocol, and signature failures have their own types derived
from `RaimsException`. Argument errors use the usual .NET exceptions.
Application-requested cancellation preserves `OperationCanceledException`.

| Exception | Condition |
| --- | --- |
| `InvalidRequestException` | HTTP 400, 405, 413, 415, or 422 |
| `AuthenticationException` | HTTP 401 |
| `PermissionException` | HTTP 403 |
| `NotFoundException` | HTTP 404 |
| `ConflictException` | HTTP 409 |
| `RateLimitException` | HTTP 429 |
| `ApiException` | Other HTTP failures, including 5xx |
| `ApiConnectionException` | Interrupted connection or timeout |
| `UnexpectedResponseException` | Redirect, unexpected success, incompatible JSON or structure |

```csharp
try
{
    var customer = await raims.Customers.GetAsync(customerId);
}
catch (RateLimitException error)
{
    TimeSpan? wait = error.RetryAfter;
    // Reschedule the read according to your application's limits.
}
catch (ApiConnectionException)
{
    // No response was received. A write may have been processed.
}
catch (ApiErrorException error)
{
    string? code = error.ErrorCode;
    string? requestId = error.RequestId;
    // Decide what to do from the code without logging the full error.
}
```

Use the HTTP exception's properties to obtain the response and structured error
when available. An HTML response from a proxy may not contain this envelope.
Log only the status, code, and `Request-Id`; avoid logging bodies, credentials,
and complete headers.

## Idempotency

```csharp
var charge = await raims.Charges.CreateAsync(options, new RequestOptions
{
    IdempotencyKey = persistedKeyForThisOrder,
});
```

Create and persist the key before the call. It must identify one intent and
contain 1 to 255 visible ASCII characters without spaces. After losing a
response, keep the same key and parameters while that result can be reused.
Do not change the key to work around a failure.

The SDK sends the header only when you supply it to an endpoint that supports
idempotency. The [reference](reference.md) identifies these endpoints. Without a
key, the API behaves normally; the SDK does not generate one automatically.

A `202` response or interrupted connection does not confirm the financial
effect. `IDEMPOTENCY_REQUEST_IN_PROGRESS` requires waiting according to
`Retry-After`; `IDEMPOTENCY_RESULT_UNKNOWN` requires retrieving and reconciling
the resource before repeating the intent. See the
[API rules](https://docs.raims.com/br/api/idempotencia).

## Retries

The default is zero retries. Set `MaxNetworkRetries` from 1 to 5 to allow
additional attempts only for GET reads. Connection failures and HTTP 408, 429,
500, 502, 503, and 504 are eligible.

The delay uses backoff with jitter and respects valid `Retry-After` values up
to 30 seconds. If the server requires a longer wait, the SDK returns the failure
so the application can reschedule it. Each attempt has its own timeout; total
time may include several deadlines and delays.

POST, PATCH, and DELETE are not retried automatically, even with an idempotency
key. `202` responses do not start automatic polling.
