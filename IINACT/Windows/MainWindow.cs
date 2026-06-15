using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using FFXIV_ACT_Plugin.Config;
using RainbowMage.OverlayPlugin;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using NAudio.Wave;
using RainbowMage.OverlayPlugin.EventSources;
using System.Collections.ObjectModel;
using System.Speech.Synthesis;

namespace IINACT.Windows;

public class MainWindow : Window, IDisposable
{
    private Plugin Plugin { get; }

    private int selectedOverlayIndex;
    private readonly ReadOnlyCollection<InstalledVoice> installedVoicesSapi;

    public MainWindow(Plugin plugin) : base($"IINACT v{plugin.Version}")
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(307, 207),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        if (Dalamud.Utility.Util.IsWine())
        {
            installedVoicesSapi = new([]);
        }
        else
        {
            using var synth = new SpeechSynthesizer();
            installedVoicesSapi = synth.GetInstalledVoices();
        }

        Plugin = plugin;
    }

    public IPluginConfig? OverlayPluginConfig { get; set; }
    public BuiltinEventConfig? OverlayPluginEventConfig { get; set; }
    public IReadOnlyList<RainbowMage.OverlayPlugin.IOverlayTemplate>? OverlayPresets { get; set; }
    private string[]? OverlayNames => OverlayPresets?.Select(x => x.Name).ToArray();
    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }

    public void Dispose() { }

    public override void Draw()
    {
        using var bar = ImRaii.TabBar("settingsTabs");
        if (!bar) return;

        DrawMainWindow();
        DrawParseSettings();
        DrawTtsSettings();
        DrawWebSocketSettings();
    }

    private void DrawMainWindow()
    {
        using var tab = ImRaii.TabItem("Status");
        if (!tab) return;

        ImGui.Spacing();
        ImGui.TextColored(ImGuiColors.DalamudGrey, "OverlayPlugin Status:");
        ImGuiHelpers.ScaledRelativeSameLine(155);
        ImGui.Text(Plugin.OverlayPluginStatus);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(ImGuiColors.DalamudGrey, "Overlay URI generator:");

        var comboWidth = ImGui.GetWindowWidth() * 0.8f;
        
        var selectedIndexOverlayName = OverlayNames?[selectedOverlayIndex] ?? "";
        var selectedOverlayName = Plugin.Configuration.SelectedOverlay ?? selectedIndexOverlayName;
        if (selectedOverlayName != selectedIndexOverlayName)
            for (var i = 0; i < OverlayNames?.Length; i++)
                if (OverlayNames?[i] == selectedOverlayName) 
                    selectedOverlayIndex = i;
        
        ImGui.SetNextItemWidth(comboWidth);
        if (ImGui.BeginCombo("Overlay", selectedOverlayName))
        {
            for (var i = 0; i < OverlayNames?.Length; i++)
            {
                var currentOverlayName = OverlayNames?[i] ?? "";
                if (ImGui.Selectable(currentOverlayName, currentOverlayName == selectedOverlayName))
                {
                    selectedOverlayIndex = i;
                    Plugin.Configuration.SelectedOverlay = currentOverlayName;
                    Plugin.Configuration.Save();
                }
            }

            ImGui.EndCombo();
        }

        var selectedOverlay = OverlayPresets?[selectedOverlayIndex];
        Uri.TryCreate($"ws://{Server?.Address}:{Server?.Port}/ws", UriKind.Absolute, out var webSocketServer);
        var overlayUri = selectedOverlay?.ToOverlayUri(webSocketServer);
        var overlayUriString = overlayUri?.ToString() ?? "<Error generating URI>";

        ImGui.SetNextItemWidth(comboWidth);
        ImGui.InputText("URI", ref overlayUriString, 1000, ImGuiInputTextFlags.ReadOnly);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();
        var serverStatus = Server is null ? "Initializing..." : "Stopped";

        if (Server?.Running ?? false)
            serverStatus = $"Listening on {Server?.Address}:{Server?.Port}";

        if (Server?.Failed ?? false)
        {
            serverStatus = Server.LastException?.Message ?? "Failed";
            if (Server.LastException is SocketException { ErrorCode: 10048 })
                serverStatus = $"Port {Server?.Port} is already in use";
        }

        ImGui.TextColored(ImGuiColors.DalamudGrey, $"WebSocket Server:");
        ImGuiHelpers.ScaledRelativeSameLine(155);
        ImGui.Text(serverStatus);
        ImGui.GetWindowDpiScale();

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

     private void DrawParseSettings()
    {
        using var tab = ImRaii.TabItem("Parser");
        if (!tab) return;

        ImGui.Spacing();
        var elementWidth = ImGui.GetWindowWidth() - (150 * ImGuiHelpers.GlobalScale);
        var logFilePath = Plugin.Configuration.LogFilePath;
        ImGui.SetNextItemWidth(elementWidth);
        ImGui.InputText("Log File Path", ref logFilePath, 200, ImGuiInputTextFlags.ReadOnly);
        ImGui.SameLine();
        if (ImGuiComponents.DisabledButton(FontAwesomeIcon.Folder))
        {
            Plugin.FileDialogManager.OpenFolderDialog("Pick a folder to save logs to", (success, path) =>
            {
                if (!success) return;
                Plugin.Configuration.LogFilePath = path;
                Plugin.Configuration.Save();
            }, Plugin.Configuration.LogFilePath);
        }
        ImGui.Spacing();
        ImGui.SetNextItemWidth(elementWidth);
        if (ImGui.BeginCombo("Parse Filter",
                             Enum.GetName(typeof(ParseFilterMode), Plugin.Configuration.ParseFilterMode)))
        {
            foreach (var filter in Enum.GetValues<ParseFilterMode>())
                if (ImGui.Selectable(Enum.GetName(typeof(ParseFilterMode), filter),
                                     (ParseFilterMode)Plugin.Configuration.ParseFilterMode == filter))
                {
                    Plugin.Configuration.ParseFilterMode = (int)filter;
                    Plugin.Configuration.Save();
                }

            ImGui.EndCombo();
        }

        ImGui.Spacing();
        
        var writeLogFile = Plugin.Configuration.WriteLogFile;
        if (ImGui.Checkbox("Write out network log file", ref writeLogFile))
        {
            Plugin.Configuration.WriteLogFile = writeLogFile;
            Plugin.Configuration.Save();
        }

        var disablePvp = Plugin.Configuration.DisablePvp;
        if (ImGui.Checkbox("Disable writing out network log file in PvP", ref disablePvp))
        {
            if (Plugin.ClientState.IsPvP && disablePvp) Plugin.Configuration.DisableWritingPvpLogFile = true;

            Plugin.Configuration.DisablePvp = disablePvp;
            Plugin.Configuration.Save();
        }

        var logChatMessages = Plugin.Configuration.LogChatMessages;
        if (ImGui.Checkbox("Include chat and echo messages in log files", ref logChatMessages))
        {
            Plugin.Configuration.LogChatMessages = logChatMessages;
            Plugin.SetChatMessageLoggingEnabled(logChatMessages);
            Plugin.Configuration.Save();
        }

        var autoDeleteNetworkLogs = Plugin.Configuration.AutoDeleteNetworkLogs;
        if (ImGui.Checkbox("Automatically delete old network log files", ref autoDeleteNetworkLogs))
        {
            Plugin.Configuration.AutoDeleteNetworkLogs = autoDeleteNetworkLogs;
            Plugin.Configuration.Save();
        }

        if (autoDeleteNetworkLogs)
        {
            var networkLogRetentionDays = Plugin.Configuration.NetworkLogRetentionDays;
            ImGui.Text("Delete logs older than");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(30 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputInt("days", ref networkLogRetentionDays))
            {
                Plugin.Configuration.NetworkLogRetentionDays = Math.Clamp(networkLogRetentionDays, 1, 3650);
                Plugin.Configuration.Save();
            }
        }

        var disableDamageShield = Plugin.Configuration.DisableDamageShield;
        if (ImGui.Checkbox("Disable Damage Shield Estimates", ref disableDamageShield))
        {
            Plugin.Configuration.DisableDamageShield = disableDamageShield;
            Plugin.Configuration.Save();
        }

        var disableCombinePets = Plugin.Configuration.DisableCombinePets;
        if (ImGui.Checkbox("Disable Combine Pets with Owners", ref disableCombinePets))
        {
            Plugin.Configuration.DisableCombinePets = disableCombinePets;
            Plugin.Configuration.Save();
        }

        var endEncounterOutOfCombat = OverlayPluginEventConfig?.EndEncounterOutOfCombat ?? true;
        if (ImGui.Checkbox("End encounter automatically after leaving combat", ref endEncounterOutOfCombat))
        {
            if (OverlayPluginEventConfig is not null)
            {
                OverlayPluginEventConfig.EndEncounterOutOfCombat = endEncounterOutOfCombat;
                if (OverlayPluginConfig is not null)
                {
                    OverlayPluginEventConfig.SaveConfig(OverlayPluginConfig);
                    OverlayPluginConfig.Save();
                }
            }
        }

        var showDebug = Plugin.Configuration.ShowDebug;
        if (ImGui.Checkbox("Show Debug Options", ref showDebug))
        {
            Plugin.Configuration.ShowDebug = showDebug;
            Plugin.Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var playerCharacterName = Plugin.Configuration.PlayerCharacterName;
        ImGui.SetNextItemWidth(elementWidth);
        if (ImGui.InputText("Player name", ref playerCharacterName, 100))
        {
            Plugin.Configuration.PlayerCharacterName = playerCharacterName;
            Plugin.Configuration.Save();
        }

        if (!showDebug) return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var simulateIndividualDoTCrits = Plugin.Configuration.SimulateIndividualDoTCrits;
        if (ImGui.Checkbox("Simulate Individual DoT Crits", ref simulateIndividualDoTCrits))
        {
            Plugin.Configuration.SimulateIndividualDoTCrits = simulateIndividualDoTCrits;
            Plugin.Configuration.Save();
        }

        var showRealDoTTicks = Plugin.Configuration.ShowRealDoTTicks;
        if (ImGui.Checkbox("Also Show 'Real' DoT Ticks", ref showRealDoTTicks))
        {
            Plugin.Configuration.ShowRealDoTTicks = showRealDoTTicks;
            Plugin.Configuration.Save();
        }
    }

    private void DrawTtsSettings()
    {
        using var tab = ImRaii.TabItem("Text to Speech");
        if (!tab) return;

        ImGui.Spacing();

        var ttsDeviceCount = WaveOut.DeviceCount;
        var currentDevice = Plugin.Configuration.TtsPlaybackDevice;
        var currentDeviceName = currentDevice == -1 ? "Default" : WaveOut.GetCapabilities(currentDevice).ProductName;

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);

        if (ImGui.BeginCombo("Playback Device", currentDeviceName))
        {
            if (ImGui.Selectable("Default", currentDevice == -1))
            {
                Plugin.Configuration.TtsPlaybackDevice = -1;
                Plugin.Configuration.Save();
            }

            for (var i = 0; i < ttsDeviceCount; i++)
            {
                var caps = WaveOut.GetCapabilities(i);
                if (ImGui.Selectable(caps.ProductName, currentDevice == i))
                {
                    Plugin.Configuration.TtsPlaybackDevice = i;
                    Plugin.Configuration.Save();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.Spacing();

        ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);

        var volumePercent = (int)(Plugin.Configuration.TtsVolume * 100f);
        if (ImGui.SliderInt("TTS Volume", ref volumePercent, 0, 200, "%d%%"))
        {
            Plugin.Configuration.TtsVolume = volumePercent / 100f;
            Plugin.Configuration.Save();
        }

        ImGui.Spacing();
        ImGui.Spacing();

        var forceGoogleTts = Plugin.Configuration.ForceGoogleTts;

        ImGui.BeginDisabled(Dalamud.Utility.Util.IsWine());
        {
            if (ImGui.RadioButton("SAPI:", ref forceGoogleTts, false))
            {
                Plugin.Configuration.ForceGoogleTts = forceGoogleTts;
                Plugin.Configuration.Save();
            }

            ImGui.Spacing();

            ImGui.BeginDisabled(forceGoogleTts);
            {
                ImGui.SetNextItemWidth(200 * ImGuiHelpers.GlobalScale);

                var hasVoices = installedVoicesSapi?.Any() ?? false;

                var currentVoice = hasVoices
                                 ? (Plugin.Configuration.SapiVoice ?? installedVoicesSapi?.FirstOrDefault()?.VoiceInfo.Name)
                                 : "No voices available";

                if (ImGui.BeginCombo("SAPI Voice", currentVoice))
                {
                    if (hasVoices)
                    {
                        foreach (var voice in installedVoicesSapi!)
                        {
                            if (ImGui.Selectable(voice.VoiceInfo.Name, voice.VoiceInfo.Name.Equals(currentVoice)))
                            {
                                Plugin.Configuration.SapiVoice = voice.VoiceInfo.Name;
                                Plugin.Configuration.Save();
                            }
                        }
                    }
                    else
                    {
                        ImGui.BeginDisabled();
                        {
                            ImGui.Selectable("No voices available", false);
                        }
                        ImGui.EndDisabled();
                    }

                    ImGui.EndCombo();
                }
            }
            ImGui.EndDisabled();
        }
        ImGui.EndDisabled();

        ImGui.Spacing();
        ImGui.Spacing();

        if (ImGui.RadioButton("GoogleTTS:", ref forceGoogleTts, true))
        {
            Plugin.Configuration.ForceGoogleTts = forceGoogleTts;
            Plugin.Configuration.Save();
        }

        ImGui.Spacing();

        ImGui.BeginDisabled(!forceGoogleTts);
        {
            var googleTtsLanguage = Plugin.Configuration.GoogleTtsLanguage;
            ImGui.SetNextItemWidth(100 * ImGuiHelpers.GlobalScale);
            if (ImGui.InputText("Language", ref googleTtsLanguage, 10))
            {
                Plugin.Configuration.GoogleTtsLanguage = googleTtsLanguage;
                Plugin.Configuration.Save();
            }
            ImGui.SameLine();
            ImGui.TextColored(ImGuiColors.DalamudGrey, "(e.g. ja, en, de, fr, ko)");
        }
        ImGui.EndDisabled();
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
