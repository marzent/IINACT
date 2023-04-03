using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;

namespace OtterGui;

public static class ImGuiPie
{
    private static bool Clockwise(Vector2 v1, Vector2 v2)
        => v1.Y * v2.X - v1.X * v2.Y > 0;

    private static bool InSegment(Vector2 v, Vector2 center, float radius, float piStart, float piEnd)
    {
        var segmentStart = center + radius * new Vector2((float)Math.Cos(piStart), (float)Math.Sin(piStart));
        var segmentEnd   = center + radius * new Vector2((float)Math.Cos(piEnd),   (float)Math.Sin(piEnd));
        return !Clockwise(segmentStart, v) && Clockwise(segmentEnd, v);
    }

    public static void Draw(float radius, IReadOnlyList<(float Percentage, Action DrawTooltip, uint Color)> sections)
    {
        var ptr       = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetWindowPos() + ImGui.GetCursorPos() + ImGui.GetStyle().ItemSpacing.X * Vector2.UnitX;
        var center    = cursorPos + Vector2.One * radius;

        var lastArcEnd = 0f;
        ptr.AddCircle(center, radius, 0xFFFFFFFF, 128, 4 * ImGuiHelpers.GlobalScale);
        ptr.AddCircleFilled(center, radius, 0xFF808080);
        var cursor       = ImGui.GetMousePos() - center;
        var cursorLength = cursor.LengthSquared();
        var radiusSq     = radius * radius;
        var radians      = Math.Atan2(cursor.Y, cursor.X);
        if (radians < 0)
            radians += 2 * Math.PI;
        foreach (var section in sections)
        {
            var newArcEnd = lastArcEnd + section.Percentage * 2 * (float)Math.PI;
            ptr.PathClear();
            ptr.PathArcTo(center, radius, lastArcEnd, newArcEnd);
            ptr.PathLineTo(center);
            ptr.PathFillConvex(section.Color);
            if (cursorLength <= radiusSq && radians >= lastArcEnd && radians < newArcEnd)
                section.DrawTooltip();
            lastArcEnd = newArcEnd;
        }

        ImGui.SetCursorPos(center + Vector2.UnitX * radius);
    }
}
