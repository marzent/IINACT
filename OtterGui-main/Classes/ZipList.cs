using System;
using System.Collections;
using System.Collections.Generic;

namespace OtterGui.Classes;

// A simple zipped list that combines two IReadOnlyLists at once.
public readonly struct ZipList<T1, T2> : IReadOnlyList<(T1, T2)>
{
    public readonly IList<T1> List1 = Array.Empty<T1>();
    public readonly IList<T2> List2 = Array.Empty<T2>();
    public          int       Count { get; } = 0;

    public ZipList(IList<T1> list1, IList<T2> list2)
    {
        List1 = list1;
        List2 = list2;
        Count = Math.Min(list1.Count, list2.Count);
    }

    public IEnumerator<(T1, T2)> GetEnumerator()
    {
        for (var i = 0; i < Count; ++i)
            yield return (List1[i], List2[i]);
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public (T1, T2) this[int index]
        => (List1[index], List2[index]);
}

public static class ZipList
{
    public static ZipList<T1, T2> FromSortedList<T1, T2>(SortedList<T1, T2> list) where T1 : notnull
        => new(list.Keys, list.Values);
}