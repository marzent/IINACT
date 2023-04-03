using System;
using System.Linq;
using System.Numerics;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Filesystem;
using OtterGui.Raii;

namespace OtterGui.FileSystem.Selector;

public partial class FileSystemSelector<T, TStateStorage>
{
    private ImGuiStoragePtr _stateStorage;
    private int             _currentDepth;
    private int             _currentIndex;
    private int             _currentEnd;
    private DateTimeOffset  _lastButtonTime = DateTimeOffset.UtcNow;

    private (Vector2, Vector2) DrawStateStruct(StateStruct state)
    {
        return state.Path switch
        {
            FileSystem<T>.Folder f => DrawFolder(f),
            FileSystem<T>.Leaf l   => DrawLeaf(l, state.StateStorage),
            _                      => (Vector2.Zero, Vector2.Zero),
        };
    }

    // Draw a leaf. Returns its item rectangle and manages
    //     - drag'n drop,
    //     - right-click context menu,
    //     - selection.
    private (Vector2, Vector2) DrawLeaf(FileSystem<T>.Leaf leaf, in TStateStorage state)
    {
        DrawLeafName(leaf, state, leaf == SelectedLeaf);
        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            Select(leaf, state);

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(leaf.Identifier.ToString());

        DragDropSource(leaf);
        DragDropTarget(leaf);
        RightClickContext(leaf);
        return (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
    }

    // Used for clipping. If we start with an object not on depth 0,
    // we need to add its indentation and the folder-lines for it.
    private void DrawPseudoFolders()
    {
        var first   = _state[_currentIndex]; // The first object drawn during this iteration
        var parents = first.Path.Parents();
        // Push IDs in order and indent.
        ImGui.Indent(ImGui.GetStyle().IndentSpacing * parents.Length);

        // Get start point for the lines (top of the selector).
        var lineStart = ImGui.GetCursorScreenPos();

        // For each pseudo-parent in reverse order draw its children as usual, starting from _currentIndex.
        for (_currentDepth = parents.Length; _currentDepth > 0; --_currentDepth)
        {
            DrawChildren(lineStart);
            lineStart.X -= ImGui.GetStyle().IndentSpacing;
            ImGui.Unindent();
        }
    }

    // Used for clipping. If we end not on depth 0 we need to check
    // whether to terminate the folder lines at that point or continue them to the end of the screen.
    private Vector2 AdjustedLineEnd(Vector2 lineEnd)
    {
        if (_currentIndex != _currentEnd)
            return lineEnd;

        var y = ImGui.GetWindowHeight() + ImGui.GetWindowPos().Y;
        if (y > lineEnd.Y + ImGui.GetTextLineHeight())
            return lineEnd;

        // Continue iterating from the current end.
        for (var idx = _currentEnd; idx < _state.Count; ++idx)
        {
            var state = _state[idx];

            // If we find an object at the same depth, the current folder continues
            // and the line has to go out of the screen.
            if (state.Depth == _currentDepth)
                return lineEnd with { Y = y };

            // If we find an object at a lower depth before reaching current depth,
            // the current folder stops and the line should stop at the last drawn child, too.
            if (state.Depth < _currentDepth)
                return lineEnd;
        }

        // All children are in subfolders of this one, but this folder has no further children on its own.
        return lineEnd;
    }

    // Draw children of a folder or pseudo-folder with a given line start using the current index and end.
    private void DrawChildren(Vector2 lineStart)
    {
        // Folder line stuff.
        var offsetX  = -ImGui.GetStyle().IndentSpacing + ImGui.GetTreeNodeToLabelSpacing() / 2;
        var drawList = ImGui.GetWindowDrawList();
        lineStart.X += offsetX;
        lineStart.Y -= 2 * ImGuiHelpers.GlobalScale;
        var lineEnd = lineStart;

        for (; _currentIndex < _currentEnd; ++_currentIndex)
        {
            // If we leave _currentDepth, its not a child of the current folder anymore.
            var state = _state[_currentIndex];
            if (state.Depth != _currentDepth)
                break;

            var lineSize = Math.Max(0, ImGui.GetStyle().IndentSpacing - 9 * ImGuiHelpers.GlobalScale);
            // Draw the child
            var (minRect, maxRect) = DrawStateStruct(state);
            if (minRect.X == 0)
                continue;

            // Draw the notch and increase the line length.
            var midPoint = (minRect.Y + maxRect.Y) / 2f - 1f;
            drawList.AddLine(lineStart with { Y = midPoint }, new Vector2(lineStart.X + lineSize, midPoint), FolderLineColor,
                ImGuiHelpers.GlobalScale);
            lineEnd.Y = midPoint;
        }

        // Finally, draw the folder line.
        drawList.AddLine(lineStart, AdjustedLineEnd(lineEnd), FolderLineColor, ImGuiHelpers.GlobalScale);
    }

    // Draw a folder. Handles
    //     - drag'n drop
    //     - right-click context menus
    //     - expanding/collapsing
    private (Vector2, Vector2) DrawFolder(FileSystem<T>.Folder folder)
    {
        var       flags = ImGuiTreeNodeFlags.NoTreePushOnOpen | (FoldersDefaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None);
        var       expandedState = GetPathState(folder);
        using var color = ImRaii.PushColor(ImGuiCol.Text, expandedState ? ExpandedFolderColor : CollapsedFolderColor);
        var       recurse = ImGui.TreeNodeEx((IntPtr)folder.Identifier, flags, folder.Name);

        if (expandedState != recurse)
            AddOrRemoveDescendants(folder, recurse);

        color.Pop();

        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
            ImGui.OpenPopup(folder.Identifier.ToString());
        DragDropSource(folder);
        DragDropTarget(folder);
        RightClickContext(folder);

        var rect = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());

        // If the folder is expanded, draw its children one tier deeper.
        if (!recurse)
            return rect;

        ++_currentDepth;
        ++_currentIndex;
        ImGui.Indent();
        DrawChildren(ImGui.GetCursorScreenPos());
        ImGui.Unindent();
        --_currentIndex;
        --_currentDepth;

        return rect;
    }

