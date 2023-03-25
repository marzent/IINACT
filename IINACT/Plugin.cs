using System.Diagnostics;
using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using IINACT.Windows;

namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "IINACT";
    
    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    public readonly WindowSystem WindowSystem = new("IINACT");
    
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal static CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal static GameNetwork GameNetwork { get; private set; } = null!;
    [PluginService] internal static DataManager DataManager { get; private set; } = null!;
    [PluginService] internal static ChatGui ChatGui { get; private set; } = null!;
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    internal Configuration Configuration { get; init; }
    private TextToSpeechProvider TextToSpeechProvider { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    internal FileDialogManager FileDialogManager { get; init; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; init; }
    private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    private PluginLogTraceListener PluginLogTraceListener { get; init; }

    public Plugin()
    {
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        
        FileDialogManager = new FileDialogManager();
        Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = GameNetwork;
        
        var fetchDeps = new FetchDependencies.FetchDependencies(
            PluginInterface.AssemblyLocation.Directory!.FullName, Util.HttpClient);
        
        fetchDeps.GetFfxivPlugin();

        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        this.TextToSpeechProvider = new TextToSpeechProvider();
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

        FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration, DataManager.Language, ChatGui);
        OverlayPlugin = InitOverlayPlugin();

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Displays the IINACT main window"
        });

        CommandManager.AddHandler(EndEncCommandName, new CommandInfo(EndEncounter)
        {
            HelpMessage = "Ends the current encounter IINACT is parsing"
        });

        PluginInterface.UiBuilder.Draw += DrawUI;
        PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    public void Dispose()
    {
        FfxivActPluginWrapper.Dispose();
        OverlayPlugin.DeInitPlugin();
        Trace.Listeners.Remove(PluginLogTraceListener);

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(MainWindowCommandName);
        CommandManager.RemoveHandler(EndEncCommandName);
    }

    private RainbowMage.OverlayPlugin.PluginMain InitOverlayPlugin()
    {
        var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
        
        var logger = new RainbowMage.OverlayPlugin.Logger();
        container.Register(logger);
        container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

        container.Register(Util.HttpClient);
        container.Register(FileDialogManager);

        var overlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;
        
        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(PluginInterface.ConfigDirectory.FullName);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayPresets;
            MainWindow.Server = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            ConfigWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
        });

        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
        FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.GetServerTime();
    }

    private static void EndEncounter(string command, string args)
    {
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(false);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
