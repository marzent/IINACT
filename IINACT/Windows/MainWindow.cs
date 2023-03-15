using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;

namespace IINACT.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    public IPluginConfig? OverlayPluginConfig { get; set; }
    public IReadOnlyList<IOverlayPreset>? OverlayPresets { get; set; }
    private string[]? OverlayNames => OverlayPresets?.Select(x => x.Name).ToArray();
    private int selectedOverlayIndex = 0;

    public MainWindow(Plugin plugin) : base(
        "IINACT", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        Size = new Vector2(430, 150);
        SizeCondition = ImGuiCond.Always;

        Plugin = plugin;
    }

    public override void Draw()
    {
        ImGui.Text($"OverlayPlugin Status:  {Plugin.OverlayPluginStatus.Text}");

        ImGui.Spacing();

        var selectedOverlayName = OverlayNames?[selectedOverlayIndex] ?? "";
        ImGui.SetNextItemWidth(340);
        if (ImGui.BeginCombo("Overlay", selectedOverlayName))
        {
            for (var i = 0; i < OverlayNames?.Length; i++)
            {
                var currentOverlayName = OverlayNames[i] ?? "";
                if (ImGui.Selectable(currentOverlayName, currentOverlayName == selectedOverlayName))
                    selectedOverlayIndex = i;
            }

            ImGui.EndCombo();
        }

        var selectedOverlay = OverlayPresets?[selectedOverlayIndex];
        var overlayURL = selectedOverlay?.HttpUrl ?? "";
        if (!string.IsNullOrEmpty(overlayURL))
        {
            var ip = OverlayPluginConfig?.WSServerIP;
            var port = OverlayPluginConfig?.WSServerPort;

            if (selectedOverlay?.Modern ?? false)
                overlayURL += $"?OVERLAY_WS=ws://{ip}:{port}/ws";
            else
                overlayURL += $"?HOST_PORT=ws://{ip}:{port}";
            if (!string.IsNullOrEmpty(selectedOverlay?.Options))
                overlayURL += selectedOverlay?.Options;
        }

        ImGui.SetNextItemWidth(340);
        ImGui.InputText("URL", ref overlayURL, 1000, ImGuiInputTextFlags.ReadOnly);

        ImGui.Spacing();

        if (ImGui.Button("Show Settings"))
        {
            Plugin.DrawConfigUI();
        }
    }

    public void Dispose() { }
}