    // Open a collapse/expand context menu when right-clicking the selector without a selected item.
    private void MainContext()
    {
        const string mainContext = "MainContext";
        if (!ImGui.IsAnyItemHovered() && ImGui.IsMouseClicked(ImGuiMouseButton.Right) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
        {
            if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
                ImGui.SetWindowFocus(Label);
            ImGui.OpenPopup(mainContext);
        }

        using var pop = ImRaii.Popup(mainContext);
        if (!pop)
            return;

        RightClickMainContext();
    }


    // Draw the whole list.
    private bool DrawList(float width)
    {
        // Filter row is outside the child for scrolling.
        DrawFilterRow(width);

        using var style = ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        using var _     = ImRaii.Child(Label, new Vector2(width, -ImGui.GetFrameHeight()), true);
        style.Pop();
        MainContext();
        if (!_)
            return false;

        _stateStorage = ImGui.GetStateStorage();
        style.Push(ImGuiStyleVar.IndentSpacing, 14f * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.ItemSpacing,  new Vector2(ImGui.GetStyle().ItemSpacing.X, ImGuiHelpers.GlobalScale))
            .Push(ImGuiStyleVar.FramePadding, new Vector2(ImGuiHelpers.GlobalScale,       ImGui.GetStyle().FramePadding.Y));
        //// Check if filters are dirty and recompute them before the draw iteration if necessary.
        ApplyFilters();

        if (_jumpToSelection != null)
        {
            var idx = _state.FindIndex(s => s.Path == _jumpToSelection);
            if (idx >= 0)
            {
                ImGui.SetScrollFromPosY(ImGui.GetTextLineHeightWithSpacing() * idx - ImGui.GetScrollY());
            }

            _jumpToSelection = null;
        }

        ImGuiListClipperPtr clipper;
        unsafe
        {
            clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
        }

        // TODO: do this right.
        //HandleKeyNavigation();
        clipper.Begin(_state.Count, ImGui.GetTextLineHeightWithSpacing());
        // Draw the clipped list.

        while (clipper.Step())
        {
            _currentIndex = clipper.DisplayStart;
            _currentEnd   = Math.Min(_state.Count, clipper.DisplayEnd);
            if (_currentIndex >= _currentEnd)
                continue;

            if (_state[_currentIndex].Depth != 0)
                DrawPseudoFolders();
            _currentEnd = Math.Min(_state.Count, _currentEnd);
            for (; _currentIndex < _currentEnd; ++_currentIndex)
                DrawStateStruct(_state[_currentIndex]);
        }

        clipper.End();
        clipper.Destroy();

        //// Handle all queued actions at the end of the iteration.
        ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, Vector2.Zero);
        HandleActions();
        style.Push(ImGuiStyleVar.WindowPadding, Vector2.Zero);
        return true;
    }

