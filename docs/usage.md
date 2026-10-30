# Configuração e respostas

## Configurar o cliente

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

`Timeout` limita cada tentativa, incluindo a leitura do corpo. Em .NET 10,
`ConnectTimeout` limita também a conexão. No destino Standard, usado pelo .NET
Framework, o transporte usa o prazo total e não oferece um prazo independente
para conectar. Retries de leitura podem somar tentativas e intervalos.

`RequestOptions` permite alterar a chave, o timeout, a idempotência e os retries
de uma única chamada. O cliente não armazena configuração global. A URL-base
aceita uma origem HTTPS; HTTP é permitido somente em loopback para desenvolvimento.

Todos os métodos aceitam `CancellationToken`. O cancelamento interrompe a
espera e os intervalos de retry; ele não prova que uma escrita deixou de ser
processada. Use as mesmas regras de reconciliação de uma conexão interrompida.

## Campos omitidos e null

As propriedades das opções usam `Optional<T>`. Uma propriedade que você não
definiu é omitida do JSON. Atribuir `null` envia null somente quando esse tipo
permite null; atribuir `false`, zero, texto vazio ou mapa vazio preserva o valor.

```csharp
var options = new UpdateChargeOptions
{
    Title = (string?)null,
};
await raims.Charges.UpdateAsync(chargeId, options);
```

O exemplo limpa o título e preserva os campos omitidos. A API define quais campos
podem ser limpos e em que estados. Em `metadata`, omitir ou enviar `{}` preserva
o mapa, null remove todos os pares e null em uma chave remove somente esse par.
Consulte a descrição específica da operação na [referência](reference.md).

Para voltar uma opção ao estado omitido, atribua `default` à propriedade.
Evite alterar as opções enquanto uma chamada estiver sendo preparada.

## Ler respostas

Os modelos oferecem propriedades tipadas com setters internos. Campos novos
recebidos da API ficam preservados em `ExtraFields`. Os códigos de enum são
primitivos e aceitam valores futuros; as constantes geradas ajudam a escrever
comparações com os valores publicados. Regras de negócio continuam na API.

`LastResponse` informa o status HTTP, os headers e o corpo recebido. Prefira
registrar somente `StatusCode` e `RequestId`; o corpo e os headers podem conter
dados pessoais, segredos de webhook ou links temporários. Não registre a
resposta completa.

DELETE de webhook retorna `EmptyResponse` com os metadados HTTP e status `204`.
Uma resposta `202` mantém os dados do recurso pendente e não inicia polling
automaticamente. Na revogação de cobrança, confira `Revoked` e
`RevocationPending`, além do status HTTP.

## Documentos e uploads

Métodos de recibo e boleto retornam `FinancialDocument`, com URL temporária e
expiração. Baixe a URL com um cliente HTTP separado, sem chave Bearer da Raims.
Após a expiração, consulte novamente o endpoint autenticado.

Os métodos de personalização retornam os dados para upload. O SDK não envia
o arquivo nem navega para URLs retornadas. Sua aplicação realiza o upload
conforme o contrato público, usando os dados retornados e sem a chave de API.

## Diagnósticos

`RequestObserver` recebe metadados de cada tentativa: operação, método, caminho
sem IDs, duração, status, `Request-Id` e decisão de retry. O callback não recebe
credenciais, parâmetros, corpo nem URL de recurso. Uma falha no observador não
altera o resultado da chamada.

Se o servidor retornar bytes que não formam texto UTF-8 válido, o SDK recusa a
resposta com `UnexpectedResponseException`. O diagnóstico conserva o status
HTTP e `Request-Id`, sem uma nova tentativa automática. A propriedade `Response`
da exceção conserva esses metadados; seu `Body` fica vazio porque o corpo não
pôde ser decodificado.

A aplicação consumidora deve agregar falhas e recuperações para seus alertas.
Não envie notificações por requisição nem use payloads como dados de telemetria.
O SDK não envia telemetria ou alertas externos.

## Transporte próprio

`RaimsClientOptions.HttpClient` aceita `IRaimsHttpClient`. A aplicação é
responsável por encerrar esse transporte e preservar TLS, prazos, cancelamento,
ausência de redirects e proteção de credenciais. Ao encerrar `RaimsClient`, o
SDK libera somente o transporte padrão que ele mesmo criou.
