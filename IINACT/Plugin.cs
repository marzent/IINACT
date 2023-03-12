using Advanced_Combat_Tracker;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Game.Network;
using IINACT.Windows;

namespace IINACT
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "IINACT";
        private const string CommandName = "/iinact";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private GameNetwork GameNetwork { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("IINACT");
        public Label OverlayPluginStatus = new();

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        private FfxivActPluginWrapper FfxivActPluginWrapper { get; init; }
        private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] GameNetwork gameNetwork)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            GameNetwork = gameNetwork;
            Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = GameNetwork;

            var fetchDeps = new FetchDependencies.FetchDependencies(
                PluginInterface.AssemblyLocation.Directory?.FullName!, Dalamud.Utility.Util.HttpClient);
            try 
            {
                fetchDeps.GetFfxivPlugin();
            }
            catch
            {
                return;
            }

            ActGlobals.oFormActMain = new FormActMain();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            
            if (Directory.Exists(Configuration.LogFilePath))
                ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

            FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration);

            Task.Run(InitOverlayPlugin);
            
            ConfigWindow = new ConfigWindow(this);
            MainWindow = new MainWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);
            WindowSystem.AddWindow(MainWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "A useful message to display in /xlhelp"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }
        
        private void InitOverlayPlugin() {
            var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
            var logger = new RainbowMage.OverlayPlugin.Logger();
            container.Register(logger);
            container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

            OverlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
                Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "OverlayPlugin"), logger, container);
            container.Register(OverlayPlugin);
            ActGlobals.oFormActMain.OverlayPluginContainer = container;

            OverlayPlugin.InitPlugin(OverlayPluginStatus);
        }

        public void Dispose()
        {
            FfxivActPluginWrapper.Dispose();
            OverlayPlugin.DeInitPlugin();

            WindowSystem.RemoveAllWindows();
            
            ConfigWindow.Dispose();
            MainWindow.Dispose();
            
            CommandManager.RemoveHandler(CommandName);
        }

        private void OnCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            MainWindow.IsOpen = true;
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
}
