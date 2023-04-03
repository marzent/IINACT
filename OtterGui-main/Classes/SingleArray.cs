using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace OtterGui.Classes;

public readonly struct SingleArray<T> : IReadOnlyList<T> where T : notnull
{
    private readonly object? _value;

    public SingleArray()
        => _value = null;

    public SingleArray(params T[] values)
        => _value = values.Length switch
        {
            0 => null,
            1 => values[0],
            _ => values,
        };

    public SingleArray(IEnumerable<T> values)
        : this(values.ToArray())
    { }

    public SingleArray(T value)
        => _value = value;

    public int Count
        => _value switch
        {
            T     => 1,
            T[] l => l.Length,
            _     => 0,
        };

    public IEnumerator<T> GetEnumerator()
    {
        switch (_value)
        {
            case T v:
                yield return v;

                break;
            case T[] l:
            {
                foreach (var vl in l)
                    yield return vl;

                break;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
        => GetEnumerator();

    public T this[int index]
    {
        get
        {
            return _value switch
            {
                T v when index == 0 => v,
                T[] l               => l[index],
                _                   => throw new IndexOutOfRangeException(),
            };
        }
    }

    public SingleArray<T> Append(T value)
    {
        return _value switch
        {
            T v                   => new SingleArray<T>(v, value),
            T[] { Length: > 0 } l => new SingleArray<T>(l.Append(value)),
            _                     => new SingleArray<T>(value),
        };
    }

    public SingleArray<T> Prepend(T value)
    {
        return _value switch
        {
            T v                   => new SingleArray<T>(v, value),
            T[] { Length: > 0 } l => new SingleArray<T>(l.Prepend(value)),
            _                     => new SingleArray<T>(value),
        };
    }

    public SingleArray<T> Remove(T value)
        => Remove(v => ReferenceEquals(v, value));

    public SingleArray<T> Remove(Func<T, bool> predicate)
    {
        return _value switch
        {
            T v when predicate(v)       => new SingleArray<T>(),
            T[] l when l.Any(predicate) => new SingleArray<T>(l.Where(v => !predicate(v))),
            _                           => this,
        };
    }
}
