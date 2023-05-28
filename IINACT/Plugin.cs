using Advanced_Combat_Tracker;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin;
using Dalamud.Utility;
using FFXIV_ACT_Plugin.Logfile;
using IINACT.Windows;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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

    private delegate void OnUpdateInputUI(IntPtr EventArgument);
    private Hook<OnUpdateInputUI> onUpdateInputUIHook;
    private static readonly Queue<string> ChatQueue = new();
    public DateTime NextClick;
    private delegate long ReplayZonePacketDownDelegate(long a, long targetId, long dataPtr);
	private readonly Hook<ReplayZonePacketDownDelegate> replayZonePacketDownHook;
    private delegate void UpdateParty(IntPtr header, IntPtr data, byte a3);
    private Hook<UpdateParty> UpdatePartyHook;
    public Plugin(DalamudPluginInterface pluginInterface)
    {
        DalamudApi.Initialize(this, pluginInterface);
        PluginLogTraceListener = new PluginLogTraceListener();
        Trace.Listeners.Add(PluginLogTraceListener);
        
        FileDialogManager = new FileDialogManager();
        Machina.FFXIV.Dalamud.DalamudClient.GameNetwork = DalamudApi.Network;

        var fetchDeps = new FetchDependencies.FetchDependencies(
            DalamudApi.PluginInterface.AssemblyLocation.Directory!.FullName, Util.HttpClient, DalamudApi.ClientState.ClientLanguage);
        
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
        this.replayZonePacketDownHook = Hook<ReplayZonePacketDownDelegate>.FromAddress(DalamudApi.SigScanner.ScanText("E8 ?? ?? ?? ?? 80 BB ?? ?? ?? ?? ?? 77 93"), ReplayZonePacketDownDetour);
        replayZonePacketDownHook.Enable();
        UpdatePartyHook = Hook<UpdateParty>.FromAddress(DalamudApi.SigScanner.ScanText("48 89 5C 24 ?? 55 56 57 41 54 41 55 41 56 41 57 48 81 EC ?? ?? ?? ?? 48 8B 05 ?? ?? ?? ?? 48 33 C4 48 89 84 24 ?? ?? ?? ?? 48 8B 0D ?? ?? ?? ??"), UpdatePartyDetor);
        UpdatePartyHook.Enable();
        this.NextClick = DateTime.Now;
        DalamudApi.Commands.AddHandler(MainWindowCommandName, new CommandInfo(OnCommand)
        {
            HelpMessage = "Displays the IINACT main window"
        });
        //DalamudApi.Commands.ProcessCommand(MainWindowCommandName);
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
        FormActMain.delta0 = DalamudApi.SigScanner.GetStaticAddressFromSig("89 1D ?? ?? ?? ?? 40 84 FF");
        FormActMain.delta4 = DalamudApi.SigScanner.GetStaticAddressFromSig("89 15 ?? ?? ?? ?? EB 1E");
        FormActMain.deltaC = DalamudApi.SigScanner.GetStaticAddressFromSig("03 05 ?? ?? ?? ?? 03 C3");
    }
    private FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetPluginData()
    {
        return ActGlobals.oFormActMain.FfxivPlugin;
    }
    private int partyLength = 0;
    private void UpdatePartyDetor(IntPtr header, IntPtr dataptr, byte a3)
    {
        PluginLog.Debug($"PartyLength = {partyLength}");
        PluginLog.Debug($"PartyUpdate");
        //partyLength = Marshal.ReadByte(dataptr, (440 * 8) + 17);
        //var lists = new List<uint>();
        //for (int i = 32 + 8; i < 440 * partyLength; i += 440)
        //{
        //    lists.Add((uint)Marshal.ReadInt32(dataptr, i));
        //}
        //var plugin = GetPluginData();
        //var date = (DataSubscription)plugin._iocContainer.GetService(typeof(DataSubscription));
        //date.OnPartyListChanged(lists.AsReadOnly(), partyLength);
        UpdatePartyHook.Original(header, dataptr, a3);
    
    }
    private long ReplayZonePacketDownDetour(long a, long b, long c)
    {
        var opcode = (ushort)Marshal.ReadInt16((nint)b);
        var dataPtr = (IntPtr)c;
        SafeMemory.Read<UInt32>((IntPtr)b + 8, out var sourceID);
        //FFXIVNetworkMonitor.Replay(dataPtr,opcode, sourceID);
        return replayZonePacketDownHook.Original(a, b, c);
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
        replayZonePacketDownHook.Dispose();
        WindowSystem.RemoveAllWindows();
        UpdatePartyHook.Dispose();
        ConfigWindow.Dispose();
        MainWindow.Dispose();
        DalamudApi.Framework.Update -= Updata;
        DalamudApi.Commands.RemoveHandler(MainWindowCommandName);
        DalamudApi.Commands.RemoveHandler(EndEncCommandName);
        DalamudApi.Commands.RemoveHandler(ChatCommandName);
        onUpdateInputUIHook.Disable();
        cactboSelf.DeInitPlugin();
        //post.DeInitPlugin();
    }
    public  static CactbotSelf.CactbotSelf cactboSelf;
    public static PostNamazu.PostNamazu post;
    private RainbowMage.OverlayPlugin.PluginMain InitOverlayPlugin()
    {
        var container = new RainbowMage.OverlayPlugin.TinyIoCContainer();
        
        var logger = new RainbowMage.OverlayPlugin.Logger();
        container.Register(logger);
        container.Register<RainbowMage.OverlayPlugin.ILogger>(logger);

        container.Register(Util.HttpClient);
        container.Register(FileDialogManager);
        container.Register(DalamudApi.PluginInterface);

        var overlayPlugin = new RainbowMage.OverlayPlugin.PluginMain(
            DalamudApi.PluginInterface.AssemblyLocation.Directory!.FullName, logger, container);
        container.Register(overlayPlugin);
        Advanced_Combat_Tracker.ActGlobals.oFormActMain.OverlayPluginContainer = container;
        
        Task.Run(() =>
        {
            overlayPlugin.InitPlugin(DalamudApi.PluginInterface.ConfigDirectory.FullName);

            var registry = container.Resolve<RainbowMage.OverlayPlugin.Registry>();
            MainWindow.OverlayPresets = registry.OverlayTemplates;
            WebSocketServer = container.Resolve<RainbowMage.OverlayPlugin.WebSocket.ServerController>();
            MainWindow.Server = WebSocketServer;
            IpcProviders.Server = WebSocketServer;
            IpcProviders.OverlayIpcHandler = container.Resolve<RainbowMage.OverlayPlugin.Handlers.Ipc.IpcHandlerController>();
            ConfigWindow.OverlayPluginConfig = container.Resolve<RainbowMage.OverlayPlugin.IPluginConfig>();
             post = new PostNamazu.PostNamazu(DalamudApi.Commands);
            post.InitPlugin();
            cactboSelf = new CactbotSelf.CactbotSelf(Configuration.shunxu, true);
            cactboSelf.InitPlugin();
            PluginLog.Log("初始化鲶鱼精");
            setZone();

        });

        return overlayPlugin;
    }
    private void setZone()
    {
        var terr = DalamudApi.GameData.GetExcelSheet<Lumina.Excel.GeneratedSheets.TerritoryType>();
        var zoneID = DalamudApi.ClientState.TerritoryType;
        var zoneName = terr?.GetRow(zoneID)?.PlaceName.Value?.Name.RawString;
        ActGlobals.oFormActMain.CurrentZone = zoneName;
        if (_logOutput == null)
        {
            var plugin = ActGlobals.oFormActMain.FfxivPlugin;
            _logOutput = (ILogOutput)plugin._iocContainer.GetService(typeof(ILogOutput));
        }
        string text = FormatChangeZoneMessage(zoneID, zoneName);
        WriteLogLineImpl(1, text);
    }
    public string FormatChangeZoneMessage(uint ZoneId, string ZoneName)
    {
        return ((FormattableString)$"{ZoneId:X2}|{ZoneName}").ToString(CultureInfo.InvariantCulture);
    }
    private ILogOutput _logOutput;
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal bool WriteLogLineImpl(int ID, string line)
    {

        var timestamp = DateTime.Now;
        _logOutput?.WriteLine((FFXIV_ACT_Plugin.Logfile.LogMessageType)ID, timestamp, line);
        return true;
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
