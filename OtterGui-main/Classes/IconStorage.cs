using System;
using System.Collections.Generic;
using Dalamud.Data;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using ImGuiScene;
using Lumina.Data.Files;

namespace OtterGui.Classes;

public class IconStorage : IDisposable
{
    private readonly DalamudPluginInterface        _pi;
    private readonly DataManager                   _gameData;
    private readonly Dictionary<uint, TextureWrap> _icons;

    public IconStorage(DalamudPluginInterface pi, DataManager gameData, int size = 0)
    {
        _pi       = pi;
        _gameData = gameData;
        _icons    = new Dictionary<uint, TextureWrap>(size);
    }

    private static string HqPath(uint id)
        => $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}_hr1.tex";

    private static string NormalPath(uint id)
        => $"ui/icon/{id / 1000 * 1000:000000}/{id:000000}.tex";

    public bool IconExists(uint id)
        => _gameData.FileExists(HqPath(id)) || _gameData.FileExists(NormalPath(id));

    public TextureWrap this[int id]
        => LoadIcon(id);

    private TexFile? LoadIconHq(uint id)
        => _gameData.GetFile<TexFile>(HqPath(id));

    public TextureWrap LoadIcon(int id)
        => LoadIcon((uint)id);

    public TextureWrap LoadIcon(uint id)
    {
        if (_icons.TryGetValue(id, out var ret))
            return ret;

        var icon = LoadIconHq(id) ?? _gameData.GetIcon(id);
        if (icon == null)
        {
            PluginLog.Warning($"No icon with id {id} could be found.");
            ret = _pi.UiBuilder.LoadImageRaw(new byte[] { 0, 0, 0, 0, }, 1, 1, 4);
        }
        else
        {
            var iconData = icon.GetRgbaImageData();

            ret = _pi.UiBuilder.LoadImageRaw(iconData, icon.Header.Width, icon.Header.Height, 4);
        }

        _icons[id] = ret;
        return ret;
    }

    public void Dispose()
    {
        foreach (var icon in _icons.Values)
            icon.Dispose();
        _icons.Clear();
    }

    ~IconStorage()
        => Dispose();
}
