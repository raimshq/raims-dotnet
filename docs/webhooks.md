# Receber webhooks

Use `WebhookUtility` para autenticar o corpo original antes de interpretar
o evento. O helper usa os headers `webhook-id`, `webhook-timestamp` e
`webhook-signature` do protocolo da Raims.

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
    // Valide o evento e grave o trabalho de forma durável antes do aceite HTTP.
}
catch (SignatureVerificationException)
{
    // Responda com falha HTTP e não processe o corpo.
}
```

`rawBodyBytes` é o array de bytes original recebido pelo servidor. Os valores
dos headers vêm da requisição; `webhookSecret` é o `secret_key` retornado ao
criar ou rotacionar o webhook. Ele é diferente da chave Bearer da API. Guarde-o
em um cofre; consultas posteriores do cadastro não retornam o segredo.

Não reserialize o JSON antes de autenticar. Espaços, Unicode e quebras de linha
participam da assinatura. O overload com string exige texto UTF-8 válido;
prefira os bytes originais quando seu servidor permitir obtê-los.

`ConstructEvent` valida a assinatura e exige um objeto JSON; retorna um
`JsonElement` independente. `VerifySignature` faz a mesma autenticação sem
interpretar o JSON. A validação de negócio continua na sua aplicação.

## Relógio e rotação

A tolerância padrão é de 300 segundos, tanto no passado quanto no futuro.
Mantenha o relógio sincronizado. Tolerância zero exige o mesmo segundo e não
desativa a proteção temporal; valores negativos são inválidos. O argumento
opcional `now` permite injetar um relógio para validação local.

Os nomes dos headers não diferenciam maiúsculas de minúsculas. Headers de
assinatura duplicados ou ambíguos são recusados. Durante a rotação, várias
assinaturas `v1=` podem aparecer separadas por vírgula; uma correspondência com
o segredo configurado autentica a entrega. Use `previous_secret_expires_at`
para coordenar a atualização dos servidores da sua aplicação.

## Processar uma entrega

Antes de responder com sucesso, confira tipo, ambiente e recurso do evento.
Deduplicate pelo `id` do corpo em armazenamento durável; `webhook-id` identifica
a entrega e não substitui o identificador do evento. Grave o trabalho de forma
atômica e processe o efeito com idempotência. Se a gravação falhar, devolva uma
falha HTTP para permitir nova entrega.

A assinatura válida não impede duplicidade dentro da tolerância. O campo
`created_at` do evento usa ISO 8601; ele é diferente do timestamp Unix do header.
Consulte o [guia de recebimento](https://docs.raims.com/br/integracoes/webhooks/receber-eventos).