    private void HandleKeyNavigation()
    {
        if (!ImGui.IsWindowFocused() || (DateTimeOffset.UtcNow - _lastButtonTime).Milliseconds < 250)
            return;

        var current = _state.FindIndex(s => s.Path == SelectedLeaf);
        if (current < 0)
            current = 0;

        var parent = SelectedLeaf?.Parent ?? FileSystem.Root;
        var shuffleAround = _state.WithIndex().Skip(current + 1)
            .Concat(_state.WithIndex().Take(current))
            .Where(s => s.Item1.Path.Parent == parent && s.Item1.Path is FileSystem<T>.Leaf)
            .Select(s => ((FileSystem<T>.Leaf)s.Item1.Path, s.Item2));
        FileSystem<T>.Leaf? next = null;
        var                 idx  = 0;
        if (VirtualKey.DOWN.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault();
        else if (VirtualKey.UP.IsPressed(_keyState))
            (next, idx) = shuffleAround.LastOrDefault();
        else if (VirtualKey.A.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'a' or 'A');
        else if (VirtualKey.B.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'b' or 'B');
        else if (VirtualKey.C.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'c' or 'C');
        else if (VirtualKey.D.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'd' or 'D');
        else if (VirtualKey.E.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'e' or 'E');
        else if (VirtualKey.F.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'f' or 'F');
        else if (VirtualKey.G.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'g' or 'G');
        else if (VirtualKey.H.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'h' or 'H');
        else if (VirtualKey.I.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'i' or 'I');
        else if (VirtualKey.J.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'j' or 'J');
        else if (VirtualKey.K.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'k' or 'K');
        else if (VirtualKey.L.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'l' or 'L');
        else if (VirtualKey.M.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'm' or 'M');
        else if (VirtualKey.N.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'n' or 'N');
        else if (VirtualKey.O.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'o' or 'O');
        else if (VirtualKey.P.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'p' or 'P');
        else if (VirtualKey.Q.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'q' or 'Q');
        else if (VirtualKey.R.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'r' or 'R');
        else if (VirtualKey.S.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 's' or 'S');
        else if (VirtualKey.T.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 't' or 'T');
        else if (VirtualKey.U.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'u' or 'U');
        else if (VirtualKey.V.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'v' or 'V');
        else if (VirtualKey.W.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'w' or 'W');
        else if (VirtualKey.X.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'x' or 'X');
        else if (VirtualKey.Y.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'y' or 'Y');
        else if (VirtualKey.Z.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is 'z' or 'Z');
        else if (VirtualKey.KEY_0.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '0');
        else if (VirtualKey.KEY_1.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '1');
        else if (VirtualKey.KEY_2.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '2');
        else if (VirtualKey.KEY_3.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '3');
        else if (VirtualKey.KEY_4.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '4');
        else if (VirtualKey.KEY_5.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '5');
        else if (VirtualKey.KEY_6.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '6');
        else if (VirtualKey.KEY_7.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '7');
        else if (VirtualKey.KEY_8.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '8');
        else if (VirtualKey.KEY_9.IsPressed(_keyState))
            (next, idx) = shuffleAround.FirstOrDefault(p => p.Item1.Name[0] is '9');

        if (next != null)
        {
            _lastButtonTime = DateTimeOffset.UtcNow;
            Select(next);
            var max = ImGui.GetScrollMaxY();
            if (max != 0)
            {
                var y = ImGui.GetScrollY();

                var offset = idx * ImGui.GetTextLineHeightWithSpacing();
                var space  = ImGui.GetContentRegionAvail().Y - ImGui.GetFrameHeight();
                if (idx > current) // Movement downwards
                {
                    if (y + ImGui.GetContentRegionAvail().Y < offset + ImGui.GetTextLineHeightWithSpacing())
                        ImGui.SetScrollY(Math.Clamp(offset - space / ImGui.GetTextLineHeightWithSpacing(), 0, max));
                }
                else if (y > offset) // Movement upwards
                {
                    ImGui.SetScrollY(Math.Clamp(offset, 0, max));
                }
            }
        }
    }
}
