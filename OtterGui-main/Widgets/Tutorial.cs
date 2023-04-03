using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using ImGuiNET;
using static OtterGui.Raii.ImRaii;

namespace OtterGui.Widgets;

public class Tutorial
{
    public record struct Step(string Name, string Text, bool Enabled);

    public uint   HighlightColor { get; init; } = 0xFF20FFFF;
    public uint   BorderColor    { get; init; } = 0xD00000FF;
    public string PopupLabel     { get; init; } = "Tutorial";

    private readonly List<Step> _steps      = new();
    private          int        _waitFrames = 0;

    public int EndStep
        => _steps.Count;

    public IReadOnlyList<Step> Steps
        => _steps;

    public Tutorial Register(string name, string text)
    {
        _steps.Add(new Step(name, text, true));
        return this;
    }

    public Tutorial Deprecated()
    {
        _steps.Add(new Step(string.Empty, string.Empty, false));
        return this;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Open(int id, int current, Action<int> setter)
    {
        if (current != id)
            return;

        OpenWhenMatch(current, setter);
        --_waitFrames;
    }

    [MethodImpl(MethodImplOptions.AggressiveOptimization | MethodImplOptions.AggressiveInlining)]
    public void Skip(int id, int current, Action<int> setter)
    {
        if (current != id)
            return;

        setter(NextId(current));
    }

    private void OpenWhenMatch(int current, Action<int> setter)
    {
        var step = Steps[current];

        // Skip disabled tutorials.
        if (!step.Enabled)
        {
            setter(NextId(current));
            return;
        }

        if (_waitFrames > 0)
            --_waitFrames;
        else if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows) && !ImGui.IsPopupOpen(PopupLabel))
            ImGui.OpenPopup(PopupLabel);

        var windowPos = HighlightObject();
        DrawPopup(windowPos, step, NextId(current), setter);
    }

    private Vector2 HighlightObject()
    {
        ImGui.SetScrollHereX();
        ImGui.SetScrollHereY();
        var offset = ImGuiHelpers.ScaledVector2(5, 4);
        var min    = ImGui.GetItemRectMin() - offset;
        var max    = ImGui.GetItemRectMax() + offset;
        ImGui.GetForegroundDrawList().PushClipRect(ImGui.GetWindowPos() - offset, ImGui.GetWindowPos() + ImGui.GetWindowSize() + offset);
        ImGui.GetForegroundDrawList().AddRect(min, max, HighlightColor, 5 * ImGuiHelpers.GlobalScale, ImDrawFlags.RoundCornersAll,
            2 * ImGuiHelpers.GlobalScale);
        ImGui.GetForegroundDrawList().PopClipRect();
        return max + new Vector2(ImGuiHelpers.GlobalScale);
    }

    private void DrawPopup(Vector2 pos, Step step, int next, Action<int> setter)
    {
        using var style = DefaultStyle()
            .Push(ImGuiStyleVar.PopupBorderSize, 2 * ImGuiHelpers.GlobalScale)
            .Push(ImGuiStyleVar.PopupRounding,   5 * ImGuiHelpers.GlobalScale);
        using var color = DefaultColors()
            .Push(ImGuiCol.Border,  BorderColor)
            .Push(ImGuiCol.PopupBg, 0xFF000000);
        using var font = DefaultFont();
        // Prevent the window from opening outside of the screen.
        var size = ImGuiHelpers.ScaledVector2(350, 0);
        var diff = ImGui.GetWindowSize().X - size.X;
        pos.X = diff < 0 ? ImGui.GetWindowPos().X : Math.Clamp(pos.X, ImGui.GetWindowPos().X, ImGui.GetWindowPos().X + diff);

        // Ensure the header line is visible with a button to go to next.
        pos.Y = Math.Clamp(pos.Y, ImGui.GetWindowPos().Y + ImGui.GetFrameHeightWithSpacing(), ImGui.GetWindowPos().Y + ImGui.GetWindowSize().Y - ImGui.GetFrameHeightWithSpacing());

        ImGui.SetNextWindowPos(pos);
        ImGui.SetNextWindowSize(size);
        ImGui.SetNextWindowFocus();
        using var popup = Popup(PopupLabel, ImGuiWindowFlags.AlwaysAutoResize | ImGuiWindowFlags.Popup);
        if (!popup)
            return;

        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted(step.Name);
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - ImGui.GetTextLineHeight());
        int? nextValue = ImGuiUtil.DrawDisabledButton(FontAwesomeIcon.ArrowCircleRight.ToIconString(), new Vector2(ImGui.GetFrameHeight()),
            "Go to next tutorial step.", false, true)
            ? next
            : null;

        ImGui.Separator();
        ImGui.PushTextWrapPos();
        foreach (var text in step.Text.Split('\n', StringSplitOptions.TrimEntries))
        {
            if (text.Length == 0)
                ImGui.Spacing();
            else
                ImGui.TextUnformatted(text);
        }

        ImGui.PopTextWrapPos();
        ImGui.NewLine();
        var buttonText = next == EndStep ? "Finish" : "Next";
        nextValue = ImGui.Button(buttonText) ? next : nextValue;
        ImGui.SameLine();
        nextValue = ImGui.Button("Skip Tutorial") ? EndStep : nextValue;
        ImGuiUtil.HoverTooltip("Skip all current tutorial entries, but show any new ones added later.");
        ImGui.SameLine();
        nextValue = ImGui.Button("Disable Tutorial") ? -1 : nextValue;
        ImGuiUtil.HoverTooltip("Disable all tutorial entries.");

        if (nextValue != null)
        {
            setter(nextValue.Value);
            _waitFrames = 2;
            ImGui.CloseCurrentPopup();
        }
    }

    private int NextId(int current)
    {
        for (var i = current + 1; i < EndStep; ++i)
        {
            if (Steps[i].Enabled)
                return i;
        }

        return EndStep;
    }

    // Obtain the current ID if it is enabled, and otherwise the first enabled id after it.
    public int CurrentEnabledId(int current)
    {
        if (current < 0)
            return -1;

        for (var i = current; i < EndStep; ++i)
        {
            if (Steps[i].Enabled)
                return i;
        }

        return EndStep;
    }

    // Make sure you have as many tutorials registered as you intend to.
    public Tutorial EnsureSize(int size)
    {
        if (_steps.Count != size)
            throw new Exception("Tutorial size is incorrect.");

        return this;
    }
}
