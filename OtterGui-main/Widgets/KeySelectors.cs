using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Keys;
using ImGuiNET;
using OtterGui.Classes;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public static partial class Widget
{
    // Regular combo to select a Dalamud virtual key from the given list of available keys..
    // Can have a tooltip on hover.
    // Returns true if a different key was selected and calls the setter.
    public static bool KeySelector(string label, string description, VirtualKey currentValue, Action<VirtualKey> setter,
        IReadOnlyList<VirtualKey> keys)
    {
        using var id    = ImRaii.PushId(label);
        using var combo = ImRaii.Combo(label, currentValue.GetFancyName());
        ImGuiUtil.HoverTooltip(description);
        if (!combo)
            return false;

        var ret = false;
        // Draw the actual combo values.
        foreach (var key in keys)
        {
            if (!ImGui.Selectable(key.GetFancyName(), currentValue == key) || currentValue == key)
                continue;

            setter(key);
            ret = true;
        }

        return ret;
    }

    // A KeySelector that only allows valid Modifier hotkeys.
    public static bool ModifierSelector(string label, string description, ModifierHotkey currentValue, Action<ModifierHotkey> setter)
        => KeySelector(label, description, currentValue, k => setter(k), ModifierHotkey.ValidKeys);

    // A selector widget for one or two modifiers.
    // If the first modifier is set, shows a second modifier key selector.
    // Returns true and calls the setter if any new key was selected.
    // If an earlier key is set to No Key, all subsequent keys are set to No Key, too.
    public static bool DoubleModifierSelector(string label, string description, float width, DoubleModifier currentValue,
        Action<DoubleModifier> setter)
    {
        var       changes = false;
        var       copy    = currentValue;
        using var id      = ImRaii.PushId(label);
        ImGui.SetNextItemWidth(width);
        changes |= ModifierSelector(label, description, currentValue.Modifier1, k => copy.SetModifier1(k));

        if (currentValue.Modifier1 != ModifierHotkey.NoKey)
        {
            using var indent = ImRaii.PushIndent();
            ImGui.SetNextItemWidth(width - indent.Indentation);
            changes |= ModifierSelector("Additional Modifier",
                "Set another optional modifier key to be used in conjunction with the first modifier.",
                currentValue.Modifier2, k => copy.SetModifier2(k));
        }

        if (changes)
            setter(copy);
        return changes;
    }

    // A selector widget for a full modifiable key.
    // Shows a key selector for the given list of available keys.
    // If this key is set, shows a modifier key selector.
    // If the first modifier is set, shows a second modifier key selector.
    // Returns true and calls the setter if any new key was selected.
    // If an earlier key is set to No Key, all subsequent keys are set to No Key, too.
    public static bool ModifiableKeySelector(string label, string description, float width, ModifiableHotkey currentValue,
        Action<ModifiableHotkey> setter, IReadOnlyList<VirtualKey> keys)
    {
        using var id   = ImRaii.PushId(label);
        var       copy = currentValue;
        ImGui.SetNextItemWidth(width);
        var changes = KeySelector(label, description, currentValue.Hotkey, k => copy.SetHotkey(k), keys);

        if (currentValue.Hotkey != VirtualKey.NO_KEY)
        {
            using var indent = ImRaii.PushIndent();
            ImGui.SetNextItemWidth(width - indent.Indentation);
            changes |= ModifierSelector("Modifier", "Set an optional modifier key to be used in conjunction with the selected hotkey.",
                currentValue.Modifier1,             k => copy.SetModifier1(k));

            if (currentValue.Modifier1 != VirtualKey.NO_KEY)
            {
                ImGui.SetNextItemWidth(width - indent.Indentation);
                changes |= ModifierSelector("Additional Modifier",
                    "Set another optional modifier key to be used in conjunction with the selected hotkey and the first modifier.",
                    currentValue.Modifier2, k => copy.SetModifier2(k));
            }
        }

        if (changes)
            setter(copy);
        return changes;
    }
}
