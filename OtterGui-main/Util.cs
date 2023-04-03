using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui;

[InterpolatedStringHandler]
public ref struct HoverTooltipStringHandler
{
    private DefaultInterpolatedStringHandler _builder;
    public  bool                             IsEnabled;

    public HoverTooltipStringHandler(int literalLength, int formattedCount, out bool isEnabled)
    {
        IsEnabled = (literalLength > 0 || formattedCount > 0) && ImGui.IsItemHovered();
        isEnabled = IsEnabled;
        _builder  = isEnabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }

    public HoverTooltipStringHandler(int literalLength, int formattedCount, ImGuiHoveredFlags flags, out bool isEnabled)
    {
        IsEnabled = (literalLength > 0 || formattedCount > 0) && ImGui.IsItemHovered(flags);
        isEnabled = IsEnabled;
        _builder  = isEnabled ? new DefaultInterpolatedStringHandler(literalLength, formattedCount) : default;
    }

    public void AppendLiteral(string s)
        => _builder.AppendLiteral(s);

    public void AppendFormatted<T>(T t)
        => _builder.AppendFormatted(t);

    public void AppendFormatted<T>(T t, string format) where T : IFormattable
        => _builder.AppendFormatted(t, format);

    public void AppendFormatted<T>(T t, int alignment) where T : IFormattable
        => _builder.AppendFormatted(t, alignment);

    public void AppendFormatted<T>(T t, int alignment, string format) where T : IFormattable
        => _builder.AppendFormatted(t, alignment, format);

    internal string GetFormattedText()
        => _builder.ToStringAndClear();
}

public static partial class ImGuiUtil
{
    // Exception safe clipboard.
    public static string GetClipboardText()
    {
        try
        {
            return ImGui.GetClipboardText();
        }
        catch
        {
            return string.Empty;
        }
    }

    // Print unformatted text wrapped.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void TextWrapped(string text)
    {
        ImGui.PushTextWrapPos(0);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
    }

    // Draw the same text multiple times to simulate a shadowed text.
    public static void TextShadowed(string text, uint foregroundColor, uint shadowColor, byte shadowWidth = 1)
    {
        var x = ImGui.GetCursorPosX();
        var y = ImGui.GetCursorPosY();

        for (var i = -shadowWidth; i <= shadowWidth; i++)
        {
            for (var j = -shadowWidth; j <= shadowWidth; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                ImGui.SetCursorPosX(x + i);
                ImGui.SetCursorPosY(y + j);
                TextColored(shadowColor, text);
            }
        }

        ImGui.SetCursorPosX(x);
        ImGui.SetCursorPosY(y);
        TextColored(foregroundColor, text);
    }

    // Draw the same text multiple times to simulate a shadowed text.
    public static void TextShadowed(ImDrawListPtr drawList, Vector2 position, string text, uint foregroundColor, uint shadowColor, byte shadowWidth = 1)
    {
        for (var i = -shadowWidth; i <= shadowWidth; i++)
        {
            for (var j = -shadowWidth; j <= shadowWidth; j++)
            {
                if (i == 0 && j == 0)
                    continue;

                drawList.AddText(position + new Vector2(i, j), shadowColor, text);
            }
        }

        drawList.AddText(position, foregroundColor, text);
    }


    // Go to the next column, then enter text.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void TextNextColumn(string text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }

    // Draw a single piece of text in the given color.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void TextColored(uint color, string text)
    {
        using var _ = ImRaii.PushColor(ImGuiCol.Text, color);
        ImGui.TextUnformatted(text);
    }

    public static void BulletTextColored(uint color, string text)
    {
        using var g = ImRaii.Group();
        ImGui.Bullet();
        ImGui.SameLine();
        TextColored(color, text);
    }

    // Create a selectable that copies its text to clipboard when clicked.
    // Also adds a tooltip on hover.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void CopyOnClickSelectable(string text)
    {
        if (ImGui.Selectable(text))
            ImGui.SetClipboardText(text);

        HoverTooltip("Click to copy to clipboard.");
    }

    // Draw a single FontAwesomeIcon.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void PrintIcon(FontAwesomeIcon icon)
    {
        using var font = ImRaii.PushFont(UiBuilder.IconFont);
        ImGui.TextUnformatted(icon.ToIconString());
    }

    // Draw a help marker, followed by a label.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void LabeledHelpMarker(string label, string tooltip)
    {
        ImGuiComponents.HelpMarker(tooltip);
        ImGui.SameLine();
        ImGui.TextUnformatted(label);
        HoverTooltip(tooltip);
    }

