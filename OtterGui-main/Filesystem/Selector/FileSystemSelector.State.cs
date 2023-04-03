using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage> : IDisposable
{
    // The state storage can contain arbitrary data for each visible node.
    // It also contains the path of the visible node itself
    // as well as its depth in the file system tree.
    private struct StateStruct
    {
        public TStateStorage       StateStorage;
        public FileSystem<T>.IPath Path;
        public byte                Depth;
    }

    // Only contains values not filtered out at any time.
    private readonly List<StateStruct> _state;

    private FileSystem<T>.Leaf? _singleLeaf = null;
    private int                 _leafCount  = 0;

    public virtual void Dispose()
    {
        FileSystem.Changed -= OnFileSystemChange;
    }

    // The default filter string that is input.
    protected string FilterValue { get; private set; } = string.Empty;

    // If the filter was changed, recompute the state before the next draw iteration.
    private bool _filterDirty = true;

    public void SetFilterDirty()
        => _filterDirty = true;

    protected string FilterTooltip = string.Empty;

    // Customization point that gets triggered whenever FilterValue is changed.
    // Should return whether the filter is to be set dirty afterwards.
    protected virtual bool ChangeFilter(string filterValue)
        => true;

    private bool ChangeFilterInternal(string filterValue)
    {
        if (filterValue == FilterValue)
            return false;

        FilterValue = filterValue;
        return true;
    }

    // Customization point to draw additional filters into the filter row.
    // Parameters are start position for the filter input field and selector width.
    // It should return the remaining width for the text input.
    protected virtual float CustomFilters(float width)
        => width;

    // Draw the default filter row of a given width.
    private void DrawFilterRow(float width)
    {
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, Vector2.Zero).Push(ImGuiStyleVar.FrameRounding, 0);
        width = CustomFilters(width);
        ImGui.SetNextItemWidth(width);
        var tmp = FilterValue;
        if (ImGui.InputTextWithHint("##Filter", "Filter...", ref tmp, 128) && ChangeFilterInternal(tmp) && ChangeFilter(tmp))
            SetFilterDirty();
        style.Pop();
        if (FilterTooltip.Length > 0)
            ImGuiUtil.HoverTooltip(FilterTooltip);
    }

    // Customization point on how a path should be filtered.
    // Checks whether the FullName contains the current string by default.
    // Is not called directly, but through ApplyFiltersAndState, which can be overwritten separately.
    protected virtual bool ApplyFilters(FileSystem<T>.IPath path)
        => FilterValue.Length != 0 && !path.FullName().Contains(FilterValue);

    // Customization point to get the state associated with a given path.
    // Is not called directly, but through ApplyFiltersAndState, which can be overwritten separately.
    protected virtual TStateStorage GetState(FileSystem<T>.IPath path)
        => default;

    // If state and filtering are connected, you can overwrite this method.
    // Otherwise it just calls both functions separately.
    protected virtual bool ApplyFiltersAndState(FileSystem<T>.IPath path, out TStateStorage state)
    {
        state = GetState(path);
        return ApplyFilters(path);
    }

    // Recursively apply filters.
    // Folders are explored on their current expansion state as well as being filtered themselves.
    // But if any of a folders descendants is visible, the folder will also remain visible.
    private bool ApplyFiltersAddInternal(FileSystem<T>.IPath path, ref int idx, byte currentDepth)
    {
        var filtered = ApplyFiltersAndState(path, out var state);
        _state.Insert(idx, new StateStruct()
        {
            Depth        = currentDepth,
            Path         = path,
            StateStorage = state,
        });

        if (path is FileSystem<T>.Folder f)
        {
            if (GetPathState(f))
                foreach (var child in f.GetChildren(SortMode))
                {
                    ++idx;
                    filtered &= ApplyFiltersAddInternal(child, ref idx, (byte)(currentDepth + 1));
                }
            else
                filtered = ApplyFiltersScanInternal(path);
        }
        else if (!filtered && _leafCount++ == 0)
        {
            _singleLeaf = path as FileSystem<T>.Leaf;
        }

        // Remove a completely filtered folder again.
        if (filtered)
            _state.RemoveAt(idx--);

        return filtered;
    }

    // Scan for visible descendants of an uncollapsed folder.
    private bool ApplyFiltersScanInternal(FileSystem<T>.IPath path)
    {
        if (!ApplyFiltersAndState(path, out var state))
        {
            if (path is FileSystem<T>.Leaf l && _leafCount++ == 0)
                _singleLeaf = l;
            return false;
        }

        if (path is FileSystem<T>.Folder f)
            return f.GetChildren(ISortMode<T>.Lexicographical).All(ApplyFiltersScanInternal);


        return true;
    }

    // Non-recursive entry point for recreating filters if dirty.
    private void ApplyFilters()
    {
        if (!_filterDirty)
            return;

        _leafCount = 0;
        _state.Clear();
        var idx = 0;
        foreach (var child in FileSystem.Root.GetChildren(SortMode))
        {
            ApplyFiltersAddInternal(child, ref idx, 0);
            ++idx;
        }

        if (_leafCount == 1 && _singleLeaf! != SelectedLeaf)
        {
            _filterDirty = ExpandAncestors(_singleLeaf!);
            Select(_singleLeaf, GetState(_singleLeaf!));
        }
        else
        {
            _filterDirty = false;
        }
    }


    // Add or remove descendants of the given folder depending on if it is opened or closed.
    private void AddOrRemoveDescendants(FileSystem<T>.Folder folder, bool open)
    {
        if (open)
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => AddDescendants(folder, idx));
        }
        else
        {
            RemoveDescendants(_currentIndex);
        }
    }

    // Given the cache-index to a folder, remove its descendants from the cache.
    // Used when folders are collapsed.
    // ParentIndex == -1 indicates Root.
    private void RemoveDescendants(int parentIndex)
    {
        var start = parentIndex + 1;
        var depth = parentIndex < 0 ? -1 : _state[parentIndex].Depth;
        var end   = start;
        for (; end < _state.Count; ++end)
        {
            if (_state[end].Depth <= depth)
                break;
        }

        _state.RemoveRange(start, end - start);
        _currentEnd -= end - start;
    }

    // Given a folder and its cache-index, add all its expanded and unfiltered descendants to the cache.
    // Used when folders are expanded.
    // ParentIndex == -1 indicates Root.
    private void AddDescendants(FileSystem<T>.Folder f, int parentIndex)
    {
        var depth = (byte)(parentIndex == -1 ? 0 : _state[parentIndex].Depth + 1);
        foreach (var child in f.GetChildren(SortMode))
        {
            ++parentIndex;
            ApplyFiltersAddInternal(child, ref parentIndex, depth);
        }
    }

    // Any file system change also sets the filters dirty.
    // Easier than checking specific changes.
    private void EnableFileSystemSubscription()
        => FileSystem.Changed += OnFileSystemChange;

    private void OnFileSystemChange(FileSystemChangeType type, FileSystem<T>.IPath changedObject, FileSystem<T>.IPath? previousParent,
        FileSystem<T>.IPath? newParent)
    {
        switch (type)
        {
            case FileSystemChangeType.ObjectMoved:
                EnqueueFsAction(() =>
                {
                    ExpandAncestors(changedObject);
                    SetFilterDirty();
                });
                break;
            case FileSystemChangeType.ObjectRemoved when changedObject == SelectedLeaf:
            case FileSystemChangeType.Reload:
                ClearSelection();
                SetFilterDirty();
                break;
            default:
                SetFilterDirty();
                break;
        }
    }
}
