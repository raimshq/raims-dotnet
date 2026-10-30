# Raims .NET

SDK C# para a API pública da Raims, com serviços por recurso, opções e respostas
tipadas, paginação por cursor e validação de webhooks.

O SDK cobre os 67 endpoints do contrato publicado, com métodos síncronos e
assíncronos. Os modelos mantêm os nomes JSON da API e os tipos no editor.

Os fontes de desenvolvimento estão disponíveis em
[raimshq/raims-dotnet](https://github.com/raimshq/raims-dotnet), na branch `main`.
Instale a versão `0.1.3` pelo NuGet:

```powershell
dotnet add package Raims --version 0.1.3
```

Veja a [instalação](docs/installation.md) para requisitos, atualização,
uso dos fontes e geração de um pacote local.

A chave de API define a empresa, o ambiente e as permissões; a URL-base é
`https://api.raims.com`.

## Requisitos

Aplicação x64 em .NET Framework 4.8/4.8.1 ou .NET 10. O pacote inclui os destinos
`netstandard2.0` e `net10.0`; as verificações de compatibilidade executam
consumidores em .NET Framework 4.8 e .NET 10. A versão Standard também pode ser
consumida por outras versões compatíveis, sem ampliar a matriz de suporte validada.

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
`Async`. A [referência gerada](docs/reference.md) lista cada operação, opções,
retorno, status de sucesso e suporte à idempotência.

## Documentação

- [Instalação e compatibilidade](docs/installation.md)
- [Configuração, respostas e atualizações parciais](docs/usage.md)
- [Paginação](docs/pagination.md)
- [Erros, idempotência e retries](docs/errors-and-retries.md)
- [Recebimento e validação de webhooks](docs/webhooks.md)
- [Referência dos endpoints](docs/reference.md)
- [Contrato e manutenção](docs/maintenance.md)
- [Contribuição](CONTRIBUTING.md) e [segurança](SECURITY.md)
- [Documentação da API](https://docs.raims.com/br/api)

## Licença

Distribuído sob a licença [MIT](LICENSE).
