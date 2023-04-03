using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    // Add a button to the bottom-list. Should be an object that does not exceed the size parameter.
    // Buttons are sorted from left to right on priority, then subscription order.
    public void AddButton(Action<Vector2> action, int priority)
        => AddPrioritizedDelegate(_buttons, action, priority);

    // Remove a button from the bottom-list by reference equality.
    public void RemoveButton(Action<Vector2> action)
        => RemovePrioritizedDelegate(_buttons, action);

    // List sorted on priority, then subscription order.
    private readonly List<(Action<Vector2>, int)> _buttons = new(1);


    // Draw all subscribed buttons.
    private void DrawButtons(float width)
    {
        var buttonWidth = new Vector2(width / Math.Max(_buttons.Count, 1), ImGui.GetFrameHeight());
        using var style = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 0f)
            .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        foreach (var button in _buttons)
        {
            button.Item1.Invoke(buttonWidth);
            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    // Draw necessary popups from buttons outside of pushed styles.
    protected virtual void DrawPopups()
    {}

    // Protected so it can be removed.
    protected void FolderAddButton(Vector2 size)
    {
        const string newFolderName = "folderName";

        if (ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.FolderPlus.ToIconString(), size,
                "Create a new, empty folder. Can contain '/' to create a directory structure.", false, true))
            ImGui.OpenPopup(newFolderName);

        // Does not need to be delayed since it is not in the iteration itself.
        FileSystem<T>.Folder? folder = null;
        if (ImGuiUtil.OpenNameField(newFolderName, ref _newName) && _newName.Length > 0)
            try
            {
                folder = FileSystem.FindOrCreateAllFolders(_newName);
            }
            catch
            {
                // Ignored
            }

        if (folder != null)
            _filterDirty |= ExpandAncestors(folder);
    }

    private void InitDefaultButtons()
    {
        AddButton(FolderAddButton, 50);
    }
}
