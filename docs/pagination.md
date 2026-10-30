# Paginação

Os métodos `List` e `ListAsync` retornam uma página tipada. Enumerar a página
atual não faz novas chamadas HTTP:

```csharp
var page = await raims.Charges.ListAsync(new ListChargesOptions { PageSize = 20 });
foreach (var charge in page)
{
    Console.WriteLine(charge.Id);
}
```

As informações em `Page` indicam se existe continuação e os cursores opacos
`NextAfter` e `PreviousBefore`. Não interprete nem altere esses valores.

`NextPageAsync()` e `PreviousPageAsync()` carregam a página seguinte ou anterior.
Quando não há continuação, retornam null. Os métodos conservam os filtros, a
ordenação, a identidade do recurso e as opções da chamada original.

```csharp
var next = await page.NextPageAsync();
var previous = await page.PreviousPageAsync();
```

Para percorrer as próximas páginas, use `GetAutoPagingEnumerable()` ou a
variante assíncrona `GetAutoPagingAsyncEnumerable()`. O carregamento acontece
durante a enumeração e pode fazer várias requisições. Limite o trabalho na sua
aplicação e passe um `CancellationToken` quando necessário.

Uma página vazia pode conter um cursor de continuação. O iterador segue esse
cursor; cursores ausentes ou repetidos com continuação ativa são falhas de
protocolo e interrompem a leitura.
