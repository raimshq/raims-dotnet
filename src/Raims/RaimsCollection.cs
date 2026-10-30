using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Raims
{
    internal interface IPagedCollection
    {
        void AttachPageLoader(Func<Dictionary<string, object?>, CancellationToken, Task<RaimsEntity>> loader, Dictionary<string, object?> parameters);
    }

    /// <summary>One resource page. Enumerating it does not request additional pages.</summary>
    public abstract class RaimsCollection<T> : RaimsEntity, IReadOnlyList<T>, IPagedCollection where T : RaimsEntity
    {
        private Func<Dictionary<string, object?>, CancellationToken, Task<RaimsEntity>>? loader;
        private Dictionary<string, object?> parameters = new Dictionary<string, object?>(StringComparer.Ordinal);
        /// <summary>Items returned in this page.</summary>
        [JsonInclude, JsonPropertyName("list")]
        public List<T> List { get; internal set; } = new List<T>();
        /// <summary>Opaque cursor navigation for this page.</summary>
        [JsonInclude, JsonPropertyName("page")]
        public Pagination? Page { get; internal set; }
        /// <summary>Number of items in the current page.</summary>
        [JsonIgnore]
        public int Count => List.Count;
        /// <summary>Item at the specified page position.</summary>
        public T this[int index] => List[index];
        /// <summary>Whether the current page is empty.</summary>
        [JsonIgnore]
        public bool IsEmpty => Count == 0;
        /// <summary>Returns the first item, or null for an empty page.</summary>
        public T? FirstOrDefault() => Count == 0 ? null : List[0];
        /// <summary>Returns the last item, or null for an empty page.</summary>
        public T? LastOrDefault() => Count == 0 ? null : List[Count - 1];
        /// <summary>Enumerates only this page.</summary>
        public IEnumerator<T> GetEnumerator() => List.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        void IPagedCollection.AttachPageLoader(Func<Dictionary<string, object?>, CancellationToken, Task<RaimsEntity>> value, Dictionary<string, object?> query)
        { loader = value; parameters = new Dictionary<string, object?>(query, StringComparer.Ordinal); }
        /// <summary>Fetches the following page, preserving original filters and request options.</summary>
        public Task<RaimsCollection<T>?> NextPageAsync(CancellationToken cancellationToken = default) => LoadPageAsync(true, cancellationToken);
        /// <summary>Fetches the preceding page, preserving original filters and request options.</summary>
        public Task<RaimsCollection<T>?> PreviousPageAsync(CancellationToken cancellationToken = default) => LoadPageAsync(false, cancellationToken);
        /// <summary>Fetches the following page synchronously.</summary>
        public RaimsCollection<T>? NextPage(CancellationToken cancellationToken = default) => NextPageAsync(cancellationToken).GetAwaiter().GetResult();
        /// <summary>Fetches the preceding page synchronously.</summary>
        public RaimsCollection<T>? PreviousPage(CancellationToken cancellationToken = default) => PreviousPageAsync(cancellationToken).GetAwaiter().GetResult();
        private async Task<RaimsCollection<T>?> LoadPageAsync(bool next, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Page == null || !(next ? Page.HasNext : Page.HasPrevious)) return null;
            var cursor = next ? Page.NextAfter : Page.PreviousBefore;
            if (string.IsNullOrEmpty(cursor)) throw new UnexpectedResponseException("Pagination indicates another page without a valid cursor.", LastResponse);
            if (loader == null) throw new InvalidOperationException("This page has no API page loader.");
            var query = new Dictionary<string, object?>(parameters, StringComparer.Ordinal);
            query.Remove(next ? "before" : "after");
            query[next ? "after" : "before"] = cursor;
            var result = await loader(query, cancellationToken).ConfigureAwait(false);
            if (!(result is RaimsCollection<T> page) || result.GetType() != GetType()) throw new UnexpectedResponseException("Pagination returned an unexpected resource type.", result.LastResponse);
            return page;
        }
        /// <summary>Lazily enumerates this page and all following pages, including empty continuation pages.</summary>
        public IEnumerable<T> GetAutoPagingEnumerable(CancellationToken cancellationToken = default)
        {
            RaimsCollection<T>? page = this;
            var seen = InitialCursors();
            while (page != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in page) { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
                if (page.Page == null || !page.Page.HasNext) yield break;
                CheckNextCursor(page, seen);
                page = page.NextPage(cancellationToken);
            }
        }
        /// <summary>Asynchronously enumerates this page and following pages, preserving filters and cancellation.</summary>
        public async IAsyncEnumerable<T> GetAutoPagingAsyncEnumerable([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            RaimsCollection<T>? page = this;
            var seen = InitialCursors();
            while (page != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                foreach (var item in page) { cancellationToken.ThrowIfCancellationRequested(); yield return item; }
                if (page.Page == null || !page.Page.HasNext) yield break;
                CheckNextCursor(page, seen);
                page = await page.NextPageAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        private HashSet<string> InitialCursors()
        {
            var seen = new HashSet<string>(StringComparer.Ordinal);
            if (parameters.TryGetValue("after", out var cursor) && cursor is string text) seen.Add(text);
            return seen;
        }
        private static void CheckNextCursor(RaimsCollection<T> page, HashSet<string> seen)
        {
            var cursor = page.Page!.NextAfter;
            if (string.IsNullOrEmpty(cursor) || !seen.Add(cursor!)) throw new UnexpectedResponseException("Pagination returned an invalid or repeated cursor.", page.LastResponse);
        }
    }
}
