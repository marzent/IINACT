using System.Globalization;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Memory.MemoryReader;
using FFXIV_ACT_Plugin.Network;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using IINACT.Properties;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.MinIoC;
using ACTWrapper = FFXIV_ACT_Plugin.Common.ACTWrapper;

namespace IINACT;

public class FfxivActPluginWrapper {
    public FFXIVNetworkMonitor Monitor;
    public IDataSubscription Subscription;
    public IDataRepository Repository;
    public ParseSettings ParseSettings = null!;
    public DataCollectionSettingsEventArgs DataCollectionSettings = null!;
    public readonly ProcessManager ProcessManager;

    private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin _ffxivActPlugin;
    private readonly ParseMediator _parseMediator;
    private readonly DataCollection _dataCollection;

    public FfxivActPluginWrapper() {
        _ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        _ffxivActPlugin.CallMethod("ConfigureIOC", null);
        OpcodeManager.Instance.SetRegion(GameRegion.Global);

        var iocContainer = _ffxivActPlugin.GetField<Container>("_iocContainer");
        iocContainer.Resolve<ResourceManager>().LoadResources();

        Subscription = iocContainer.Resolve<DataSubscription>();
        _ffxivActPlugin.SetProperty("DataSubscription", Subscription);

        _parseMediator = iocContainer.Resolve<ParseMediator>();

        _dataCollection = iocContainer.Resolve<DataCollection>();
        _ffxivActPlugin.SetField("_dataCollection", _dataCollection);

        var scanPackets = _dataCollection.GetField<ScanPackets>("_scanPackets");
        ProcessManager = scanPackets.GetField<ProcessManager>("_processManager");

        SetupActWrapper();

        SetupDataSubscription();

        SetupSettingsMediator();

        Repository = iocContainer.Resolve<IDataRepository>();
        _ffxivActPlugin.SetProperty("DataRepository", Repository);

        Monitor = scanPackets.GetField<FFXIVNetworkMonitor>("_monitor");

        var rpcapEnabled = Settings.Default.RPcap;
        Monitor.RPCap.host = rpcapEnabled ? "127.0.0.1" : "";

        _dataCollection.StartMemory();

        ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
    }


    private void SetupSettingsMediator() {
        var settingsMediator = _dataCollection.GetField<SettingsMediator>("_settingsMediator");

        DataCollectionSettings = new DataCollectionSettingsEventArgs {
            LogFileFolder = ActGlobals.oFormActMain.LogFilePath,
            UseSocketFilter = false,
            UseWinPCap = true
        };
        settingsMediator.DataCollectionSettings = DataCollectionSettings;

        var readProcesses = ProcessManager.GetField<ReadProcesses>("_readProcesses");
        //wait for game
        while (!readProcesses.Read64(true).Any())
            Thread.Sleep(500);

        ParseSettings = new ParseSettings {
            LanguageID = (Language)Settings.Default.Language,
            ShowDebug = false
        };
        settingsMediator.ParseSettings = ParseSettings;

        settingsMediator.ProcessException = OnProcessException; 

        var logOutput = _dataCollection.GetField<LogOutput>("_logOutput");
        var logFormat = _dataCollection.GetField<LogFormat>("_logFormat");

        var line = logFormat.FormatParseSettings(ParseSettings.DisableDamageShield, ParseSettings.DisableCombinePets, ParseSettings.LanguageID, ParseSettings.ParseFilter, ParseSettings.SimulateIndividualDoTCrits, ParseSettings.ShowRealDoTTicks);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line);

        var line2 = logFormat.FormatMemorySettings(DataCollectionSettings.ProcessID, DataCollectionSettings.LogFileFolder, DataCollectionSettings.LogAllNetworkData, DataCollectionSettings.DisableCombatLog, DataCollectionSettings.NetworkIP, DataCollectionSettings.UseWinPCap, DataCollectionSettings.UseSocketFilter);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line2);

        logOutput.CallMethod("ConfigureLogFile", null);
        ActGlobals.oFormActMain.GetDateTimeFromLog = _parseMediator.ParseLogDateTime;

        ProcessManager.Verify();
    }

    private void SetupActWrapper() {
        var logOutput = _dataCollection.GetField<LogOutput>("_logOutput");
        var actWrapper = logOutput.GetField<ACTWrapper>("_actWrapper");

        actWrapper.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture).Length + 3;
        actWrapper.LogPathHasCharName = false;
        actWrapper.OverrideMainFormVisible = true;

        ACT_UIMods.UpdateACTTables(false);

        ActGlobals.oFormActMain.FfxivPlugin = _ffxivActPlugin;
    }

    private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo) {
        (logInfo.logLine, logInfo.detectedType) = _parseMediator.BeforeLogLineRead(isImport, logInfo.detectedTime, logInfo.logLine);
    }

    private void SetupDataSubscription() {
        _ffxivActPlugin.DataSubscription.ZoneChanged += OnZoneChanged;
    }

    private static void OnZoneChanged(uint ZoneID, string ZoneName) {
        ActGlobals.oFormActMain.ChangeZone(ZoneName);
    }

    private static void OnProcessException(DateTime timestamp, string text) {
    }

    public static void LogLineWritten(string text) {
        ActGlobals.oFormActMain.LogQueue.Enqueue(text);
        ActGlobals.oFormActMain.LogFileQueue.Enqueue(text);
    }

}
