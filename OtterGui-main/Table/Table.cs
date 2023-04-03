using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Table;

public static class Table
{
    public const float ArrowWidth = 10;
}

public class Table<T>
{
    protected          bool           FilterDirty = true;
    protected          bool           SortDirty   = true;
    protected readonly ICollection<T> Items;
    internal readonly  List<(T, int)> FilteredItems;

    protected readonly string      Label;
    protected readonly Column<T>[] Headers;

    protected float ItemHeight  { get; set; }
    public    float ExtraHeight { get; set; } = 0;

    private int _currentIdx = 0;

    protected bool Sortable
    {
        get => Flags.HasFlag(ImGuiTableFlags.Sortable);
        set => Flags = value ? Flags | ImGuiTableFlags.Sortable : Flags & ~ImGuiTableFlags.Sortable;
    }

    protected int SortIdx = -1;

    public ImGuiTableFlags Flags = ImGuiTableFlags.RowBg
      | ImGuiTableFlags.Sortable
      | ImGuiTableFlags.BordersOuter
      | ImGuiTableFlags.ScrollY
      | ImGuiTableFlags.ScrollX
      | ImGuiTableFlags.PreciseWidths
      | ImGuiTableFlags.BordersInnerV
      | ImGuiTableFlags.NoBordersInBodyUntilResize;

    public int TotalItems
        => Items.Count;

    public int CurrentItems
        => FilteredItems.Count;

    public int TotalColumns
        => Headers.Length;

    public int VisibleColumns { get; private set; }

    public Table(string label, ICollection<T> items, params Column<T>[] headers)
    {
        Label          = label;
        Items          = items;
        Headers        = headers;
        FilteredItems  = new List<(T, int)>(Items.Count);
        VisibleColumns = Headers.Length;
    }

    public void Draw(float itemHeight)
    {
        ItemHeight = itemHeight;
        using var id = ImRaii.PushId(Label);
        UpdateFilter();
        DrawTableInternal();
    }

    public bool WouldBeVisible(T value)
        => Headers.All(header => header.FilterFunc(value));

    protected virtual void DrawFilters()
        => throw new NotImplementedException();

    protected virtual void PreDraw()
    { }

    private void SortInternal()
    {
        if (!Sortable)
            return;

        var sortSpecs = ImGui.TableGetSortSpecs();
        SortDirty |= sortSpecs.SpecsDirty;

        if (!SortDirty)
            return;

            SortIdx = sortSpecs.Specs.ColumnIndex;

            if (Headers.Length <= SortIdx)
                SortIdx = 0;

            if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Ascending)
                FilteredItems.StableSort((a, b) => Headers[SortIdx].Compare(a.Item1, b.Item1));
            else if (sortSpecs.Specs.SortDirection == ImGuiSortDirection.Descending)
                FilteredItems.StableSort((a, b) => Headers[SortIdx].CompareInv(a.Item1, b.Item1));
            else
                SortIdx = -1;
            SortDirty            = false;
            sortSpecs.SpecsDirty = false;
    }

    private void UpdateFilter()
    {
        if (!FilterDirty)
            return;

        FilteredItems.Clear();
        var idx = 0;
        foreach (var item in Items)
        {
            if (WouldBeVisible(item))
                FilteredItems.Add((item, idx));
            idx++;
        }

        FilterDirty = false;
        SortDirty   = true;
    }

    private void DrawItem((T, int) pair)
    {
        var       column = 0;
        using var id     = ImRaii.PushId(_currentIdx);
        _currentIdx = pair.Item2;
        foreach (var header in Headers)
        {
            id.Push(column++);
            if (ImGui.TableNextColumn())
                header.DrawColumn(pair.Item1, pair.Item2);
            id.Pop();
        }
    }

    private void DrawTableInternal()
    {
        using var table = ImRaii.Table("Table", Headers.Length, Flags,
            ImGui.GetContentRegionAvail() - ExtraHeight * Vector2.UnitY * ImGuiHelpers.GlobalScale);
        if (!table)
            return;

        PreDraw();
        ImGui.TableSetupScrollFreeze(1, 1);

        foreach (var header in Headers)
            ImGui.TableSetupColumn(header.Label, header.Flags, header.Width);

        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        var i = 0;
        VisibleColumns = 0;
        foreach (var header in Headers)
        {
            using var id = ImRaii.PushId(i);
            if (ImGui.TableGetColumnFlags(i).HasFlag(ImGuiTableColumnFlags.IsEnabled))
                ++VisibleColumns;
            if (!ImGui.TableSetColumnIndex(i++))
                continue;

            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
            ImGui.TableHeader(string.Empty);
            ImGui.SameLine();
            style.Pop();
            if (header.DrawFilter())
                FilterDirty = true;
        }

        SortInternal();
        _currentIdx = 0;
        ImGuiClip.ClippedDraw(FilteredItems, DrawItem, ItemHeight);
    }
}
