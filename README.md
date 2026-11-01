# Raims .NET

C# SDK for integrating with the Raims public API.

Install from NuGet:

```powershell
dotnet add package Raims --version 0.1.5
```

See the [installation guide](https://github.com/raimshq/raims-dotnet/blob/main/docs/installation.md)
for requirements, usage, and upgrade instructions.

The API key determines the company, environment, and permissions. The base URL is
`https://api.raims.com`.

## Requirements

An x64 application running .NET Framework 4.8/4.8.1 or .NET 10.

Use the SDK on the server. Keep API keys, webhook secrets, personal documents,
and temporary links out of browsers, version control, and logs.

## Quick start

```csharp
using System;
using Raims;

string apiKey = Environment.GetEnvironmentVariable("RAIMS_API_KEY")
    ?? throw new InvalidOperationException("Configure RAIMS_API_KEY.");

using (var raims = new RaimsClient(apiKey))
{
    var customers = await raims.Customers.ListAsync(new ListCustomersOptions
    {
        PageSize = 20,
    });

    foreach (var customer in customers)
    {
        Console.WriteLine(customer.Id);
    }
}
```

Reuse a client instance for the lifetime of the integration to reuse HTTP
connections. Each client keeps its own credential and configuration.
Asynchronous examples are snippets for `async` methods in your application.

## Create a customer and a charge

`customerTaxId` is the valid CPF or CNPJ of the customer authorized by your
application. `customerIdempotencyKey` and `chargeIdempotencyKey` are separate
keys, created and persisted before the requests. `chargeDueDate` is the due date
chosen and persisted for the order, in Unix seconds UTC. When retrying the same
intent after losing a response, keep the key and all parameters, including the
due date.

With a configured `raims` instance:

```csharp
var customer = await raims.Customers.CreateAsync(new CreateCustomerOptions
{
    TaxId = customerTaxId,
}, new RequestOptions { IdempotencyKey = customerIdempotencyKey });

var charge = await raims.Charges.CreateAsync(new CreateChargeOptions
{
    CustomerId = customer.Id,
    AmountCents = 15000,
    DueDate = chargeDueDate,
    Title = "Order 1234",
}, new RequestOptions { IdempotencyKey = chargeIdempotencyKey });

int? httpStatus = charge.LastResponse?.StatusCode;
string? requestId = charge.LastResponse?.RequestId;
```

`15000` represents BRL 150.00. Resource dates use Unix seconds UTC.
A `202` response indicates a pending result that must be tracked through the
resource; it does not confirm payment or financial completion. The SDK does not
generate idempotency keys or retry writes automatically.

## Services

| Property | Resources |
| --- | --- |
| `Accounts` | Account details, fees, and checkout customization |
| `Balances` | Balance and forecast |
| `Customers` | Customer creation and retrieval |
| `Charges` | Charges, payment, documents, and refunds for a charge |
| `PaymentLinks` | Payment links and email delivery requests |
| `Refunds` | Refund retrieval and receipts |
| `Disputes` | Disputes |
| `Statements` | Statements |
| `Withdrawals` | Withdrawals, limits, summary, and automatic configuration |
| `WithdrawalDestinations` | Withdrawal destinations |
| `Webhooks` | Registration, pause, health, testing, and secret rotation |
| `WebhookEventTypes` | Available event types |
| `Simulations.Charges` | Charge simulations with a Sandbox key |
| `Simulations.Refunds` | Refund simulations with a Sandbox key |

Resource methods use `Create`, `Get`, `Update`, and `List`, with `Async`
variants. See the [SDK reference](https://github.com/raimshq/raims-dotnet/blob/main/docs/reference.md)
for each service's parameters and responses.

## Documentation

- [Installation and compatibility](https://github.com/raimshq/raims-dotnet/blob/main/docs/installation.md)
- [Configuration, responses, and partial updates](https://github.com/raimshq/raims-dotnet/blob/main/docs/usage.md)
- [Pagination](https://github.com/raimshq/raims-dotnet/blob/main/docs/pagination.md)
- [Errors, idempotency, and retries](https://github.com/raimshq/raims-dotnet/blob/main/docs/errors-and-retries.md)
- [Receiving and validating webhooks](https://github.com/raimshq/raims-dotnet/blob/main/docs/webhooks.md)
- [SDK reference](https://github.com/raimshq/raims-dotnet/blob/main/docs/reference.md)
- [Maintenance](https://github.com/raimshq/raims-dotnet/blob/main/docs/maintenance.md)
- [Contributing](https://github.com/raimshq/raims-dotnet/blob/main/CONTRIBUTING.md) and [security](https://github.com/raimshq/raims-dotnet/blob/main/SECURITY.md)
- [API documentation](https://docs.raims.com/br/api)

## License

Distributed under the [MIT license](https://github.com/raimshq/raims-dotnet/blob/main/LICENSE).
