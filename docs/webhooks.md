# Receive webhooks

Use `WebhookUtility` to authenticate the original body before interpreting the
event. The helper uses the Raims protocol's `webhook-id`, `webhook-timestamp`,
and `webhook-signature` headers.

```csharp
using System.Collections.Generic;
using Raims;

var headers = new Dictionary<string, string>
{
    ["webhook-id"] = receivedDeliveryId,
    ["webhook-timestamp"] = receivedTimestamp,
    ["webhook-signature"] = receivedSignature,
};

try
{
    var webhookEvent = WebhookUtility.ConstructEvent(rawBodyBytes, headers, webhookSecret);
    // Validate the event and persist the work durably before HTTP acceptance.
}
catch (SignatureVerificationException)
{
    // Return an HTTP failure and do not process the body.
}
```

`rawBodyBytes` is the original byte array received by the server. Header values
come from the request; `webhookSecret` is the `secret_key` returned when creating
or rotating the webhook. It differs from the API Bearer key. Store it in a
vault; subsequent registration queries do not return the secret.

Do not reserialize JSON before authentication. Spaces, Unicode, and line breaks
participate in the signature. The string overload requires valid UTF-8 text;
prefer the original bytes when your server makes them available.

`ConstructEvent` validates the signature and requires a JSON object; it returns
an independent `JsonElement`. `VerifySignature` performs the same authentication
without parsing JSON. Business validation remains in your application.

## Clock and rotation

The default tolerance is 300 seconds, both in the past and in the future.
Keep the clock synchronized. Zero tolerance requires the same second and does
not disable time protection; negative values are invalid. The optional `now`
argument lets you inject a clock for local validation.

Header names are case-insensitive. Duplicate or ambiguous signature headers
are rejected. During rotation, several `v1=` signatures may appear separated
by commas; a match with the configured secret authenticates the delivery.
Use `previous_secret_expires_at` to coordinate updates to your application's
servers.

## Process a delivery

Before returning success, check the event's type, environment, and resource.
Deduplicate by the body's `id` in durable storage; `webhook-id` identifies the
delivery and does not replace the event identifier. Persist the work atomically
and process its effect idempotently. If persistence fails, return an HTTP
failure to allow another delivery.

A valid signature does not prevent duplicates within the tolerance window.
The event's `created_at` field uses ISO 8601; it differs from the header's Unix
timestamp. See the
[receiving guide](https://docs.raims.com/br/integracoes/webhooks/receber-eventos).
