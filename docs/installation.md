# Instalação

Os fontes de desenvolvimento estão em
[raimshq/raims-dotnet](https://github.com/raimshq/raims-dotnet), na branch `main`.
O pacote público é [`Raims`](https://www.nuget.org/packages/Raims), com versões
para aplicações x64 em .NET Framework 4.8/4.8.1 e .NET 10.

## Instalar pelo NuGet

No diretório da aplicação consumidora:

```powershell
dotnet add package Raims --version 0.1.2
```

Mantenha a versão sob controle da aplicação. Revise as notas de cada release
antes de atualizar e teste a integração nos frameworks usados pelo seu projeto.
A versão do SDK é independente da versão HTTP `/v1` da API.

## Usar os fontes

Baixe os fontes e compile a solução com .NET SDK 10:

```powershell
git clone https://github.com/raimshq/raims-dotnet.git Raims-Dotnet
cd Raims-Dotnet
dotnet build Raims.sln -c Release -p:Platform=x64
```

Depois, adicione uma referência ao projeto a partir do diretório da sua aplicação:

```powershell
dotnet add reference ..\Raims-Dotnet\src\Raims\Raims.csproj
```

O exemplo considera a aplicação e o checkout em pastas vizinhas; ajuste o
caminho à sua organização. Compile a aplicação em x64. A solução do SDK usa
Release/x64 por padrão. Mantenha a revisão escolhida sob controle da sua
aplicação; a branch `main` pode receber novas versões de desenvolvimento.

## Usar um pacote local

Na raiz do checkout, gere o pacote em uma pasta local de artefatos:

```powershell
dotnet pack Raims.sln -c Release -p:Platform=x64 -o C:\server\raims\.tmp\raims-dotnet-sdk\packages
```

Acrescente a pasta do pacote e a fonte oficial ao `NuGet.Config` da aplicação,
preservando as demais configurações. Ajuste o caminho ao diretório em que o
pacote foi gerado. Para um projeto novo, o arquivo pode conter:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="raims-local" value="C:\server\raims\.tmp\raims-dotnet-sdk\packages" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

No diretório da aplicação consumidora, onde esse arquivo foi salvo:

```powershell
dotnet add package Raims --version 0.1.2 --no-restore
dotnet restore --configfile NuGet.Config
```

O primeiro comando adiciona a referência sem tentar baixar o pacote. O restore
usa as fontes do arquivo: a pasta local para o SDK e o NuGet oficial para suas
dependências. Mantenha as duas fontes, inclusive em máquinas com cache vazio.
Esses comandos não publicam o SDK.

## Compatibilidade

| Aplicação x64 | Destino selecionado pelo NuGet |
| --- | --- |
| .NET Framework 4.8 ou 4.8.1 no Windows | `netstandard2.0` |
| .NET 10 | `net10.0` |

Os consumidores de validação executam em .NET Framework 4.8 e .NET 10.
Outras implementações compatíveis com .NET Standard 2.0 podem resolver esse
destino; esse fato, por si só, não representa suporte validado a elas.

O SDK usa `System.Text.Json`. O destino Standard inclui também as interfaces
assíncronas compatíveis. Em aplicações .NET Framework, permita a geração dos
binding redirects e instale as atualizações do runtime e do sistema operacional.
Não desative a verificação de certificados para contornar erros de TLS.

Chaves Sandbox e Produção usam `https://api.raims.com`. A chave define o
ambiente, a empresa e as permissões; métodos de simulação exigem chave Sandbox.
