using System.Diagnostics;
using System.Windows.Forms;
using CactbotSelf;
using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Network;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIV_ACT_Plugin.Network.PacketHandlers;
using IINACT.Windows;
using PostNamazu;
using static OtterGui.Raii.ImRaii;

namespace IINACT;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class Plugin : IDalamudPlugin
{
    public string Name => "IINACT";
    
    private const string MainWindowCommandName = "/iinact";
    private const string EndEncCommandName = "/endenc";
    private const string ChatCommandName = "/chat";
    public readonly WindowSystem WindowSystem = new("IINACT");
    
    // ReSharper disable UnusedAutoPropertyAccessor.Local
    // ReSharper restore UnusedAutoPropertyAccessor.Local
    internal Configuration Configuration { get; init; }
    private TextToSpeechProvider TextToSpeechProvider { get; init; }
    private ConfigWindow ConfigWindow { get; init; }
    private MainWindow MainWindow { get; init; }
    internal FileDialogManager FileDialogManager { get; init; }
    private IpcProviders IpcProviders { get; init; }

    private FfxivActPluginWrapper FfxivActPluginWrapper { get; init; }
    private RainbowMage.OverlayPlugin.PluginMain OverlayPlugin { get; set; }
    internal string OverlayPluginStatus => OverlayPlugin.Status;
    private PluginLogTraceListener PluginLogTraceListener { get; init; }

    private delegate void OnUpdateInputUI(IntPtr EventArgument);
    private Hook<OnUpdateInputUI> onUpdateInputUIHook;
    private static readonly Queue<string> ChatQueue = new();
    public DateTime NextClick;
    public Plugin(DalamudPluginInterface pluginInterface)
    {
        DalamudApi.Initialize(this, pluginInterface);
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        
        FileDialogManager = new FileDialogManager();
        Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = DalamudApi.Network;

        var fetchDeps = new FetchDependencies.FetchDependencies(
            DalamudApi.PluginInterface.AssemblyLocation.Directory!.FullName, Util.HttpClient);
        
        fetchDeps.GetFfxivPlugin();

        Advanced_Combat_Tracker.ActGlobals.oFormActMain = new Advanced_Combat_Tracker.FormActMain();

        Configuration = DalamudApi.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
        Configuration.Initialize(DalamudApi.PluginInterface);

        this.TextToSpeechProvider = new TextToSpeechProvider(Configuration);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.LogFilePath = Configuration.LogFilePath;

        FfxivActPluginWrapper = new FfxivActPluginWrapper(Configuration, DalamudApi.ClientState.ClientLanguage, DalamudApi.Chat);
        OverlayPlugin = InitOverlayPlugin();

        IpcProviders = new IpcProviders(DalamudApi.PluginInterface);

        ConfigWindow = new ConfigWindow(this);
        MainWindow = new MainWindow(this);

        WindowSystem.AddWindow(ConfigWindow);
        WindowSystem.AddWindow(MainWindow);
        DalamudApi.Framework.Update += Updata;
        onUpdateInputUIHook= Hook<OnUpdateInputUI>.FromAddress(
                    DalamudApi.SigScanner.ScanText("4C 8B DC 53 56 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 83 B9"), OnUpdateInputUIDo);
        onUpdateInputUIHook.Enable();
        this.NextClick = DateTime.Now;
        DalamudApi.Commands.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Displays the IINACT main window"
        });
        DalamudApi.Commands.ProcessCommand(MainWindowCommandName);
        DalamudApi.Commands.AddHandler(EndEncCommandName, new CommandInfo(EndEncounter)
        {
            HelpMessage = "Ends the current encounter IINACT is parsing"
        });
        DalamudApi.Commands.AddHandler(ChatCommandName, new CommandInfo(ChatDo)
        {
            HelpMessage = "chat"
        });
        DalamudApi.PluginInterface.UiBuilder.Draw += DrawUI;
        DalamudApi.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
    }

    private void OnUpdateInputUIDo(IntPtr EventArgument)
    {
        onUpdateInputUIHook.Original(EventArgument);
        var now = DateTime.Now;

        if (this.NextClick < now && ChatQueue.Count > 0)
        {

            var com = ChatQueue.Dequeue();
            ChatHelper.SendMessage(com);
            this.NextClick = now.AddSeconds(1/6);
        }
    }

    private void Updata(Framework framework)
    {

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
        DalamudApi.Framework.Update -= Updata;
        DalamudApi.Commands.RemoveHandler(MainWindowCommandName);
        DalamudApi.Commands.RemoveHandler(EndEncCommandName);
        DalamudApi.Commands.RemoveHandler(ChatCommandName);
        onUpdateInputUIHook.Disable();
    }
    public  static CactbotSelf.CactbotSelf cactboSelf;
    private RainbowMage.OverlayPlugin.PluginMain InitOverlayPlugin()
    {
        var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
        
        var logger = new RainbowMage.OverlayPlugin.Logger();
        container.Register(logger);
        container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

        container.Register(Util.HttpClient);
        container.Register(FileDialogManager);

        var overlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            DalamudApi.PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;
        
        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(DalamudApi.PluginInterface.ConfigDirectory.FullName);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayPresets;
            var serverController = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            MainWindow.Server = serverController;
            IpcProviders.Server = serverController;
            ConfigWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
            var post = new PostNamazu.PostNamazu(DalamudApi.Commands);
            post.InitPlugin();
            cactboSelf = new CactbotSelf.CactbotSelf(Configuration.shunxu, true);
            cactboSelf.InitPlugin();
            PluginLog.Log("初始化鲶鱼精");
        });

        return overlayPlugin;
    }

    private void OnCommand(string command, string args)
    {
        MainWindow.IsOpen = !MainWindow.IsOpen;
        FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.GetServerTime();
    }

    private static void EndEncounter(string command, string args)
    {
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.EndCombat(false);
    }
    private void ChatDo(string command, string arguments)
    {
        string[] array = arguments.Split(new char[]
    {
                    ' '
    });
        if (array.Length >=2)
        {
            NextClick = DateTime.Now.AddSeconds(1/6);
            ChatQueue.Enqueue(arguments);
            //ChatHelper.SendMessage(arguments);
        }

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
