using System.Net;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using RainbowMage.OverlayPlugin;
using FFXIV_ACT_Plugin.Config;
using Dalamud.Interface.Raii;
using Advanced_Combat_Tracker;

namespace IINACT.Windows;

public class ConfigWindow : Window, IDisposable
{
    public List<string> shunxu = new List<string> { "黑骑", "枪刃", "战士", "骑士", "白魔", "占星", "贤者", "学者", "武士", "武僧", "镰刀", "龙骑", "忍者", "机工", "舞者", "诗人", "黑魔", "召唤", "赤魔" };
    public enum TTS
    {
        女晓晓,
        女晓依,
        男云健,
        男云扬,
        男云霞,
        男云希,
        女曉佳, 
        女曉臻,
        女七海,
        女阿莉雅,
        女珍妮,
        男盖, 
        女索尼娅
    }
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
        if (Configuration.shunxu is null || Configuration.shunxu.Count <= 10)
        {
            Configuration.shunxu = shunxu;
        }
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
                Configuration.Save();
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
        ImGui.Spacing();
        if (ImGui.Checkbox("使用Edeg语音",ref Configuration.UseEdeg))
        {
            Configuration.Save();
        }

        if (Configuration.UseEdeg)
        {
            if (ImGui.BeginCombo("TTS", Enum.GetName(typeof(TTS), Configuration.TTSIndex)))
            {
                foreach (var tts in Enum.GetValues<TTS>())
                {
                    if (ImGui.Selectable(Enum.GetName(typeof(TTS), tts),
                                    (TTS)Configuration.TTSIndex == tts))
                    {
                        Configuration.TTSIndex = (int)tts;
                        Configuration.Save();
                    }
                }
                ImGui.EndCombo();
            }

        }

        if (showDebug)
        {
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
        ImGui.BeginChild("cover window", new Vector2(100, 200));
        ImGui.ListBox("##1", ref selet, Configuration.shunxu.ToArray(), Configuration.shunxu.Count);
        ImGui.EndChild();
        ImGui.SameLine(0, -10);
        ImGui.SetCursorPosY((float)(ImGui.GetCursorPosY() + 30));
        ImGui.SetCursorPosX((float)(ImGui.GetCursorPosX() -20));
        ImGui.BeginChild("cover window1", new Vector2(50, 150), false, ImGuiWindowFlags.NoDocking);
        if (ImGui.Button("↑"))
        {
            if (selet>=1)
            {
                var 交换 = Configuration.shunxu[selet];
                Configuration.shunxu[selet] = Configuration.shunxu[selet - 1];
                Configuration.shunxu[selet - 1] = 交换;
                selet -= 1;
                if (Plugin.cactboSelf is not null)
                {
                    Plugin.cactboSelf.ChangeSetting(Configuration.shunxu, true);
                }
              
            }
        }
        ImGui.SetCursorPosY((float)(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() * 0.5));
        if (ImGui.Button("initi"))
        {
           
           Configuration.shunxu=shunxu;
            selet = 0;
            if (Plugin.cactboSelf is not null)
            {
                Plugin.cactboSelf.ChangeSetting(Configuration.shunxu, true);
            }
        }
        ImGui.SetCursorPosY((float)(ImGui.GetCursorPosY() + ImGui.GetTextLineHeight() * 0.5));
        if (ImGui.Button("↓"))
        {
            if (selet <= Configuration.shunxu.Count-2)
            {
                var 交换 = Configuration.shunxu[selet];
                Configuration.shunxu[selet] = Configuration.shunxu[selet + 1];
                Configuration.shunxu[selet + 1] = 交换;
                selet += 1;
                if (Plugin.cactboSelf is not null)
                {
                    Plugin.cactboSelf.ChangeSetting(Configuration.shunxu, true);
                }
            }
        }
        ImGui.EndChild();
        ;
    }
    private int selet=0;
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
