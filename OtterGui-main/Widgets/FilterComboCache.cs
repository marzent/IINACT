using System;
using System.Collections.Generic;
using ImGuiNET;
using OtterGui.Classes;

namespace OtterGui.Widgets;

/// <summary>
/// A wrapper around filterable combos that makes them work with temporary lists without taking permanent additional memory.
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class FilterComboCache<T> : FilterComboBase<T>
{
    public T? CurrentSelection { get; protected set; }

    private readonly ICachingList<T> _items;
    protected        int             CurrentSelectionIdx = -1;

    protected FilterComboCache(IEnumerable<T> items)
        : base(new TemporaryList<T>(items), false)
    {
        CurrentSelection = default;
        _items           = (ICachingList<T>)Items;
    }

    protected FilterComboCache(Func<IReadOnlyList<T>> generator)
        : base(new LazyList<T>(generator), false)
    {
        CurrentSelection = default;
        _items           = (ICachingList<T>)Items;
    }

    protected override void Cleanup()
        => _items.ClearList();


    protected override void DrawList(float width, float itemHeight)
    {
        base.DrawList(width, itemHeight);
        if (NewSelection != null && Items.Count > NewSelection.Value)
            CurrentSelection = Items[NewSelection.Value];
    }

    public bool Draw(string label, string preview, string tooltip, float previewWidth, float itemHeight, ImGuiComboFlags flags = ImGuiComboFlags.None)
        => Draw(label, preview, tooltip, ref CurrentSelectionIdx, previewWidth, itemHeight, flags);
}