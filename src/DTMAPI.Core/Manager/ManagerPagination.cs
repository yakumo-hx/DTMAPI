using System;

namespace DTMAPI.Core.Manager
{
    internal readonly struct ManagerPageWindow
    {
        internal ManagerPageWindow(int total, int pageSize, int pageIndex, int pageCount, int start, int end)
        {
            Total = total;
            PageSize = pageSize;
            PageIndex = pageIndex;
            PageCount = pageCount;
            Start = start;
            End = end;
        }

        internal int Total { get; }
        internal int PageSize { get; }
        internal int PageIndex { get; }
        internal int PageCount { get; }
        internal int Start { get; }
        internal int End { get; }
        internal bool IsEmpty => Total == 0;
    }

    internal static class ManagerPagination
    {
        internal static ManagerPageWindow Create(int total, int pageIndex, int pageSize)
        {
            total = Math.Max(0, total);
            pageSize = Math.Max(1, pageSize);
            int pageCount = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            pageIndex = Math.Max(0, Math.Min(pageIndex, pageCount - 1));
            int start = Math.Min(total, pageIndex * pageSize);
            int end = Math.Min(total, start + pageSize);
            return new ManagerPageWindow(total, pageSize, pageIndex, pageCount, start, end);
        }
    }
}
