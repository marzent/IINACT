using System.Diagnostics;
using System.Reflection;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using IINACT.Windows;

namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "IINACT";
    public Version Version { get; }

    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    public readonly WindowSystem WindowSystem = new("IINACT");
    
    internal DalamudPluginInterface PluginInterface { get; }
    internal CommandManager CommandManager { get; }
    internal GameNetwork GameNetwork { get; }
    internal DataManager DataManager { get; }
    internal ChatGui ChatGui { get; }
    internal Framework Framework { get; }

    internal Configuration Configuration { get; }
    private TextToSpeechProvider TextToSpeechProvider { get; }
    private MainWindow MainWindow { get; }
    internal FileDialogManager FileDialogManager { get; }
    private IpcProviders IpcProviders { get; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; }
    private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    private RainbowMage.OverlayPlugin.WebSocket.ServerController? WebSocketServer { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    private PluginLogTraceListener PluginLogTraceListener { get; }
    private HttpClient HttpClient { get; }

    public Plugin([RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
                  [RequiredVersion("1.0")] CommandManager commandManager,
                  [RequiredVersion("1.0")] GameNetwork gameNetwork,
                  [RequiredVersion("1.0")] DataManager dataManager,
                  [RequiredVersion("1.0")] ChatGui chatGui,
                  [RequiredVersion("1.0")] Framework framework)
    {
        PluginInterface = pluginInterface;
        CommandManager = commandManager;
        GameNetwork = gameNetwork;
        DataManager = dataManager;
        ChatGui = chatGui;
        Framework = framework;

        Version = Assembly.GetExecutingAssembly().GetName().Version!;

        FileDialogManager = new FileDialogManager();
        Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = GameNetwork;

        HttpClient = new HttpClient();
        
        var fetchDeps =
            new FetchDependencies.FetchDependencies(Version, PluginInterface.AssemblyLocation.Directory!.FullName,
                                                    HttpClient);
        
        fetchDeps.GetFfxivPlugin();
        
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);

        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        this.TextToSpeechProvider = new TextToSpeechProvider();
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

        FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration, DataManager.Language, ChatGui);
        OverlayPlugin = InitOverlayPlugin();

        IpcProviders = new IpcProviders(PluginInterface);

        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(MainWindow);

        CommandManager.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Displays the IINACT main window"
        });

        CommandManager.AddHandler(EndEncCommandName, new CommandInfo(OnCommand)
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

        container.Register(HttpClient);
        container.Register(FileDialogManager);
        container.Register(PluginInterface);
        container.Register(Framework);

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
            MainWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
        });

        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
        if (command == EndEncCommandName)
        {
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(false);
            return;
        }
            
        switch (args) 
        {
            case "start": //deprecated
            case "ws start":
                WebSocketServer?.Start();
                break;
            case "stop": //deprecated
            case "ws stop":
                WebSocketServer?.Stop();
                break;
            case "log start":
                Configuration.WriteLogFile = true;
                Configuration.Save();
                break;
            case "log stop":
                Configuration.WriteLogFile = false;
                Configuration.Save();
                break;
            default:
                MainWindow.IsOpen = true;
                break;
        }
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
        FileDialogManager.Draw();
    }

    public void DrawConfigUI()
    {
        MainWindow.IsOpen = true;
    }
}
