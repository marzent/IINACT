using Dalamud.Data;
using Dalamud.Game.Command;
using Dalamud.Game.Network;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Utility;
using IINACT.Windows;

namespace IINACT;

public sealed class Plugin : IDalamudPlugin
{
    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    public Label OverlayPluginStatus = new();
    public WindowSystem WindowSystem = new("IINACT");
    
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    [PluginService] internal static DalamudPluginInterface PluginInterface { get; private set; }
    [PluginService] internal static CommandManager CommandManager { get; private set; }
    [PluginService] internal static GameNetwork GameNetwork { get; private set; }
    [PluginService] internal static DataManager DataManager { get; private set; }
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    internal Configuration Configuration { get; init; }

    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; init; }
    private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    public string Name => "IINACT";

    public Plugin()
    {
        Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = GameNetwork;
        
        var fetchDeps = new FetchDependencies.FetchDependencies(
            PluginInterface.AssemblyLocation.Directory!.FullName, Util.HttpClient);
        
        try
        {
            fetchDeps.GetFfxivPlugin();
        }
        catch
        {
            // TODO: log and handle errors here
            return;
        }

        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain();

        Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(PluginInterface);

        if (Directory.Exists(Configuration.LogFilePath))
            Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

        FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration, DataManager.Language);

        Task.Run(InitOverlayPlugin);

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

        WindowSystem.RemoveAllWindows();

        ConfigWindow.Dispose();
        MainWindow.Dispose();

        CommandManager.RemoveHandler(MainWindowCommandName);
        CommandManager.RemoveHandler(EndEncCommandName);
    }

    private void InitOverlayPlugin()
    {
        var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
        var logger = new RainbowMage.OverlayPlugin.Logger();
        container.Register(logger);
        container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

        OverlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(OverlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;

        OverlayPlugin.InitPlugin(OverlayPluginStatus, PluginInterface.ConfigDirectory.FullName);

        var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
        MainWindow.OverlayPresets = registry.OverlayPresets;
        MainWindow.Server = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
        ConfigWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = true;
    }

    private static void EndEncounter(string command, string args)
    {
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(false);
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    public void DrawConfigUI()
    {
        ConfigWindow.IsOpen = true;
    }
}
