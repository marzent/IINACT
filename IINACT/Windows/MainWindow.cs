using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace IINACT.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin Plugin;
    private int selectedOverlayIndex;

    public MainWindow(Plugin plugin) : base("IINACT")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(430, 170),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
        SizeCondition = ImGuiCond.Always;

        Plugin = plugin;
    }
    
    public IReadOnlyList<RainbowMage.OverlayPlugin.IOverlayPreset>? OverlayPresets { get; set; }
    private string[]? OverlayNames => OverlayPresets?.Select(x => x.Name).ToArray();
    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.Text("OverlayPlugin Status:");
        ImGui.SameLine(165);
        ImGui.Text(Plugin.OverlayPluginStatus.Text);
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
            if (selectedOverlay?.Modern ?? false)
                overlayURL += $"?OVERLAY_WS=ws://{Server?.Address}:{Server?.Port}/ws";
            else
                overlayURL += $"?HOST_PORT=ws://{Server?.Address}:{Server?.Port}";
            if (!string.IsNullOrEmpty(selectedOverlay?.Options))
                overlayURL += selectedOverlay?.Options;
        }

        ImGui.SetNextItemWidth(340);
        ImGui.InputText("URL", ref overlayURL, 1000, ImGuiInputTextFlags.ReadOnly);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var serverStatus = Server is null ? "Initializing..." : "Stopped";

        if (Server?.Running ?? false)
            serverStatus = $"Listening on {Server?.Address}:{Server?.Port}";

        if (Server?.Failed ?? false)
            serverStatus = Server.LastException.Message;
        
        ImGui.Text($"WebSocket Server:");
        ImGui.SameLine(165);
        ImGui.Text(serverStatus);

        if (Server?.Running ?? false)
        {
            if (ImGui.Button("Stop")) 
                Server.Stop();
            
            ImGui.SameLine();
            
            if (ImGui.Button("Restart")) 
                Server.Restart();
        }
        else if (Server is not null)
        {
            if (ImGui.Button("Start")) 
                Server.Start();
        }
        
    }
}
