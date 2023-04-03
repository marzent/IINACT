using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public static partial class Widget
{
    // Create a palette color picker with color boxes of size iconSize.
    // The currentColorIdx is selected, the defaultColorIdx can be returned to with a Default button.
    // Colors are given by a dictionary of idx => color.
    // Returns true if a new color was selected in newColorIdx, false if nothing changed.
    // Will set newColorIdx to -1 if the color was cleared.
    public static bool PaletteColorPicker(string label, Vector2 iconSize, int currentColorIdx, int defaultColorIdx,
        IDictionary<int, uint> colors, out int newColorIdx, int boxesPerLine = 10)
    {
        newColorIdx = -1;
        using var group = ImRaii.Group();
        using var id    = ImRaii.PushId(label);

        // If the current index can not be found, draw a black box.
        if (colors.TryGetValue(currentColorIdx, out var currentColor))
            DrawColorBox("##preview",                                                               currentColor, iconSize,
                $"{currentColorIdx} - {Functions.ColorBytes(currentColor)}\nRight-click to clear.", true);
        else
            DrawColorBox("##preview", 0, iconSize, "None", false);

        // On right clicks, clear the current color.
        if (ImGui.IsItemClicked(ImGuiMouseButton.Right))
        {
            newColorIdx = -1;
            return currentColorIdx != -1;
        }

        if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
            ImGui.OpenPopup("##popup");

        // If the given default color exists, add a button in the same line
        // that resets to the default color and is enabled if it is not the default color.
        if (colors.TryGetValue(defaultColorIdx, out var def))
        {
            ImGui.SameLine();
            if (ImGuiUtil.DrawDisabledButton("Default", Vector2.Zero, $"Reset this color to {defaultColorIdx} ({Functions.ColorBytes(def)}).",
                    currentColorIdx == defaultColorIdx))
            {
                newColorIdx = defaultColorIdx;
                return true;
            }
        }

        // If the label contains actual text, add it behind the default button.
        if (label.Length > 0 && !label.StartsWith("##"))
        {
            ImGui.SameLine();
            ImGui.TextUnformatted(label);
        }
        group.Dispose();

        // Draw the selection popup if it is open.
        using var popup = ImRaii.ContextPopup("##popup");
        if (popup)
        {
            var counter = 0;
            foreach (var (idx, value) in colors)
            {
                var text = $"{idx} - {Functions.ColorBytes(value)}";
                DrawColorBox(text, value, iconSize, text, true);
                if (ImGui.IsItemClicked(ImGuiMouseButton.Left))
                {
                    newColorIdx = idx;
                    ImGui.CloseCurrentPopup();
                }

                if (counter++ % boxesPerLine != boxesPerLine - 1)
                    ImGui.SameLine();
            }
        }

        return newColorIdx != -1;
    }

    // Helper function to draw a single box of a specific color.
    // Can have a tooltip.
    private static void DrawColorBox(string label, uint color, Vector2 iconSize, string description, bool push)
    {
        using var c     = ImRaii.PushColor(ImGuiCol.ChildBg, color, push);
        using var style = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, ImGui.GetStyle().FrameRounding);
        ImGui.BeginChild(label, iconSize, true);
        ImGui.EndChild();
        c.Pop();
        ImGuiUtil.HoverTooltip(description);
    }
}
