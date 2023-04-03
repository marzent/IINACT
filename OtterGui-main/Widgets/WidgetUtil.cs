using System.Numerics;
using ImGuiNET;

namespace OtterGui.Widgets;

public static class WidgetUtil
{
    private static void RenderArrow(ImDrawListPtr drawList, Vector2 pos, uint color, ImGuiDir dir, float scale)
    {
        var h      = ImGui.GetFontSize();
        var r      = h * 0.4f * scale;
        var center = pos + new Vector2(h / 2, h / 2 * scale);
        var (a, b, c) = dir switch
        {
            ImGuiDir.Down  => (new Vector2(0,          0.75f * r), new Vector2(-0.866f * r, -0.75f * r), new Vector2(0.866f * r, -0.75f * r)),
            ImGuiDir.Up    => (new Vector2(0,          -0.75f * r), new Vector2(0.866f * r, 0.75f * r), new Vector2(-0.866f * r, 0.75f * r)),
            ImGuiDir.Right => (new Vector2(0.75f * r,  0), new Vector2(-0.75f * r,          0.866f * r), new Vector2(-0.75f * r, -0.866f * r)),
            _              => (new Vector2(-0.75f * r, 0), new Vector2(0.75f * r,           -0.866f * r), new Vector2(0.75f * r, 0.866f * r)),
        };
        drawList.AddTriangleFilled(center + a, center + b, center + c, color);
    }
}
