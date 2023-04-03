using System;
using System.Collections;
using System.Collections.Generic;

namespace OtterGui.Classes;

public interface ICachingList<out T> : IReadOnlyList<T>
{
    public void ClearList();
}


/// <summary> A ReadOnlyList using a generator on access and caching the result until ClearList is called. </summary>
public class LazyList<T> : ICachingList<T>
{
    private          IReadOnlyList<T>?      _list;
    private readonly Func<IReadOnlyList<T>> _generator;

    public LazyList(Func<IReadOnlyList<T>> generator)
        => _generator = generator;

    public IEnumerator<T> GetEnumerator()
        => InitList().GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => InitList().Count;

    private IReadOnlyList<T> InitList()
        => _list ??= _generator();

    public void ClearList()
        => _list = null;

    public T this[int index]
        => InitList()[index];
}