    // Drag between min and max with the given speed and format.
    // Has width of width.
    // Returns true if the item was edited but is not active anymore.
    public static bool DragFloat(string label, ref float value, float width, float speed, float min, float max, string format)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.DragFloat(label, ref value, speed, min, max, format))
            value = Math.Clamp(value, min, max);

        return ImGui.IsItemDeactivatedAfterEdit();
    }

    // Drag between min and max with the given speed and format.
    // Has width of width.
    // Returns true if the item was edited but is not active anymore.
    public static bool DragInt(string label, ref int value, float width, float speed, int min, int max, string format)
    {
        ImGui.SetNextItemWidth(width);
        if (ImGui.DragInt(label, ref value, speed, min, max, format))
            value = Math.Clamp(value, min, max);

        return ImGui.IsItemDeactivatedAfterEdit();
    }

    // Create a centered, modal help popup with the given content for the given label.
    // It has a centered 'Understood' button to close the window.
    public static void HelpPopup(string label, Vector2 size, Action content)
    {
        ImGui.SetNextWindowPos(ImGui.GetMainViewport().GetCenter(), ImGuiCond.Always, new Vector2(0.5f));
        ImGui.SetNextWindowSize(size);
        using var pop = ImRaii.Popup(label, ImGuiWindowFlags.Modal | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove);
        if (pop)
        {
            content();
            const string buttonText   = "Understood";
            var          buttonSize   = Math.Max(size.X / 5, ImGui.CalcTextSize(buttonText).X + 2 * ImGui.GetStyle().FramePadding.X);
            var          buttonCenter = (size.X - buttonSize) / 2 - ImGui.GetStyle().WindowPadding.X;
            ImGui.SetCursorPos(new Vector2(buttonCenter, size.Y - ImGui.GetFrameHeight() * 1.75f));
            if (ImGui.Button(buttonText, new Vector2(buttonSize, 0)))
                ImGui.CloseCurrentPopup();
        }
    }

    // Draw a selectable combo box for a generic enumerable.
    // Uses the supplied toString function if any, otherwise ToString.
    // Can specify enum values to skip at start or end and gives all those enum values as options.
    public static bool GenericEnumCombo<T>(string label, float width, T current, out T newValue,
        Func<T, string>? toString = null, int skip = 0, int skipEnd = 0) where T : struct, Enum
        => GenericEnumCombo(label, width, current, out newValue, Enum.GetValues<T>().Skip(skip).SkipLast(skipEnd), toString);

    // Draw a selectable combo box for a generic enumerable.
    // Uses the supplied toString function if any, otherwise ToString.
    // Can specify the options to supply.
    public static bool GenericEnumCombo<T>(string label, float width, T current, out T newValue,
        IEnumerable<T> options, Func<T, string>? toString = null) where T : struct, Enum
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo(label, toString?.Invoke(current) ?? current.ToString());
        if (combo)
            foreach (var data in options)
            {
                var name = toString?.Invoke(data) ?? data.ToString();
                if (name.Length == 0 || !ImGui.Selectable(name, data.Equals(current)) || data.Equals(current))
                    continue;

                newValue = data;
                return true;
            }

        newValue = current;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static bool DrawDisabledButton(string label, Vector2 size, string description, bool disabled, bool icon = false)
    {
        using var dis  = ImRaii.PushStyle(ImGuiStyleVar.Alpha, 0.5f, disabled);
        using var font = ImRaii.PushFont(UiBuilder.IconFont, icon);
        var       ret  = ImGui.Button(label, size);
        font.Pop();
        dis.Pop();
        HoverTooltip(description);
        return ret && !disabled;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void DrawTextButton(string text, Vector2 size, uint buttonColor)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Button, buttonColor)
            .Push(ImGuiCol.ButtonActive,  buttonColor)
            .Push(ImGuiCol.ButtonHovered, buttonColor);
        ImGui.Button(text, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void DrawTextButton(string text, Vector2 size, uint buttonColor, uint textColor)
    {
        using var color = ImRaii.PushColor(ImGuiCol.Button, buttonColor)
            .Push(ImGuiCol.ButtonActive,  buttonColor)
            .Push(ImGuiCol.ButtonHovered, buttonColor)
            .Push(ImGuiCol.Text,          textColor);
        ImGui.Button(text, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverTooltip(string tooltip, ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        if (tooltip.Length > 0 && ImGui.IsItemHovered(flags))
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(tooltip);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverTooltip(HoverTooltipStringHandler tooltip)
    {
        if (tooltip.IsEnabled)
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(tooltip.GetFormattedText());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverTooltip(ImGuiHoveredFlags flags, [InterpolatedStringHandlerArgument("flags")] HoverTooltipStringHandler tooltip)
    {
        if (tooltip.IsEnabled)
        {
            using var tt = ImRaii.Tooltip();
            ImGui.TextUnformatted(tooltip.GetFormattedText());
        }
    }

    public static bool Checkbox(string label, string description, bool current, Action<bool> setter,
        ImGuiHoveredFlags flags = ImGuiHoveredFlags.None)
    {
        var tmp    = current;
        var result = ImGui.Checkbox(label, ref tmp);
        HoverTooltip(description, flags);
        if (!result || tmp == current)
            return false;

        setter(tmp);
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void DrawTableColumn(string text)
    {
        ImGui.TableNextColumn();
        ImGui.TextUnformatted(text);
    }

    public static bool DrawEditButtonText(int id, string current, out string newText, ref bool edit, Vector2 buttonSize, float inputWidth,
        uint maxLength = 256)
    {
        newText = current;
        var       tmpEdit = edit;
        using var style   = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing / 2);
        using var _       = ImRaii.PushId(id);
        if (DrawDisabledButton(FontAwesomeIcon.Edit.ToIconString(), buttonSize, "Rename", edit, true))
            edit = true;
        ImGui.SameLine();
        style.Pop();
        if (!edit)
        {
            DrawTextButton(current, Vector2.Zero, ImGui.GetColorU32(ImGuiCol.FrameBg));
            return false;
        }

        ImGui.SetNextItemWidth(inputWidth);
        if (edit != tmpEdit)
        {
            ImGui.SetKeyboardFocusHere();
            ImGui.SetItemDefaultFocus();
        }

        if (ImGui.InputText("##rename", ref newText, maxLength, ImGuiInputTextFlags.EnterReturnsTrue))
            return true;

        if (edit == tmpEdit && !ImGui.IsItemActive())
            edit = false;
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void HoverIcon(ImGuiScene.TextureWrap icon, Vector2 iconSize)
    {
        ImGui.Image(icon.ImGuiHandle, iconSize);
        HoverIconTooltip(icon, iconSize);
    }

    public static void HoverIconTooltip(ImGuiScene.TextureWrap icon, Vector2 iconSize)
    {
        var size = new Vector2(icon.Width, icon.Height);
        if (iconSize.X > size.X || iconSize.Y > size.Y || !ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            return;

        using var enable = ImRaii.Enabled();
        ImGui.BeginTooltip();
        ImGui.Image(icon.ImGuiHandle, size);
        ImGui.EndTooltip();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightAlign(string text, float offset = 0)
    {
        offset = ImGui.GetContentRegionAvail().X - offset - ImGui.CalcTextSize(text).X;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void RightJustify(string text, uint color)
    {
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() - ImGui.CalcTextSize(text).X);
        TextColored(color, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static void Center(string text)
    {
        var offset = (ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X) / 2;
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
        ImGui.TextUnformatted(text);
    }

    public static bool OpenNameField(string popupName, ref string newName)
    {
        using var popup = ImRaii.Popup(popupName);
        if (!popup)
            return false;

        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            ImGui.CloseCurrentPopup();

        if (ImGui.IsWindowAppearing())
            ImGui.SetKeyboardFocusHere();

        ImGui.SetNextItemWidth(300 * ImGuiHelpers.GlobalScale);
        var enterPressed = ImGui.InputTextWithHint("##newName", "Enter New Name...", ref newName, 64, ImGuiInputTextFlags.EnterReturnsTrue);

        if (!enterPressed)
            return false;

        ImGui.CloseCurrentPopup();
        return true;
    }

    /// <summary>
    /// A dummy that is only applied conditionally.
    /// </summary>
    public static void Dummy(float width, float height, bool condition = true)
    {
        if (!condition)
            return;

        ImGui.Dummy(new Vector2(width, height));
    }

    public static void Dummy(Vector2 size, bool condition = true)
    {
        if (!condition)
            return;

        ImGui.Dummy(size);
    }


    /// <summary>
    /// Input an ushort.
    /// </summary>
    public static unsafe bool InputUInt16(string label, ref ushort v, ImGuiInputTextFlags flags)
    {
        fixed (ushort* v2 = &v)
        {
            return ImGui.InputScalar(label, ImGuiDataType.U16, (nint)v2, IntPtr.Zero, IntPtr.Zero, "%hu", flags);
        }
    }

    /// <summary>
    /// Computes the intensity of a RGB color without taking into consideration alpha values.
    /// </summary>
    public static byte ColorIntensity(uint color)
    {
        color = 1 + (color & 0xFF) + ((color >> 8) & 0xFF) + ((color >> 16) & 0xFF);
        return (byte)(color / 3);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static unsafe bool IsDropping(string name)
        => ImGui.AcceptDragDropPayload(name).NativePtr != null;
}
