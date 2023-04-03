using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage> where T : class where TStateStorage : struct
{
    public delegate void SelectionChangeDelegate(T? oldSelection, T? newSelection, in TStateStorage state);

    // The currently selected leaf, if any.
    protected FileSystem<T>.Leaf? SelectedLeaf;

    // The currently selected value, if any.
    public T? Selected
        => SelectedLeaf?.Value;

    // Fired after the selected leaf changed.
    public event SelectionChangeDelegate? SelectionChanged;
    private FileSystem<T>.Leaf? _jumpToSelection = null;

    public void ClearSelection()
        => Select(null);

    protected void Select(FileSystem<T>.Leaf? leaf, in TStateStorage storage = default)
    {
        var oldV = SelectedLeaf?.Value;
        var newV = leaf?.Value;
        if (oldV == newV)
            return;

        SelectedLeaf = leaf;
        SelectionChanged?.Invoke(oldV, newV, storage);
    }

    protected readonly FileSystem<T> FileSystem;

    public virtual ISortMode<T> SortMode
        => ISortMode<T>.Lexicographical;

    // Used by Add and AddFolder buttons.
    private string _newName = string.Empty;

    private readonly string _label = string.Empty;

    public string Label
    {
        get => _label;
        init
        {
            _label    = value;
            MoveLabel = $"{value}Move";
        }
    }

    // Default color for tree expansion lines.
    protected virtual uint FolderLineColor
        => 0xFFFFFFFF;

    // Default color for folder names.
    protected virtual uint ExpandedFolderColor
        => 0xFFFFFFFF;

    protected virtual uint CollapsedFolderColor
        => 0xFFFFFFFF;

    // Whether all folders should be opened by default or closed.
    protected virtual bool FoldersDefaultOpen
        => false;

    public FileSystemSelector(FileSystem<T> fileSystem, KeyState keyState, string label = "##FileSystemSelector")
    {
        FileSystem = fileSystem;
        _state     = new List<StateStruct>(FileSystem.Root.TotalDescendants);
        _keyState  = keyState;
        Label      = label;

        InitDefaultContext();
        InitDefaultButtons();
        EnableFileSystemSubscription();
    }

    // Default flags to use for custom leaf nodes.
    protected const ImGuiTreeNodeFlags LeafFlags = ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.Bullet | ImGuiTreeNodeFlags.NoTreePushOnOpen;

    // Customization point: Should always create a tree node using LeafFlags (with possible selection.)
    // But can add additional icons or buttons if wanted.
    // Everything drawn in here is wrapped in a group.
    protected virtual void DrawLeafName(FileSystem<T>.Leaf leaf, in TStateStorage state, bool selected)
    {
        var       flag = selected ? ImGuiTreeNodeFlags.Selected | LeafFlags : LeafFlags;
        using var _    = ImRaii.TreeNode(leaf.Name, flag);
    }

    public void Draw(float width)
    {
        try
        {
            DrawPopups();
            using var group = ImRaii.Group();
            if (DrawList(width))
            {
                ImGui.PopStyleVar();
                if (width < 0)
                    width = ImGui.GetWindowWidth() - width;
                DrawButtons(width);
            }
        }
        catch (Exception e)
        {
            throw new Exception("Exception during FileSystemSelector rendering:\n"
              + $"{_currentIndex} Current Index\n"
              + $"{_currentDepth} Current Depth\n"
              + $"{_currentEnd} Current End\n"
              + $"{_state.Count} Current State Count\n"
              + $"{_filterDirty} Filter Dirty", e);
        }
    }

    // Select a specific leaf in the file system by its value.
    // If a corresponding leaf can be found, also expand its ancestors.
    public void SelectByValue(T value)
    {
        var leaf = FileSystem.Root.GetAllDescendants(ISortMode<T>.Lexicographical).OfType<FileSystem<T>.Leaf>()
            .FirstOrDefault(l => l.Value == value);
        if (leaf != null)
        {
            EnqueueFsAction(() =>
            {
                _filterDirty |= ExpandAncestors(leaf);
                Select(leaf, GetState(leaf));
                _jumpToSelection = leaf;
            });
        }
    }
}
