using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;
using FFXIV_ACT_Plugin.Config;

namespace IINACT.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Configuration Configuration;

    public ConfigWindow(Plugin plugin) : base("IINACT Configuration")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(307, 147),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration = plugin.Configuration;
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
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
