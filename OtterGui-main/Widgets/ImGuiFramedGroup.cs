using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface.Components;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public static partial class Widget
{
    public static void BeginFramedGroup(string label, string description = "")
        => BeginFramedGroupInternal(label, Vector2.Zero, description);

    public static void BeginFramedGroup(string label, Vector2 minSize, string description = "")
        => BeginFramedGroupInternal(label, minSize, description);

    private static void BeginFramedGroupInternal(string label, Vector2 minSize, string description)
    {
        var itemSpacing     = ImGui.GetStyle().ItemSpacing;
        var frameHeight     = ImGui.GetFrameHeight();
        var halfFrameHeight = new Vector2(ImGui.GetFrameHeight() / 2, 0);

        ImGui.BeginGroup(); // First group

        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero)
            .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGui.BeginGroup(); // Second group

        var effectiveSize = minSize;
        if (effectiveSize.X < 0)
            effectiveSize.X = ImGui.GetContentRegionAvail().X;

        // Ensure width.
        ImGui.Dummy(Vector2.UnitX * effectiveSize.X);
        // Ensure left half boundary width/distance.
        ImGui.Dummy(halfFrameHeight);

        ImGui.SameLine();
        ImGui.BeginGroup(); // Third group.
        // Ensure right half of boundary width/distance
        ImGui.Dummy(halfFrameHeight);

        // Label block
        ImGui.SameLine();
        using var group     = ImRaii.Group();
        ImGui.TextUnformatted(label);

        if (description.Length > 0)
        {
            ImGui.SameLine();
            ImGui.Dummy(itemSpacing);
            ImGui.SameLine();
            ImGuiComponents.HelpMarker(description);
        }

        group.Dispose();

        var labelMin = ImGui.GetItemRectMin();
        var labelMax = ImGui.GetItemRectMax();
        ImGui.SameLine();
        // Ensure height and distance to label.
        ImGui.Dummy(Vector2.UnitY * (frameHeight + itemSpacing.Y));

        ImGui.BeginGroup(); // Fourth Group.

        style.Pop(2);
        // This seems wrong?
        //ImGui.SetWindowSize( new Vector2( ImGui.GetWindowSize().X - frameHeight, ImGui.GetWindowSize().Y ) );

        var itemWidth = ImGui.CalcItemWidth();
        ImGui.PushItemWidth(Math.Max(0f, itemWidth - frameHeight));

        LabelStack.Push((labelMin, labelMax));
    }

    private static void DrawClippedRect(Vector2 clipMin, Vector2 clipMax, Vector2 drawMin, Vector2 drawMax, uint color, float thickness)
    {
        ImGui.PushClipRect(clipMin, clipMax, true);
        ImGui.GetWindowDrawList().AddRect(drawMin, drawMax, color, ImGui.GetStyle().FrameRounding, ImDrawFlags.RoundCornersAll, thickness);
        ImGui.PopClipRect();
    }

    public static void EndFramedGroup()
    {
        var borderColor     = ImGui.GetColorU32(ImGuiCol.Border);
        var itemSpacing     = ImGui.GetStyle().ItemSpacing;
        var frameHeight     = ImGui.GetFrameHeight();
        var halfFrameHeight = new Vector2(ImGui.GetFrameHeight() / 2, 0);
        var (currentLabelMin, currentLabelMax) = LabelStack.Pop();

        ImGui.PopItemWidth();

        using var style = ImRaii.PushStyle(ImGuiStyleVar.FramePadding, Vector2.Zero)
            .Push(ImGuiStyleVar.ItemSpacing, Vector2.Zero);

        ImGui.EndGroup(); // Close fourth group
        ImGui.EndGroup(); // Close third group

        ImGui.SameLine();
        // Ensure right distance.
        ImGui.Dummy(halfFrameHeight);
        // Ensure bottom distance
        ImGui.Dummy(Vector2.UnitY * (frameHeight / 2 - itemSpacing.Y));
        ImGui.EndGroup(); // Close second group

        var itemMin   = ImGui.GetItemRectMin();
        var itemMax   = ImGui.GetItemRectMax();
        var halfFrame = new Vector2(frameHeight / 8, frameHeight / 2);
        var frameMin  = itemMin + halfFrame;
        var frameMax  = itemMax - Vector2.UnitX * halfFrame.X;
        currentLabelMin.X -= itemSpacing.X;
        currentLabelMax.X += itemSpacing.X;
        var thickness = 2 * ImGui.GetStyle().ChildBorderSize;

        // Left
        DrawClippedRect(new Vector2(-float.MaxValue, -float.MaxValue), currentLabelMin with { Y = float.MaxValue }, frameMin,
            frameMax,                                                  borderColor,                                 thickness);
        // Right
        DrawClippedRect(currentLabelMax with { Y = -float.MaxValue }, new Vector2(float.MaxValue, float.MaxValue), frameMin,
            frameMax,                                                 borderColor,                                 thickness);
        // Top
        DrawClippedRect(currentLabelMin with { Y = -float.MaxValue }, new Vector2(currentLabelMax.X, currentLabelMin.Y), frameMin,
            frameMax,                                                 borderColor,                                       thickness);
        // Bottom
        DrawClippedRect(new Vector2(currentLabelMin.X, currentLabelMax.Y), currentLabelMax with { Y = float.MaxValue }, frameMin,
            frameMax,                                                      borderColor,                                 thickness);

        style.Pop(2);
        // This seems wrong?
        // ImGui.SetWindowSize( new Vector2( ImGui.GetWindowSize().X + frameHeight, ImGui.GetWindowSize().Y ) );
        ImGui.Dummy(Vector2.Zero);

        ImGui.EndGroup(); // Close first group
    }

    private static readonly Stack<(Vector2, Vector2)> LabelStack = new();
}
