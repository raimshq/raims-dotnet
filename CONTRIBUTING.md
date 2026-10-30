# Contribuir

Leia o [guia de manutenção](docs/maintenance.md) e as instruções em `AGENTS.md`.
Relate problemas com uma reprodução mínima, sem credenciais, dados pessoais
ou URLs de acesso a recursos.

## Geração e compilação

O SDK usa .NET SDK 10 e Python 3.8 ou superior para a geração offline.
Na raiz do checkout:

```powershell
python scripts/generate.py --check
dotnet restore Raims.sln --locked-mode -p:Platform=x64
dotnet build Raims.sln -c Release -p:Platform=x64 --no-restore
```

Para alterar o contrato, siga o procedimento de manutenção e execute o gerador
sem `--check`. Não edite modelos, opções, serviços ou a referência gerados.
Revise todo o diff, inclusive arquivos novos e mudanças de nullability.

## Validação completa

```powershell
.\scripts\check.ps1
```

O comando chama o validador canônico do Developer Toolkit no checkout vizinho.
Ele materializa os consumidores e os testes somente em `.tmp`, confere o
contrato, compila a solução, executa os consumidores e inspeciona o pacote.
Os cenários usam transporte falso e loopback; não chamam a API real.

O gate também executa a instalação documentada com cache vazio, confere as
descrições no XML e na referência e usa Git para exportar uma árvore temporária
sem commit. O ZIP resultante precisa permitir geração, compilação e
empacotamento, com os links locais dos guias válidos.

Um clone isolado sem o Toolkit informa essa dependência ausente e não declara
uma validação parcial aprovada. Geração e build continuam disponíveis no SDK.

No workspace Raims, testes, simuladores, consumidores de validação, pacotes e
relatórios ficam exclusivamente em `C:\server\raims\.tmp\raims-dotnet-sdk`.
Não os inclua neste repositório ou em arquivos de distribuição. Encerre todos
os processos temporários e confira suas portas antes de concluir.

## Compatibilidade e distribuição

Preserve assinaturas públicas, tipos, nomes JSON, omissão, null, datas e valores
monetários. Não acrescente geração de idempotência ou retries de escrita.
O pacote deve conter assemblies e documentação para os dois destinos, licença
e guias de integração. Contratos de engenharia, gerador, TODO, testes e evidências
ficam fora do pacote NuGet.

O arquivo ZIP de fontes inclui os contratos, o gerador e os guias necessários
para gerar, compilar e empacotar a biblioteca. As regras de `export-ignore`
não podem remover um arquivo exigido pelo projeto ou pelos links dos guias.

Descreva os recursos da Raims diretamente nos textos e exemplos. Preserve
avisos obrigatórios de licença e procedência de código incorporado.

Os fontes de desenvolvimento ficam no GitHub. CI, release e NuGet são etapas
distintas dessa distribuição de fontes. A primeira release precisa usar os
runners CodeBuild autorizados e os gates canônicos do Toolkit, com revisão e
autorização específicas.
