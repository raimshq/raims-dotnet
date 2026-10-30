# Erros, idempotência e retries

## Tratar falhas

Use o status HTTP e o código estruturado do erro para decidir a ação. Mensagens
da API são diagnósticos em inglês e podem mudar. Os detalhes de validação
identificam campo, código e mensagem quando disponíveis.

Falhas HTTP, de conexão, de protocolo e de assinatura têm tipos próprios que
derivam de `RaimsException`. Erros de argumentos usam as exceções usuais do .NET.
Cancelamentos solicitados pela aplicação preservam `OperationCanceledException`.

| Exceção | Condição |
| --- | --- |
| `InvalidRequestException` | HTTP 400, 405, 413, 415 ou 422 |
| `AuthenticationException` | HTTP 401 |
| `PermissionException` | HTTP 403 |
| `NotFoundException` | HTTP 404 |
| `ConflictException` | HTTP 409 |
| `RateLimitException` | HTTP 429 |
| `ApiException` | Demais falhas HTTP, incluindo 5xx |
| `ApiConnectionException` | Conexão interrompida ou timeout |
| `UnexpectedResponseException` | Redirect, sucesso inesperado, JSON ou estrutura incompatível |

```csharp
try
{
    var customer = await raims.Customers.GetAsync(customerId);
}
catch (RateLimitException error)
{
    TimeSpan? wait = error.RetryAfter;
    // Reagende a leitura conforme os limites da aplicação.
}
catch (ApiConnectionException)
{
    // A resposta não foi recebida. Uma escrita pode ter sido processada.
}
catch (ApiErrorException error)
{
    string? code = error.ErrorCode;
    string? requestId = error.RequestId;
    // Decida a ação pelo código, sem registrar o erro completo.
}
```

Consulte as propriedades da exceção HTTP para obter a resposta e o erro
estruturado, quando houver. Uma resposta HTML de um proxy pode não conter esse
envelope. Registre apenas o status, o código e `Request-Id`; evite registrar
corpos, credenciais e headers completos.

## Idempotência

```csharp
var charge = await raims.Charges.CreateAsync(options, new RequestOptions
{
    IdempotencyKey = persistedKeyForThisOrder,
});
```

Crie e persista a chave antes da chamada. Ela deve identificar uma intenção e
conter de 1 a 255 caracteres ASCII visíveis, sem espaços. Depois de perder a
resposta, conserve a mesma chave e os mesmos parâmetros enquanto aquele
resultado puder ser reutilizado. Não troque a chave para contornar uma falha.

O SDK envia o header somente quando você o informa em um endpoint que aceita
idempotência. A [referência](reference.md) identifica esses endpoints. Ausência
da chave segue o comportamento normal da API, sem geração automática.

Uma resposta `202` ou uma conexão interrompida não confirma o efeito financeiro.
`IDEMPOTENCY_REQUEST_IN_PROGRESS` exige aguardar conforme `Retry-After`;
`IDEMPOTENCY_RESULT_UNKNOWN` exige consultar e reconciliar o recurso antes de
repetir a intenção. Consulte as [regras da API](https://docs.raims.com/br/api/idempotencia).

## Retries

O padrão é zero retries. Configure `MaxNetworkRetries` entre 1 e 5 para permitir
tentativas adicionais somente em leituras GET. São elegíveis falhas de conexão
e HTTP 408, 429, 500, 502, 503 e 504.

O intervalo usa backoff com jitter e respeita `Retry-After` válido até 30 segundos.
Se o servidor exigir uma espera maior, o SDK devolve a falha para a aplicação
reagendar. Cada tentativa tem seu timeout; o tempo total pode incluir vários
prazos e intervalos.

POST, PATCH e DELETE não são repetidos automaticamente, mesmo quando uma chave
de idempotência está presente. Respostas `202` não iniciam polling automático.
