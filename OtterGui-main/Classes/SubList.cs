using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OtterGui.Classes;

// Just a slice out of a list.
public readonly struct SubList<T> : IReadOnlyList<T>
{
    public static readonly SubList<T> Empty = new();

    public readonly IList<T> BaseList   = Array.Empty<T>();
    public readonly int      StartIndex = 0;
    public          int      Count { get; }

    public SubList(IList<T> list, int startIndex = 0)
    {
        BaseList   = list;
        StartIndex = Math.Clamp(startIndex, 0, list.Count);
        Count      = list.Count - startIndex;
    }

    public SubList(IList<T> list, int startIndex, int count)
    {
        BaseList   = list;
        StartIndex = Math.Clamp(startIndex, 0, list.Count);
        Count      = Math.Clamp(count,      0, list.Count - startIndex);
    }

    public T this[int i]
    {
        get
        {
            var start = i + StartIndex;
            var end   = Count + StartIndex;
            if (start > end)
                throw new IndexOutOfRangeException();

            return BaseList[start];
        }
        set
        {
            var start = i + StartIndex;
            var end   = Count + StartIndex;
            if (start > end)
                throw new IndexOutOfRangeException();

            BaseList[start] = value;
        }
    }

    public IEnumerator<T> GetEnumerator()
        => Count == 0       ? Enumerable.Empty<T>().GetEnumerator() :
            StartIndex == 0 ? BaseList.Take(Count).GetEnumerator() :
                              BaseList.Skip(StartIndex).Take(Count).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();
}
