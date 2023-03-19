using System.Net;
using System.Numerics;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;
using FFXIV_ACT_Plugin.Config;

namespace IINACT.Windows;

public class ConfigWindow : Window, IDisposable
{
    private Configuration Configuration { get; init; }

    public ConfigWindow(Plugin plugin) : base("IINACT Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(307, 247),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        ImGui.TextColored(ImGuiColors.DalamudGrey, "WebSocket Server");

        var wsServerIp = OverlayPluginConfig?.WSServerIP ?? "";
        ImGui.InputText("IP", ref wsServerIp, 100, ImGuiInputTextFlags.None);

        if (IPAddress.TryParse(wsServerIp, out var address))
        {
            if (OverlayPluginConfig is not null)
            {
                OverlayPluginConfig.WSServerIP = address.ToString();
                OverlayPluginConfig.Save();
            }
                
        }
        else if (wsServerIp == "*")
            if (OverlayPluginConfig is not null)
            {
                OverlayPluginConfig.WSServerIP = "*";
                OverlayPluginConfig.Save();
            }

        var wsServerPort = OverlayPluginConfig?.WSServerPort.ToString() ?? "";
        ImGui.InputText("Port", ref wsServerPort, 100, ImGuiInputTextFlags.None);

        if (int.TryParse(wsServerPort, out var port))
        {
            if (OverlayPluginConfig is not null)
            {
                OverlayPluginConfig.WSServerPort = port;
                OverlayPluginConfig.Save();
            }
        }
        
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        
        ImGui.TextColored(ImGuiColors.DalamudGrey, "Parse Options");

        if (ImGui.BeginCombo("Parse Filter", Enum.GetName(typeof(ParseFilterMode), Configuration.ParseFilterMode)))
        {
            foreach (var filter in Enum.GetValues<ParseFilterMode>())
                if (ImGui.Selectable(Enum.GetName(typeof(ParseFilterMode), filter), (ParseFilterMode)Configuration.ParseFilterMode == filter))
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

        if (!showDebug)
            return;

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
}
