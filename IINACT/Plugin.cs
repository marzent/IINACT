using Advanced_Combat_Tracker;
using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using IINACT.Windows;

namespace IINACT
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "IINACT";
        private const string CommandName = "/iinact";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("IINACT");

        private ConfigWindow ConfigWindow { get; init; }
        private MainWindow MainWindow { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;

            var fetchDeps = new FetchDependencies.FetchDependencies(PluginInterface.AssemblyLocation.Directory?.FullName!);
            fetchDeps.GetFfxivPlugin().Wait();

            ActGlobals.oFormActMain = new FormActMain();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);
            
            if (Directory.Exists(Configuration.LogFilePath))
                ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

            var wrapper = new FfxivActPluginWrapper(0, Configuration);

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
        
        private static void InitOverlayPlugin() {
            var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
            var logger = new RainbowMage.OverlayPlugin.Logger();
            container.Register(logger);
            container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

            var pluginMain = new RainbowMage.OverlayPlugin.PluginMain(
                Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "OverlayPlugin"), logger, container);
            container.Register(pluginMain);
            ActGlobals.oFormActMain.OverlayPluginContainer = container;

            var status = new Label();

            pluginMain.InitPlugin(status);
        }

        public void Dispose()
        {
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
