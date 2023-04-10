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
    [PluginService] internal DalamudPluginInterface PluginInterface { get; private set; } = null!;
    [PluginService] internal CommandManager CommandManager { get; private set; } = null!;
    [PluginService] internal GameNetwork GameNetwork { get; private set; } = null!;
    [PluginService] internal DataManager DataManager { get; private set; } = null!;
    [PluginService] internal ChatGui ChatGui { get; private set; } = null!;
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    internal Configuration Configuration { get; }
    private TextToSpeechProvider TextToSpeechProvider { get; }
    private ConfigWindow ConfigWindow { get; }
    private MainWindow MainWindow { get; }
    internal FileDialogManager FileDialogManager { get; }
    private IpcProviders IpcProviders { get; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; }
    private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    private RainbowMage.OverlayPlugin.WebSocket.ServerController? WebSocketServer { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    private PluginLogTraceListener PluginLogTraceListener { get; }

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

        IpcProviders = new IpcProviders(PluginInterface);

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
        IpcProviders.Dispose();
        
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
        container.Register(PluginInterface);

        var overlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;
        
        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(PluginInterface.ConfigDirectory.FullName);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayTemplates;
            WebSocketServer = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            MainWindow.Server = WebSocketServer;
            IpcProviders.Server = WebSocketServer;
            IpcProviders.OverlayIpcHandler = container.Resolve<RainbowMage.OverlayPlugin.Handlers.Ipc.IpcHandlerController>();
            ConfigWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
        });

        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
        switch (args) 
        {
            case "start":
                WebSocketServer?.Start();
                break;
            case "stop":
                WebSocketServer?.Stop();
                break;
            default:
                MainWindow.IsOpen = true;
                break;
        }
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
