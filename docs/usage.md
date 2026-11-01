# Configuration and responses

## Configure the client

```csharp
using (var raims = new RaimsClient(new RaimsClientOptions
{
    ApiKey = apiKey,
    Timeout = TimeSpan.FromSeconds(80),
    ConnectTimeout = TimeSpan.FromSeconds(10),
    MaxNetworkRetries = 0,
}))
{
    var balance = await raims.Balances.GetAsync();
}
```

`Timeout` limits each attempt, including reading the body. On .NET 10,
`ConnectTimeout` also limits the connection. On the Standard target used by
.NET Framework, transport uses the overall deadline and does not provide a
separate connection deadline. Read retries may add multiple attempts and delays.

`RequestOptions` lets you override the key, timeout, idempotency, and retries for
a single call. The client does not store global configuration. The base URL
accepts an HTTPS origin; HTTP is allowed only on loopback for development.

All methods accept `CancellationToken`. Cancellation interrupts waits and retry
delays; it does not prove that a write was not processed. Apply the same
reconciliation rules as for an interrupted connection.

## Omitted fields and null

Option properties use `Optional<T>`. A property you do not set is omitted from
JSON. Assigning `null` sends null only when the type allows it; assigning `false`,
zero, empty text, or an empty map preserves that value.

```csharp
var options = new UpdateChargeOptions
{
    Title = (string?)null,
};
await raims.Charges.UpdateAsync(chargeId, options);
```

This example clears the title and preserves omitted fields. The API determines
which fields can be cleared and in which states. In `metadata`, omission or `{}`
preserves the map, null removes all pairs, and null for a key removes only that
pair. See the operation's specific description in the [reference](reference.md).

To reset an option to the omitted state, assign `default` to the property.
Avoid modifying options while a call is being prepared.

## Read responses

Models provide typed properties with internal setters. New fields received
from the API are preserved in `ExtraFields`. Enum codes are primitives that
accept future values; generated constants help compare against published
values. Business rules remain in the API.

`LastResponse` provides the HTTP status, headers, and received body. Prefer
logging only `StatusCode` and `RequestId`; the body and headers may contain
personal data, webhook secrets, or temporary links. Do not log the full response.

Webhook DELETE returns `EmptyResponse` with HTTP metadata and status `204`.
A `202` response retains the pending resource's data and does not start
automatic polling. When revoking a charge, check `Revoked` and
`RevocationPending` as well as the HTTP status.

## Documents and uploads

Receipt and boleto methods return `FinancialDocument`, with a temporary URL
and expiration. Download the URL with a separate HTTP client, without the Raims
Bearer key. After expiration, query the authenticated endpoint again.

Customization methods return upload details. The SDK does not upload the file
or navigate to returned URLs. Your application performs the upload according
to the public contract, using the returned details and without the API key.

## Diagnostics

`RequestObserver` receives metadata for each attempt: operation, method, path
without IDs, duration, status, `Request-Id`, and retry decision. The callback
does not receive credentials, parameters, bodies, or resource URLs. An observer
failure does not change the call's result.

If the server returns bytes that do not form valid UTF-8 text, the SDK rejects
the response with `UnexpectedResponseException`. Diagnostics retain the HTTP
status and `Request-Id`, without another automatic attempt. The exception's
`Response` property retains this metadata; its `Body` is empty because the body
could not be decoded.

The consuming application should aggregate failures and recoveries for alerts.
Do not send a notification per request or use payloads as telemetry data.
The SDK does not send external telemetry or alerts.

## Custom transport

`RaimsClientOptions.HttpClient` accepts `IRaimsHttpClient`. The application is
responsible for disposing this transport and preserving TLS, deadlines,
cancellation, redirect prevention, and credential protection. When
`RaimsClient` is disposed, the SDK releases only the default transport it created.
