# Raims .NET

SDK C# para facilitar a integração com a API pública da Raims.

Instale pelo NuGet:

```powershell
dotnet add package Raims --version 0.1.5
```

Veja a [instalação](https://github.com/raimshq/raims-dotnet/blob/main/docs/installation.md)
para requisitos e orientações de uso e atualização.

A chave de API define a empresa, o ambiente e as permissões; a URL-base é
`https://api.raims.com`.

## Requisitos

Aplicação x64 em .NET Framework 4.8/4.8.1 ou .NET 10.

Use o SDK no servidor. Mantenha chaves de API, segredos de webhook, documentos
pessoais e links temporários fora do navegador, do controle de versão e dos logs.

## Primeiro uso

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

Reutilize uma instância do cliente durante a vida da integração para aproveitar
as conexões HTTP. Cada cliente mantém sua própria credencial e configuração.
Os exemplos assíncronos são trechos para métodos `async` da sua aplicação.

## Criar um cliente e uma cobrança

`customerTaxId` é o CPF ou CNPJ válido do cliente autorizado pela sua aplicação.
`customerIdempotencyKey` e `chargeIdempotencyKey` são chaves distintas, criadas
e persistidas antes das requisições. `chargeDueDate` é o vencimento escolhido e
persistido para o pedido, em segundos Unix UTC. Ao repetir uma intenção após
perder a resposta, conserve a chave e todos os parâmetros, inclusive o vencimento.

Com uma instância `raims` já configurada:

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
    Title = "Pedido 1234",
}, new RequestOptions { IdempotencyKey = chargeIdempotencyKey });

int? httpStatus = charge.LastResponse?.StatusCode;
string? requestId = charge.LastResponse?.RequestId;
```

`15000` representa R$ 150,00. Datas dos recursos usam segundos Unix UTC.
Uma resposta `202` indica resultado pendente e exige acompanhar o recurso;
ela não confirma pagamento ou conclusão financeira. O SDK não gera chaves de
idempotência nem repete escritas automaticamente.

## Serviços

| Propriedade | Recursos |
| --- | --- |
| `Accounts` | Dados da conta, tarifas e personalização do checkout |
| `Balances` | Saldo e previsão |
| `Customers` | Cadastro e consulta de clientes |
| `Charges` | Cobranças, pagamento, documentos e estornos por cobrança |
| `PaymentLinks` | Links de pagamento e solicitação de envio por e-mail |
| `Refunds` | Consulta de estornos e recibos |
| `Disputes` | Contestações |
| `Statements` | Extratos |
| `Withdrawals` | Saques, limites, resumo e configuração automática |
| `WithdrawalDestinations` | Destinos de saque |
| `Webhooks` | Cadastro, pausa, saúde, teste e rotação de segredo |
| `WebhookEventTypes` | Tipos de evento disponíveis |
| `Simulations.Charges` | Simulações de cobrança com chave Sandbox |
| `Simulations.Refunds` | Simulações de estorno com chave Sandbox |

Os métodos de recurso usam `Create`, `Get`, `Update` e `List`, com as variantes
`Async`. Consulte a [referência do SDK](https://github.com/raimshq/raims-dotnet/blob/main/docs/reference.md)
para os parâmetros e as respostas de cada serviço.

## Documentação

- [Instalação e compatibilidade](https://github.com/raimshq/raims-dotnet/blob/main/docs/installation.md)
- [Configuração, respostas e atualizações parciais](https://github.com/raimshq/raims-dotnet/blob/main/docs/usage.md)
- [Paginação](https://github.com/raimshq/raims-dotnet/blob/main/docs/pagination.md)
- [Erros, idempotência e retries](https://github.com/raimshq/raims-dotnet/blob/main/docs/errors-and-retries.md)
- [Recebimento e validação de webhooks](https://github.com/raimshq/raims-dotnet/blob/main/docs/webhooks.md)
- [Referência do SDK](https://github.com/raimshq/raims-dotnet/blob/main/docs/reference.md)
- [Manutenção](https://github.com/raimshq/raims-dotnet/blob/main/docs/maintenance.md)
- [Contribuição](https://github.com/raimshq/raims-dotnet/blob/main/CONTRIBUTING.md) e [segurança](https://github.com/raimshq/raims-dotnet/blob/main/SECURITY.md)
- [Documentação da API](https://docs.raims.com/br/api)

## Licença

Distribuído sob a licença [MIT](https://github.com/raimshq/raims-dotnet/blob/main/LICENSE).
