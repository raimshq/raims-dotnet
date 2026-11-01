# Pagination

`List` and `ListAsync` methods return a typed page. Enumerating the current page
does not make additional HTTP calls:

```csharp
var page = await raims.Charges.ListAsync(new ListChargesOptions { PageSize = 20 });
foreach (var charge in page)
{
    Console.WriteLine(charge.Id);
}
```

`Page` indicates whether more results are available and provides the opaque
`NextAfter` and `PreviousBefore` cursors. Do not interpret or modify these values.

`NextPageAsync()` and `PreviousPageAsync()` load the next or previous page.
They return null when no further page exists. The methods retain the filters,
sort order, resource identity, and options of the original call.

```csharp
var next = await page.NextPageAsync();
var previous = await page.PreviousPageAsync();
```

To iterate through subsequent pages, use `GetAutoPagingEnumerable()` or its
asynchronous variant, `GetAutoPagingAsyncEnumerable()`. Loading happens during
enumeration and may make several requests. Limit the work in your application
and pass a `CancellationToken` when needed.

An empty page may contain a continuation cursor. The iterator follows it;
missing or repeated cursors with continuation enabled are protocol failures
that stop iteration.
