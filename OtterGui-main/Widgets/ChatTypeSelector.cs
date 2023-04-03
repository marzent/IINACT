using System;
using Dalamud.Game.Text;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public static partial class Widget
{
    // Regular combo to select a Dalamud chat type.
    // Can have a tooltip on hover.
    // Returns true if a different chat type was selected and calls the setter.
    public static bool DrawChatTypeSelector(string label, string description, XivChatType currentValue, Action<XivChatType> setter)
    {
        using var id    = ImRaii.PushId(label);
        using var combo = ImRaii.Combo(label, currentValue.ToString());
        ImGuiUtil.HoverTooltip(description);
        if (!combo)
            return false;

        var ret = false;
        // Draw the actual combo values.
        foreach (var type in Enum.GetValues<XivChatType>())
        {
            if (!ImGui.Selectable(type.ToString(), currentValue == type) || type == currentValue)
                continue;

            setter(type);
            ret = true;
        }

        return ret;
    }
}
