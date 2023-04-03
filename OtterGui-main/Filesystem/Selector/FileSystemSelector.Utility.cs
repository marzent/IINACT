using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Logging;
using ImGuiNET;
using OtterGui.Filesystem;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    private readonly KeyState _keyState;

    // Some actions should not be done during due to changed collections
    // or dependency on ImGui IDs.
    protected void EnqueueFsAction(Action action)
        => _fsActions.Enqueue(action);

    private readonly Queue<Action> _fsActions = new();

    // Execute all collected actions in the queue, called after creating the selector,
    // but before starting the draw iteration.
    private void HandleActions()
    {
        while (_fsActions.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                PluginLog.Warning(e.ToString());
            }
        }
    }

    // Used for buttons and context menu entries.
    private static void RemovePrioritizedDelegate<TDelegate>(List<(TDelegate, int)> list, TDelegate action) where TDelegate : Delegate
    {
        var idxAction = list.FindIndex(p => p.Item1 == action);
        if (idxAction >= 0)
            list.RemoveAt(idxAction);
    }

    // Used for buttons and context menu entries.
    private static void AddPrioritizedDelegate<TDelegate>(List<(TDelegate, int)> list, TDelegate action, int priority)
        where TDelegate : Delegate
    {
        var idxAction = list.FindIndex(p => p.Item1 == action);
        if (idxAction >= 0)
        {
            if (list[idxAction].Item2 == priority)
                return;

            list.RemoveAt(idxAction);
        }

        var idx = list.FindIndex(p => p.Item2 > priority);
        if (idx < 0)
            list.Add((action, priority));
        else
            list.Insert(idx, (action, priority));
    }

    // Set the expansion state of a specific folder and all its descendant folders to the given value.
    // Can only be executed from the main selector window due to ID computation, so use this only in Enqueued actions.
    // Handles ImGui-state as well as cache-state.
    private void ToggleDescendants(FileSystem<T>.Folder folder, int stateIdx, bool open)
    {
        SetFolderState(folder, open);
        RemoveDescendants(stateIdx);
        foreach (var child in folder.GetAllDescendants(ISortMode<T>.Lexicographical).OfType<FileSystem<T>.Folder>())
            SetFolderState(child, open);

        if (open)
            AddDescendants(folder, stateIdx);
    }

    // Expand all ancestors of a given path, used for when new objects are created.
    // Can only be executed from the main selector window due to ID computation.
    // Handles only ImGui-state.
    // Returns if any state was changed.
    private bool ExpandAncestors(FileSystem<T>.IPath path)
    {
        if (path.IsRoot || path.Parent.IsRoot)
            return false;

        var parent  = path.Parent;
        var changes = false;
        while (!parent.IsRoot)
        {
            changes |= !GetPathState(parent);
            SetFolderState(parent, true);
            parent = parent.Parent;
        }

        return changes;
    }

    private bool GetPathState(FileSystem<T>.IPath path)
        => _stateStorage.GetBool(ImGui.GetID((IntPtr)path.Identifier), FoldersDefaultOpen);

    private void SetFolderState(FileSystem<T>.Folder path, bool state)
        => _stateStorage.SetBool(ImGui.GetID((IntPtr)path.Identifier), state);
}
