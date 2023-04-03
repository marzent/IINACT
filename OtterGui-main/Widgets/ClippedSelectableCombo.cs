using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface;
using ImGuiNET;
using OtterGui.Raii;

namespace OtterGui.Widgets;

public class ClippedSelectableCombo<T>
{
    private readonly int    _pushId;
    private readonly string _label;

    private readonly IList<T>        _items;
    private readonly Func<T, string> _itemToName;

    private          string              _filter = string.Empty;
    private readonly List<(string, int)> _remainingItems;

    private readonly float _previewSize;
    public const     int   ItemsAtOnce = 12;

    public ClippedSelectableCombo(string id, string label, float previewSize, IList<T> items, Func<T, string> itemToName)
    {
        _pushId         = id.GetHashCode();
        _label          = label;
        _items          = items;
        _itemToName     = itemToName;
        _remainingItems = items.Select((s, i) => (itemToName(s).ToLowerInvariant(), i)).ToList();
        _previewSize    = previewSize;
    }

    private bool DrawList(string currentName, out int selectedIdx)
    {
        selectedIdx = -1;
        var       height = ImGui.GetTextLineHeightWithSpacing();
        using var child  = ImRaii.Child("##List", new Vector2(_previewSize * ImGuiHelpers.GlobalScale, height * ItemsAtOnce));
        if (!child)
            return false;

        var tmpIdx = selectedIdx;

        void DrawItemInternal((string, int) p)
        {
            var name = _itemToName(_items[p.Item2]);
            if (ImGui.Selectable(name, currentName == name))
                tmpIdx = p.Item2;
        }

        ImGuiClip.ClippedDraw(_remainingItems, DrawItemInternal, height);
        if (tmpIdx == selectedIdx)
            return false;

        selectedIdx = tmpIdx;
        ImGui.CloseCurrentPopup();
        return true;
    }

    public bool Draw(string currentName, out int newIdx, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        newIdx = -1;
        using var id = ImRaii.PushId(_pushId);
        ImGui.SetNextItemWidth(_previewSize * ImGuiHelpers.GlobalScale);

        using var combo = ImRaii.Combo(_label, currentName, flags | ImGuiComboFlags.HeightLargest);
        if (!combo)
            return false;

        if (ImGui.IsWindowAppearing())
        {
            ImGui.SetKeyboardFocusHere();
            UpdateFilter(string.Empty);
        }

        ImGui.SetNextItemWidth(-1);
        var tmp   = _filter;
        var enter = ImGui.InputTextWithHint("##filter", "Filter...", ref tmp, 255, ImGuiInputTextFlags.EnterReturnsTrue);
        UpdateFilter(tmp);

        if (enter && _remainingItems.Count == 0)
        {
            ImGui.CloseCurrentPopup();
            return false;
        }

        var isFocused = ImGui.IsItemFocused();

        var ret = DrawList(currentName, out newIdx);
        if (ret)
            return true;

        if (!enter && (isFocused || _remainingItems.Count != 1))
            return false;

        newIdx = _remainingItems[0].Item2;
        ImGui.CloseCurrentPopup();
        return true;
    }

    public bool Draw(int currentIdx, out int newIdx, ImGuiComboFlags flags = ImGuiComboFlags.None)
    {
        var ret = false;
        if (currentIdx < 0 || currentIdx >= _items.Count)
        {
            currentIdx = 0;
            newIdx     = currentIdx;
            ret        = true;
        }

        var name = _items.Count > 0 ? _itemToName(_items[currentIdx]) : string.Empty;
        return Draw(name, out newIdx, flags) || ret;
    }

    private void UpdateFilter(string newFilter)
    {
        if (newFilter == _filter)
            return;

        var newLower = newFilter.ToLowerInvariant();
        var lower    = _filter.ToLowerInvariant();

        if (_filter.Length > 0 && newLower.Contains(lower))
        {
            for (var i = 0; i < _remainingItems.Count; ++i)
            {
                if (_remainingItems[i].Item1.Contains(newLower))
                    continue;

                _remainingItems.RemoveAt(i--);
            }
        }
        else if (newLower.Length > 0)
        {
            _remainingItems.Clear();
            for (var i = 0; i < _items.Count; ++i)
            {
                var itemLower = _itemToName(_items[i]).ToLowerInvariant();
                if (itemLower.Contains(newLower))
                    _remainingItems.Add((itemLower, i));
            }
        }
        else
        {
            _remainingItems.Clear();
            for (var i = 0; i < _items.Count; ++i)
                _remainingItems.Add((_itemToName(_items[i]).ToLowerInvariant(), i));
        }

        _filter = newFilter;
    }
}
