using System;
using System.Collections.Generic;

namespace DTMAPI.DebugConsole
{
    internal struct CatalogPageAccumulator<T>
        where T : class
    {
        private readonly int requestedPage;
        private readonly int pageSize;
        private readonly List<T> requestedItems;
        private readonly T?[] tail;
        private int tailCount;
        private int tailNext;

        internal CatalogPageAccumulator(int page, int size)
        {
            tailCount = tailNext = 0;
            Page = TotalItems = 0;
            TotalPages = 1;
            requestedPage = Math.Max(0, page);
            pageSize = Math.Max(1, size);
            requestedItems = new List<T>(pageSize);
            tail = new T?[pageSize];
        }

        internal int Page { get; private set; }
        internal int TotalItems { get; private set; }
        internal int TotalPages { get; private set; }

        internal void Add(T item)
        {
            long index = TotalItems;
            TotalItems++;
            long requestedStart = (long)requestedPage * pageSize;
            if (index >= requestedStart &&
                requestedItems.Count < pageSize)
            {
                requestedItems.Add(item);
            }
            tail[tailNext] = item;
            tailNext = (tailNext + 1) % pageSize;
            if (tailCount < pageSize)
                tailCount++;
        }

        internal IReadOnlyList<T> Complete()
        {
            TotalPages = Math.Max(
                1,
                (int)Math.Ceiling(TotalItems / (double)pageSize));
            Page = Math.Min(requestedPage, TotalPages - 1);
            if (Page == requestedPage)
                return requestedItems;

            int resolvedCount = TotalItems - Page * pageSize;
            var resolved = new T[resolvedCount];
            int oldest = tailCount < pageSize ? 0 : tailNext;
            int skip = tailCount - resolvedCount;
            for (int index = 0; index < resolvedCount; index++)
            {
                resolved[index] = tail[
                    (oldest + skip + index) % pageSize]!;
            }
            return resolved;
        }
    }
}
