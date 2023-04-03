using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

/// <summary>
/// Class to draw and edit a list of tags.
/// </summary>
public class TagButtons
{
    private string _currentTag = string.Empty;
    private int    _editIdx    = -1;
    private bool   _setFocus;

    /// <summary>
    /// Draw the list of tags.
    /// </summary>
    /// <param name="label">A text entry displayed before the list and used as ID. The line-broken list is wrapped at the end of this text.</param>
    /// <param name="description">Optional description displayed when hovering over a help marker before the label (if the description is not empty.)</param>
    /// <param name="tags">The list of tags.</param>
    /// <param name="editedTag">If the return value is greater or equal to 0, the user input for the tag given by the index.</param>
    /// <param name="editable">Controls if the buttons can be used to edit their tags and if new tags can be added, also controls the background color.</param>
    /// <param name="xOffset">An optional offset that is added after the tag as the text wrap point.</param>
    /// <returns>-1 if no change took place yet, the index of an edited tag (or the count of <paramref name="tags"/> for an added one) if an edit was finalized.</returns>
    public int Draw(string label, string description, IReadOnlyCollection<string> tags, out string editedTag, bool editable = true, float xOffset = 0)
    {
        using var id  = ImRaii.PushId(label);
        var       ret = -1;

        using var group = ImRaii.Group();
        AlignedHelpMarker(description);
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(label);
        ImGui.SameLine();

        var x = ImGui.GetCursorPosX() + xOffset;
        ImGui.SetCursorPosX(x);
        editedTag = string.Empty;
        var       color = ImGui.GetColorU32(editable ? ImGuiCol.Button : ImGuiCol.FrameBg);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { X = 4 * ImGuiHelpers.GlobalScale });
        using var c = ImRaii.PushColor(ImGuiCol.ButtonHovered, color, !editable)
            .Push(ImGuiCol.ButtonActive, color, !editable)
            .Push(ImGuiCol.Button,       color);
        foreach (var (tag, idx) in tags.WithIndex())
        {
            using var id2 = ImRaii.PushId(idx);
            if (_editIdx == idx)
            {
                var width = SetPosText(_currentTag, x);
                SetFocus();
                ret = InputString(width, tag, out editedTag);
            }
            else
            {
                SetPosButton(tag, x);
                Button(tag, idx, editable);

                if (editable)
                {
                    var delete = ImGui.GetIO().KeyCtrl && ImGui.IsItemClicked(ImGuiMouseButton.Right);
                    ImGuiUtil.HoverTooltip("Hold CTRL and right-click to delete.");
                    if (delete)
                    {
                        editedTag = string.Empty;
                        ret       = idx;
                    }
                }
            }
            ImGui.SameLine();
        }

        if (!editable)
            return -1;

        if (_editIdx == tags.Count)
        {
            var width = SetPosText(_currentTag, x);
            SetFocus();
            ret = InputString(width, string.Empty, out editedTag);
        }
        else
        {
            SetPos(ImGui.GetFrameHeight(), x);
            if (!ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.Plus.ToIconString(), new Vector2(ImGui.GetFrameHeight()), "Add Tag...",
                    false, true))
                return ret;

            _editIdx    = tags.Count;
            _setFocus   = true;
            _currentTag = string.Empty;
        }

        return ret;
    }

    private void SetFocus()
    {
        if (!_setFocus)
            return;

        ImGui.SetKeyboardFocusHere();
        _setFocus = false;
    }

    private static float SetPos(float width, float x)
    {
        if (width + ImGui.GetStyle().ItemSpacing.X >= ImGui.GetContentRegionAvail().X)
        {
            ImGui.NewLine();
            ImGui.SetCursorPosX(x - ImGui.GetStyle().ItemSpacing.X);
        }

        return width;
    }

    private static float SetPosButton(string tag, float x)
        => SetPos(ImGui.CalcTextSize(tag).X + ImGui.GetStyle().FramePadding.X * 2, x);

    private static float SetPosText(string tag, float x)
        => SetPos(ImGui.CalcTextSize(tag).X + ImGui.GetStyle().FramePadding.X * 2 + 15 * ImGuiHelpers.GlobalScale, x);

    private int InputString(float width, string oldTag, out string editedTag)
    {
        ImGui.SetNextItemWidth(width);
        ImGui.InputText("##edit", ref _currentTag, 128);
        if (ImGui.IsItemDeactivated())
        {
            editedTag = _currentTag;
            var ret = editedTag == oldTag ? -1 : _editIdx;
            _editIdx = -1;
            return ret;
        }

        editedTag = string.Empty;
        return -1;
    }

    private void Button(string tag, int idx, bool editable)
    {
        if (!ImGui.Button(tag) || !editable)
            return;

        _editIdx    = idx;
        _setFocus   = true;
        _currentTag = tag;
    }

    // The SameLine() in the regular HelpMarker makes it unusable with AlignTextToFramePadding.
    private static void AlignedHelpMarker(string tooltip)
    {
        if (tooltip.Length == 0)
            return;

        ImGui.AlignTextToFramePadding();
        using (var font = ImRaii.PushFont(UiBuilder.IconFont))
        {
            using var style = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemInnerSpacing);
            ImGui.TextDisabled(FontAwesomeIcon.InfoCircle.ToIconString());
        }

        ImGuiUtil.HoverTooltip(tooltip);
    }
}
