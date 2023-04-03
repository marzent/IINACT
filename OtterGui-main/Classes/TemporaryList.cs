using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OtterGui.Classes;

/// <summary>
/// A IReadOnlyList based on any IEnumerable, that can clear its temporary storage and automatically initializes it when necessary.
/// </summary>
public class TemporaryList<T> : ICachingList<T>
{
    private          IReadOnlyList<T>? _list;
    private readonly IEnumerable<T>    _items;

    public TemporaryList(IEnumerable<T> items)
    {
        _items = items;
        if (_items is IReadOnlyList<T> l)
            _list = l;
    }

    public IEnumerator<T> GetEnumerator()
        => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public int Count
        => _list?.Count ?? (_items is ICollection<T> c ? c.Count : InitList().Count);

    private IReadOnlyList<T> InitList()
        => _list ??= _items.ToList();

    public void ClearList()
    {
        if (!ReferenceEquals(_list, _items))
            _list = null;
    }

    public T this[int index]
        => InitList()[index];
}