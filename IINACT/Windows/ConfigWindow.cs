using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;

namespace IINACT.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base(
        "IINACT Configuration",
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(232, 75);
        SizeCondition = ImGuiCond.Always;

        Configuration = plugin.Configuration;
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        // can't ref a property, so use a local copy
        var configValue = Configuration.ShowDebug;
        if (ImGui.Checkbox("Show Debug", ref configValue))
        {
            Configuration.ShowDebug = configValue;
            Configuration.Save();
        }
    }
}
