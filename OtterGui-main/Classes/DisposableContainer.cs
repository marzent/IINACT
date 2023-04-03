using System;
using System.Collections.Generic;
using System.Linq;

namespace OtterGui.Classes;

public readonly struct DisposableContainer : IDisposable
{
    private readonly IReadOnlyList<IDisposable?> _disposables;

    public DisposableContainer()
        => _disposables = Array.Empty<IDisposable?>();

    public DisposableContainer(params IDisposable?[] disposables)
        => _disposables = disposables;

    public DisposableContainer(IEnumerable<IDisposable?> disposables)
        => _disposables = disposables.ToArray();

    public void Dispose()
    {
        foreach (var disposable in _disposables)
            disposable?.Dispose();
    }

    public static readonly DisposableContainer Empty = new();
}
