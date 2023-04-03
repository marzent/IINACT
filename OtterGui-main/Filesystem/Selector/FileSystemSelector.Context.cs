using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    // Add a right-click context menu item to folder context menus at the given priority.
    // Context menu items are sorted from top to bottom on priority, then subscription order.
    public void SubscribeRightClickFolder(Action<FileSystem<T>.Folder> action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsFolder, action, priority);

    // Add a right-click context menu item to leaf context menus at the given priority.
    // Context menu items are sorted from top to bottom on priority, then subscription order.
    public void SubscribeRightClickLeaf(Action<FileSystem<T>.Leaf> action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsLeaf, action, priority);

    // Add a right-click context menu item to the main context menu at the given priority.
    // Context menu items are sorted from top to bottom on priority, then subscription order.
    public void SubscribeRightClickMain(Action action, int priority = 0)
        => AddPrioritizedDelegate(_rightClickOptionsMain, action, priority);

    // Remove a right-click context menu item from the folder context menu by reference equality.
    public void UnsubscribeRightClickFolder(Action<FileSystem<T>.Folder> action)
        => RemovePrioritizedDelegate(_rightClickOptionsFolder, action);

    // Remove a right-click context menu item from the leaf context menu by reference equality.
    public void UnsubscribeRightClickLeaf(Action<FileSystem<T>.Leaf> action)
        => RemovePrioritizedDelegate(_rightClickOptionsLeaf, action);

    // Remove a right-click context menu item from the main context menu by reference equality.
    public void UnsubscribeRightClickMain(Action action)
        => RemovePrioritizedDelegate(_rightClickOptionsMain, action);

    // Draw all context menu items for folders.
    private void RightClickContext(FileSystem<T>.Folder folder)
    {
        using var _ = ImRaii.Popup(folder.Identifier.ToString());
        if (!_)
            return;

        foreach (var action in _rightClickOptionsFolder)
            action.Item1.Invoke(folder);
    }

    // Draw all context menu items for leaves.
    private void RightClickContext(FileSystem<T>.Leaf leaf)
    {
        using var _ = ImRaii.Popup(leaf.Identifier.ToString());
        if (!_)
            return;

        foreach (var action in _rightClickOptionsLeaf)
            action.Item1.Invoke(leaf);
    }

    // Draw all context menu items for the main context.
    private void RightClickMainContext()
    {
        foreach (var action in _rightClickOptionsMain)
            action.Item1.Invoke();
    }


    // Lists are sorted on priority, then subscription order.
    private readonly List<(Action<FileSystem<T>.Folder>, int)> _rightClickOptionsFolder = new(4);
    private readonly List<(Action<FileSystem<T>.Leaf>, int)>   _rightClickOptionsLeaf   = new(1);
    private readonly List<(Action, int)>                       _rightClickOptionsMain   = new(4);

    private void InitDefaultContext()
    {
        SubscribeRightClickFolder(DissolveFolder);
        SubscribeRightClickFolder(ExpandAllDescendants,   100);
        SubscribeRightClickFolder(CollapseAllDescendants, 100);
        SubscribeRightClickFolder(RenameFolder,           1000);
        SubscribeRightClickLeaf(RenameLeaf, 1000);
        SubscribeRightClickMain(ExpandAll,   1);
        SubscribeRightClickMain(CollapseAll, 1);
    }

    // Default entries for the folder context menu.
    // Protected so they can be removed by inheritors.
    protected void DissolveFolder(FileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Dissolve Folder"))
            _fsActions.Enqueue(() => FileSystem.Merge(folder, folder.Parent));
        ImGuiUtil.HoverTooltip("Remove this folder and move all its children to its parent-folder, if possible.");
    }

    protected void ExpandAllDescendants(FileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Expand All Descendants"))
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => ToggleDescendants(folder, idx, true));
        }

        ImGuiUtil.HoverTooltip("Successively expand all folders that descend from this folder, including itself.");
    }

    protected void CollapseAllDescendants(FileSystem<T>.Folder folder)
    {
        if (ImGui.MenuItem("Collapse All Descendants"))
        {
            var idx = _currentIndex;
            _fsActions.Enqueue(() => ToggleDescendants(folder, idx, false));
        }

        ImGuiUtil.HoverTooltip("Successively collapse all folders that descend from this folder, including itself.");
    }

    protected void RenameFolder(FileSystem<T>.Folder folder)
    {
        var currentPath = folder.FullName();
        if (ImGui.InputText("##Rename", ref currentPath, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            _fsActions.Enqueue(() =>
            {
                FileSystem.RenameAndMove(folder, currentPath);
                _filterDirty |= ExpandAncestors(folder);
            });

        ImGuiUtil.HoverTooltip("Enter a full path here to move or rename the folder. Creates all required parent directories, if possible.");
    }

    protected void SetQuickMove(FileSystem<T>.Folder folder, int which, string current, Action<string> onSelect)
    {
        if (ImGui.MenuItem($"Set as Quick Move Folder #{which + 1}"))
            onSelect(folder.FullName());
        ImGuiUtil.HoverTooltip($"Set this folder as a quick move location{(current.Length > 0 ? $"instead of {current}." : ".")}");
    }

    protected void ClearQuickMove(int which, string current, Action onSelect)
    {
        if (current.Length == 0)
            return;

        if (ImGui.MenuItem($"Clear Quick Move Folder #{which + 1}"))
            onSelect();
        ImGuiUtil.HoverTooltip($"Remove the current quick move folder {current}.");
    }

    protected void QuickMove(FileSystem<T>.Leaf leaf, params string[] folders)
    {
        var currentName = leaf.Name;
        var currentPath = leaf.FullName();
        foreach (var (folder, idx) in folders.WithIndex().Where(s => s.Item1.Length > 0))
        {
            var targetPath = $"{folder}/{currentName}";
            if (FileSystem.Equal(targetPath, currentPath))
                continue;

            if (ImGui.MenuItem($"Move to {folder}##QuickMove{idx}"))
                _fsActions.Enqueue(() =>
                {
                    FileSystem.RenameAndMove(leaf, targetPath);
                    _filterDirty |= ExpandAncestors(leaf);
                });
        }

        ImGuiUtil.HoverTooltip("Move the leaf to a previously set-up quick move location, if possible.");
    }

    protected void RenameLeaf(FileSystem<T>.Leaf leaf)
    {
        var currentPath = leaf.FullName();
        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere(0);
        if (ImGui.InputText("##Rename", ref currentPath, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            _fsActions.Enqueue(() =>
            {
                FileSystem.RenameAndMove(leaf, currentPath);
                _filterDirty |= ExpandAncestors(leaf);
            });
        ImGuiUtil.HoverTooltip("Enter a full path here to move or rename the leaf. Creates all required parent directories, if possible.");
    }

    protected void ExpandAll()
    {
        if (ImGui.Selectable("Expand All Directories"))
            _fsActions.Enqueue(() => ToggleDescendants(FileSystem.Root, -1, true));
    }

    protected void CollapseAll()
    {
        if (ImGui.Selectable("Collapse All Directories"))
            _fsActions.Enqueue(() =>
            {
                ToggleDescendants(FileSystem.Root, -1, false);
                AddDescendants(FileSystem.Root, -1);
            });
    }
}
