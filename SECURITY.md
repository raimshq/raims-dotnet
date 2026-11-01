# Security

To report a vulnerability, email `security@raims.com` with a minimal reproduction
and no credentials or personal data. Avoid disclosing exploitable details
before a coordinated fix.

Use the SDK on the server. Store keys and secrets in a vault and limit the
credential's permissions to the integration. Do not include credentials, request
bodies, personal documents, or temporary URLs in logs or issues.

Keep .NET, the operating system, and dependencies up to date. Do not disable TLS
or certificate verification. Custom transport must preserve the same limits on
redirects and credential transmission.

Validate webhook signatures against the original bytes. Check the environment
and event type, deduplicate durably, and commit the work before returning HTTP
acceptance.

After a write timeout or lost response, retrieve the resource and preserve the
intent's identity before deciding whether to retry.
