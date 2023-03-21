using System.Net;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;
using FFXIV_ACT_Plugin.Config;

namespace IINACT.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration { get; init; }
    private FileDialogManager FileDialogManager { get; init; }

    public ConfigWindow(Plugin plugin) : base("IINACT Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(307, 207),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
        FileDialogManager = plugin.FileDialogManager;
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        using var bar = ImRaii.TabBar("settingsTabs");
        if (!bar) return;

        DrawParseSettings();
        DrawWebSocketSettings();
    }
    
     private void DrawParseSettings()
    {
        using var tab = ImRaii.TabItem("Parser");
        if (!tab) return;
        
        ImGui.Spacing();
        var elementWidth = ImGui.GetWindowWidth() - (150 * ImGuiHelpers.GlobalScale);
        var logFilePath = Configuration.LogFilePath;
        ImGui.SetNextItemWidth(elementWidth);
        ImGui.InputText("Log File Path", ref logFilePath, 200, ImGuiInputTextFlags.ReadOnly);
        ImGui.SameLine();
        if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder))
        {
            FileDialogManager.OpenFolderDialog("Pick a folder to save logs to", (success, path) =>
            {
                if (!success) return;
                Configuration.LogFilePath = path;
            }, Configuration.LogFilePath);
        }
        ImGui.Spacing();
        ImGui.SetNextItemWidth(elementWidth);
        if (ImGui.BeginCombo("Parse Filter",
                             Enum.GetName(typeof(ParseFilterMode), Configuration.ParseFilterMode)))
        {
            foreach (var filter in Enum.GetValues<ParseFilterMode>())
                if (ImGui.Selectable(Enum.GetName(typeof(ParseFilterMode), filter),
                                     (ParseFilterMode)Configuration.ParseFilterMode == filter))
                {
                    Configuration.ParseFilterMode = (int)filter;
                    Configuration.Save();
                }

            ImGui.EndCombo();
        }

        ImGui.Spacing();

        var disableDamageShield = Configuration.DisableDamageShield;
        if (ImGui.Checkbox("Disable Damage Shield Estimates", ref disableDamageShield))
        {
            Configuration.DisableDamageShield = disableDamageShield;
            Configuration.Save();
        }

        var disableCombinePets = Configuration.DisableCombinePets;
        if (ImGui.Checkbox("Disable Combine Pets with Owners", ref disableCombinePets))
        {
            Configuration.DisableCombinePets = disableCombinePets;
            Configuration.Save();
        }

        var showDebug = Configuration.ShowDebug;
        if (ImGui.Checkbox("Show Debug Options", ref showDebug))
        {
            Configuration.ShowDebug = showDebug;
            Configuration.Save();
        }

        if (!showDebug) return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var simulateIndividualDoTCrits = Configuration.SimulateIndividualDoTCrits;
        if (ImGui.Checkbox("Simulate Individual DoT Crits", ref simulateIndividualDoTCrits))
        {
            Configuration.SimulateIndividualDoTCrits = simulateIndividualDoTCrits;
            Configuration.Save();
        }

        var showRealDoTTicks = Configuration.ShowRealDoTTicks;
        if (ImGui.Checkbox("Also Show 'Real' DoT Ticks", ref showRealDoTTicks))
        {
            Configuration.ShowRealDoTTicks = showRealDoTTicks;
            Configuration.Save();
        }
    }

    private void DrawWebSocketSettings()
    {
        using var tab = ImRaii.TabItem("WebSocket Server");
        if (!tab) return;
        
        ImGui.Spacing();
        var wsServerIp = OverlayPluginConfig?.WSServerIP ?? "";
        ImGui.InputText("IP", ref wsServerIp, 100, ImGuiInputTextFlags.None);

        if (IPAddress.TryParse(wsServerIp, out var address))
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerIP = address.ToString();
        }
        else if (wsServerIp == "*")
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerIP = "*";
        }

        var wsServerPort = OverlayPluginConfig?.WSServerPort.ToString() ?? "";
        ImGui.InputText("Port", ref wsServerPort, 100, ImGuiInputTextFlags.None);

        if (int.TryParse(wsServerPort, out var port))
        {
            if (OverlayPluginConfig is not null)
                OverlayPluginConfig.WSServerPort = port;
        }

        OverlayPluginConfig?.Save();
    }

}
