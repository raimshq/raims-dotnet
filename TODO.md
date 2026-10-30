# Próximas entregas

- [x] Disponibilizar os fontes de desenvolvimento em `raimshq/raims-dotnet`,
  na branch `main`, com instalação por checkout e pacote local documentadas.
- [x] Disponibilizar o pacote `Raims` no NuGet, com CI nos runners CodeBuild
  autorizados e publicação após uma release versionada do GitHub. A versão
  `0.1.3` foi instalada pelo registro público com cache vazio e validada em
  .NET Framework 4.8 e .NET 10, em Release/x64.
- [ ] Incluir o SDK .NET nas fontes aprovadas do atendimento em
  `raims-developer-docs` e `Raims-Chat/docs/knowledge-sources.md` quando a
  distribuição estiver disponível. A IA deve reconhecer a finalidade do SDK,
  quando sugeri-lo, instalação, requisitos, uso no servidor, obtenção da chave,
  permissões, ambiente, disponibilidade, paginação, idempotência, resultados
  pendentes e autenticação de webhooks. Atualizar a fonte responsável, regenerar
  os catálogos e validar a orientação. O SDK não exige nova ferramenta de
  consulta financeira nem mudança no contrato de contexto do chat.
- [ ] Publicar a nota do SDK .NET no changelog de `raims-developer-docs` na data
  da distribuição efetiva, com instalação e compatibilidade confirmadas.
