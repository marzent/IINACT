using System;
using System.Numerics;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public static partial class Widget
{
    // Create a default color picker box with a few ornaments
    public static bool ColorPicker(string label, string tooltip, uint currentColor, Action<uint> setter, uint defaultColor)
    {
        var       ret   = false;
        var       old   = ImGui.ColorConvertU32ToFloat4(currentColor);
        var       tmp   = old;
        using var _     = ImRaii.PushId(label);
        using var group = ImRaii.Group();
        // Draw the regular color picker with no label.
        if (ImGui.ColorEdit4("##Picker", ref tmp, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs) && tmp != old)
        {
            setter(ImGui.ColorConvertFloat4ToU32(tmp));
            ret = true;
        }

        // Draw a button to return to default.
        ImGui.SameLine();
        if (ImGuiUtil.DrawDisabledButton("Default", Vector2.Zero, string.Empty, currentColor == defaultColor))
        {
            setter(defaultColor);
            ret = true;
        }

        // Draw the default tooltip.
        if (ImGui.IsItemHovered())
        {
            using var tt = ImRaii.Tooltip();
            ImGui.AlignTextToFramePadding();
            ImGui.TextUnformatted($"Reset this color to {Functions.ColorBytes(defaultColor)}.");
            var standardV4 = ImGui.ColorConvertU32ToFloat4(currentColor);
            ImGui.SameLine();
            ImGui.ColorEdit4("", ref standardV4, ImGuiColorEditFlags.AlphaPreviewHalf | ImGuiColorEditFlags.NoInputs);
        }

        // Draw the actual label as well as a potential tooltip.
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        if (tooltip.Length > 0)
            ImGuiUtil.HoverTooltip(tooltip);

        return ret;
    }
}
