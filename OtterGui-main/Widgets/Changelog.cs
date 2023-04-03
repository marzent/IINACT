using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using Dalamud.Interface;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public enum ChangeLogDisplayType
{
    New,
    HighlightOnly,
    Never,
}

public sealed class Changelog : Window
{
    public static string ToName(ChangeLogDisplayType type)
    {
        return type switch
        {
            ChangeLogDisplayType.New           => "Show New Changelogs (Recommended)",
            ChangeLogDisplayType.HighlightOnly => "Only Show Important Changelogs",
            ChangeLogDisplayType.Never         => "Never Show Changelogs (Dangerous)",
            _                                  => string.Empty,
        };
    }

    public const int FreshInstallVersion = int.MaxValue;

    public const uint DefaultHeaderColor    = 0xFF60D0D0;
    public const uint DefaultHighlightColor = 0xFF6060FF;

    private readonly Func<(int, ChangeLogDisplayType)> _getConfig;
    private readonly Action<int, ChangeLogDisplayType> _setConfig;

    private readonly List<(string Title, List<Entry> Entries, bool HasHighlight)> _entries = new();

    private int                  _lastVersion;
    private ChangeLogDisplayType _displayType;

    public uint HeaderColor { get; set; } = DefaultHeaderColor;
    public bool ForceOpen   { get; set; } = false;

    public Changelog(string label, Func<(int, ChangeLogDisplayType)> getConfig, Action<int, ChangeLogDisplayType> setConfig)
        : base(label, ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoResize, true)
    {
        _getConfig         = getConfig;
        _setConfig         = setConfig;
        Position           = null;
        RespectCloseHotkey = false;
        ShowCloseButton    = false;
    }

    public override void PreOpenCheck()
    {
        (_lastVersion, _displayType) = _getConfig();
        if (ForceOpen)
        {
            IsOpen = true;
            return;
        }

        if (_lastVersion == FreshInstallVersion)
        {
            IsOpen = false;
            _setConfig(_entries.Count, _displayType);
            return;
        }

        switch (_displayType)
        {
            case ChangeLogDisplayType.New:
                IsOpen = _lastVersion < _entries.Count;
                break;
            case ChangeLogDisplayType.HighlightOnly:
                IsOpen = _entries.Skip(_lastVersion).Any(t => t.HasHighlight);
                if (!IsOpen && _lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.Never);
                break;
            case ChangeLogDisplayType.Never:
                IsOpen = false;
                if (_lastVersion < _entries.Count)
                    _setConfig(_entries.Count, ChangeLogDisplayType.Never);
                break;
        }
    }

    public override void PreDraw()
    {
        Size = new Vector2(Math.Min(ImGui.GetMainViewport().Size.X / ImGuiHelpers.GlobalScale / 2, 800),
            ImGui.GetMainViewport().Size.Y / ImGuiHelpers.GlobalScale / 2);
        ImGuiHelpers.SetNextWindowPosRelativeMainViewport((ImGui.GetMainViewport().Size - Size.Value * ImGuiHelpers.GlobalScale) / 2,
            ImGuiCond.Appearing);
    }

    public override void Draw()
    {
        DrawEntries();
        var pos = Size!.Value.X * ImGuiHelpers.GlobalScale / 3;
        ImGui.SetCursorPosX(pos);
        DrawDisplayTypeCombo(pos);
        ImGui.SetCursorPosX(pos);
        DrawUnderstoodButton(pos);
    }

    private void DrawEntries()
    {
        using var child = ImRaii.Child("Entries", new Vector2(-1, -ImGui.GetFrameHeight() * 3));
        if (!child)
            return;

        var i = 0;
        foreach (var ((name, list, hasHighlight), idx) in _entries.WithIndex().Reverse())
        {
            using var id    = ImRaii.PushId(i++);
            using var color = ImRaii.PushColor(ImGuiCol.Text, HeaderColor);
            var       flags = ImGuiTreeNodeFlags.NoTreePushOnOpen;


            // Do open the newest entry if it is the only new entry, if it has highlights or if no highlights are required
            var isOpen = idx == _entries.Count - 1
                ? idx == _lastVersion || _displayType != ChangeLogDisplayType.HighlightOnly || hasHighlight
                // Automatically open all entries that have not been seen, if they have highlights or do not require highlights
                : idx >= _lastVersion && (hasHighlight || _displayType != ChangeLogDisplayType.HighlightOnly);

            if (isOpen)
                flags |= ImGuiTreeNodeFlags.DefaultOpen;

            var tree = ImGui.TreeNodeEx(name, flags);
            CopyToClipboard(name, list);
            color.Pop();
            if (!tree)
                continue;

            foreach (var entry in list)
                entry.Draw();
        }
    }

    private void DrawDisplayTypeCombo(float width)
    {
        ImGui.SetNextItemWidth(width);
        using var combo = ImRaii.Combo("##DisplayType", ToName(_displayType));
        if (!combo)
            return;

        foreach (var type in Enum.GetValues<ChangeLogDisplayType>())
        {
            if (ImGui.Selectable(ToName(type)))
                _setConfig(_lastVersion, type);
        }
    }

    private void DrawUnderstoodButton(float width)
    {
        if (!ImGui.Button("Understood", new Vector2(width, 0)))
            return;

        if (_lastVersion != _entries.Count)
            _setConfig(_entries.Count, _displayType);
        ForceOpen = false;
    }

    public Changelog NextVersion(string title)
    {
        _entries.Add((title, new List<Entry>(), false));
        return this;
    }

    public Changelog RegisterHighlight(string text, ushort level = 0, uint color = DefaultHighlightColor)
    {
        var lastEntry = _entries.Last();
        lastEntry.Entries.Add(new Entry(text, color, level));
        if (color != 0)
            _entries[^1] = lastEntry with { HasHighlight = true };
        return this;
    }

    public Changelog RegisterEntry(string text, ushort level = 0)
    {
        _entries.Last().Entries.Add(new Entry(text, 0, level));
        return this;
    }

    private readonly struct Entry
    {
        public readonly string Text;
        public readonly uint   Color;
        public readonly ushort SubText;

        public Entry(string text, uint color = 0, ushort subText = 0)
        {
            Text    = text;
            Color   = color;
            SubText = subText;
        }

        public void Draw()
        {
            using var tab   = ImRaii.PushIndent(1 + SubText);
            using var color = ImRaii.PushColor(ImGuiCol.Text, Color, Color != 0);
            ImGui.Bullet();
            ImGui.PushTextWrapPos();
            ImGui.TextUnformatted(Text);
            ImGui.PopTextWrapPos();
        }

        public void Append(StringBuilder sb)
        {
            sb.Append("> ");
            if (SubText > 0)
                sb.Append('`');
            for (var i = 0; i < SubText; ++i)
                sb.Append("    ");
            if (SubText > 0)
                sb.Append('`');
            if (Color != 0)
                sb.Append("**");
            sb.Append("- ")
                .Append(Text);
            if (Color != 0)
                sb.Append("**");

            sb.Append('\n');
        }
    }

    [Conditional("DEBUG")]
    private static void CopyToClipboard(string name, List<Entry> entries)
    {
        try
        {
            if (!ImGui.IsItemClicked(ImGuiMouseButton.Right))
                return;

            var sb = new StringBuilder(1024 * 64);
            sb.Append("**")
                .Append(name)
                .Append(" notes, Update <t:")
                .Append(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                .Append(">**\n");

            foreach (var entry in entries)
                entry.Append(sb);

            ImGui.SetClipboardText(sb.ToString());
        }
        catch
        {
            // ignored
        }
    }
}
