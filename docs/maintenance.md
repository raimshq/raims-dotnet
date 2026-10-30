# Contrato e manutenção

## Fonte

O SDK consome o contrato público distribuído em
[raimshq/openapi](https://github.com/raimshq/openapi). `contracts/source.json`
registra a revisão imutável, URL, SHA-256, versão da API e quantidade de operações.
O snapshot inicial fixa 67 operações da revisão
`a412f1f2d12edec7edf001bf2fb4b48403fcec97`.

O gerador é offline: confere o hash e não baixa contratos, chama a API ou lê
credenciais. Não copie endpoints presentes somente no código local da API.
O espelho publicado continua sendo o limite da superfície distribuída.

## Organização

| Arquivo ou diretório | Responsabilidade |
| --- | --- |
| `src/Raims/RaimsClient.cs` | Composição dos serviços e ciclo de vida do cliente |
| `src/Raims/Services` | Métodos por recurso; wrapper de simulações mantido manualmente |
| `src/Raims/Options` | Entradas tipadas com distinção entre omissão e null |
| `src/Raims/Models` | Respostas e objetos aninhados |
| `src/Raims/Generated` | Catálogo HTTP, mapa de símbolos e constantes |
| `src/Raims/ApiRequestor.cs` | Serialização, validação estrutural, retries e erros |
| `src/Raims/Http` | Transporte HTTP, cancelamento e timeout |
| `src/Raims/Serialization` | Metadados, coleções e leitura exata de inteiros |
| `src/Raims/RaimsCollection.cs` | Paginação por cursor |
| `src/Raims/Exceptions` | Categorias de falha e metadados |
| `src/Raims/WebhookUtility.cs` | Autenticação dos bytes de uma entrega |
| `scripts/generate.py` | Geração determinística |
| `docs/reference.md` | Assinaturas e campos derivados do contrato |

Todos os tipos públicos usam o namespace `Raims`. As opções e os modelos são
gerados; `RaimsClient`, `BaseService`, o wrapper `SimulationsService` e o runtime
são mantidos manualmente. O catálogo de serviços identifica cada operação
exatamente uma vez, sem fallback para endpoints arbitrários.

O SDK usa classes de opções por operação e `Optional<T>` em cada campo.
As respostas preservam campos desconhecidos e permitem códigos de enum futuros.
A validação estrutural exige tipos, campos obrigatórios e nullability, sem
duplicar permissões, regras financeiras ou toda a validação de domínio da API.
Valores monetários e datas usam inteiros; o leitor não converte esses valores
por ponto flutuante.

## Atualizar o contrato

1. Selecione uma revisão pública aprovada e confira seu manifesto e checksum.
2. Atualize o snapshot e `contracts/source.json` sem inventar rotas ou campos.
3. Revise semântica, status, idempotência, campos, ambiente e paginação.
4. Ajuste `contracts/service-map.json` se houver novas operações e execute
   `python scripts/generate.py`.
5. Revise assinaturas, tipos, constantes e a referência; execute a validação
   completa do [guia de contribuição](../CONTRIBUTING.md).
6. Atualize os guias afetados e registre mudanças de compatibilidade antes
   da distribuição. Uma mudança incompatível exige versão principal.

`--check` compara os arquivos esperados sem gravá-los. O gerador recusa hash
divergente, operação ausente ou duplicada e composições sem representação
implementada. Não atualize uma base de compatibilidade para esconder uma quebra.

O projeto habilita a validação de pacote do .NET para conferir os destinos
incluídos. Após a primeira release estável, estabeleça a versão publicada como
baseline dessa validação antes de uma nova distribuição.

## Preparar uma versão

A versão do pacote em `src/Raims/Raims.csproj` e a constante
`RaimsClient.Version` precisam ter o mesmo valor. Atualize também os exemplos
de instalação para a versão que será distribuída. O nome do pacote continua
definido em `PackageId`; a versão não altera a versão HTTP `/v1` da API.

Antes de distribuir, confira a versão, os metadados e o pacote resultante,
execute os gates de entrega e valide sua instalação nos destinos suportados.
Use os runners CodeBuild autorizados para o fluxo de release. A existência de
uma release no GitHub, por si só, não confirma a disponibilidade no NuGet;
confira o resultado da publicação e a instalação da versão no registro público.

## Diagnóstico e disponibilidade

As exceções de conexão, HTTP e protocolo e o observador de tentativas permitem
à aplicação detectar indisponibilidade e respostas inválidas. Agregue essas
falhas e confirme recuperação por novas respostas válidas. Não repita uma
escrita anterior para verificar a saúde e não envie telemetria com dados pessoais.

Os fontes estão no GitHub, e o pacote `Raims` está disponível no
[NuGet](https://www.nuget.org/packages/Raims). O
[guia de integração](https://docs.raims.com/br/integracoes/sdk-dotnet) e a
[nota de publicação](https://docs.raims.com/br/changelog/post/2026-10-29-sdk-dotnet)
estão na documentação da Raims. O guia também integra o catálogo aprovado do
atendimento.
O atendimento deve distinguir um checkout de desenvolvimento de uma versão
disponível no gerenciador de pacotes.
