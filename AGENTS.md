# Raims .NET SDK

Leia o README e os guias de contribuição e manutenção antes de alterar o SDK.
Este repositório consome somente o contrato público publicado e não importa
código dos hosts da Raims.

- Namespace público `Raims`; pacote NuGet `Raims`; destinos `netstandard2.0`
  e `net10.0`. Builds e solução usam Release/x64 conforme o workspace.
- O contrato fixado e sua origem ficam em `contracts`. Geração offline deve
  conferir o SHA-256 e cobrir cada operação publicada exatamente uma vez.
- Serviços, opções, modelos, constantes e referência derivados do contrato
  são gerados. Não editar esses arquivos manualmente.
- Preserve os nomes JSON, centavos inteiros, datas Unix, omissão e null.
  Não gerar idempotência nem repetir escritas automaticamente. Respostas
  `202` mantêm o significado de processamento pendente.
- Transporte autenticado não segue redirects, não baixa arquivos externos
  e não envia uploads. Credenciais são isoladas por cliente e requisição.
- Diagnósticos expõem somente método, caminho sem IDs, tentativa, duração,
  status e Request-Id. Nunca incluir credenciais, query ou corpos.
- Testes, simuladores, projetos consumidores de validação e evidências ficam
  exclusivamente em `C:\server\raims\.tmp\raims-dotnet-sdk`.
- Descreva os recursos da Raims diretamente nos textos do repositório.
  Preserve procedência e avisos obrigatórios de qualquer código incorporado.
- Deixe as alterações locais sem commit e sem conteúdo em staging.
  GitHub, NuGet, release e deploy exigem suas autorizações próprias.
- O workflow `nuget-release.yml` publica releases versionadas após validar o
  pacote nos runners CodeBuild autorizados. Não publicar a cada commit na main
  nem usar runners hospedados pelo GitHub ou computadores pessoais.
- O CI usa o validador canônico do Developer Toolkit em uma revisão fixada.
  A publicação consome somente o pacote e o manifesto aprovados, com OIDC
  restrito ao job de publicação e ao ambiente `release`.
