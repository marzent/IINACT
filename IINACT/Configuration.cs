using Dalamud.Configuration;
using Dalamud.Plugin;
using FFXIV_ACT_Plugin.Config;

namespace IINACT;

[Serializable]
public class Configuration : IPluginConfiguration
{
    [NonSerialized]
    private DalamudPluginInterface? PluginInterface;

    public ParseFilterMode ParseFilterMode { get; set; } = ParseFilterMode.None;

    public bool DisableDamageShield { get; set; } = false;

    public bool DisableCombinePets { get; set; } = false;

    public bool SimulateIndividualDoTCrits { get; set; } = false;

    public bool ShowRealDoTTicks { get; set; } = false;

    public bool ShowDebug { get; set; } = false;

    public string? LogFilePath { get; set; }

    public int Version { get; set; } = 0;

    public void Initialize(DalamudPluginInterface pluginInterface)
    {
        PluginInterface = pluginInterface;
    }

    public void Save()
    {
        PluginInterface!.SavePluginConfig(this);
    }
}
