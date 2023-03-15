using System.Globalization;
using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Memory.MemoryReader;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.MinIoC;
using ACTWrapper = FFXIV_ACT_Plugin.Common.ACTWrapper;

namespace IINACT;

public class FfxivActPluginWrapper : IDisposable
{
    public IDataSubscription Subscription;
    public IDataRepository Repository;
    public ParseSettings ParseSettings = null!;
    public DataCollectionSettingsEventArgs DataCollectionSettings = null!;
    public readonly ProcessManager ProcessManager;

    private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivActPlugin;
    private readonly ParseMediator parseMediator;
    private readonly Configuration configuration;
    private readonly Dalamud.ClientLanguage dalamudClientLanguage;

    private Language clientLanguage =>
        dalamudClientLanguage switch
        {
            Dalamud.ClientLanguage.Japanese => Language.Japanese,
            Dalamud.ClientLanguage.English => Language.English,
            Dalamud.ClientLanguage.German => Language.German,
            Dalamud.ClientLanguage.French => Language.French,
            _ => Language.English
        };

    public FfxivActPluginWrapper(Configuration configuration, Dalamud.ClientLanguage dalamudClientLanguage)
    {
        this.configuration = configuration;
        this.dalamudClientLanguage = dalamudClientLanguage;

        ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        ffxivActPlugin.ConfigureIOC();
        OpcodeManager.Instance.SetRegion(GameRegion.Global);

        var iocContainer = ffxivActPlugin._iocContainer;
        iocContainer.Resolve<ResourceManager>().LoadResources();

        Subscription = iocContainer.Resolve<DataSubscription>();
        ffxivActPlugin.SetProperty("DataSubscription", Subscription);

        parseMediator = iocContainer.Resolve<ParseMediator>();

        ffxivActPlugin._dataCollection = iocContainer.Resolve<DataCollection>();

        var scanPackets = ffxivActPlugin._dataCollection._scanPackets;
        ProcessManager = scanPackets.GetField<ProcessManager>("_processManager");

        SetupActWrapper();

        SetupDataSubscription();

        SetupSettingsMediator();

        Repository = iocContainer.Resolve<IDataRepository>();
        ffxivActPlugin.SetProperty("DataRepository", Repository);

        ffxivActPlugin._dataCollection.StartMemory();

        ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
    }

    private void SetupSettingsMediator()
    {
        var settingsMediator = ffxivActPlugin._dataCollection._settingsMediator;

        DataCollectionSettings = new DataCollectionSettingsEventArgs
        {
            LogFileFolder = ActGlobals.oFormActMain.LogFilePath,
            UseSocketFilter = false,
            UseWinPCap = false,
            UseDeucalion = true,
            ProcessID = Environment.ProcessId
        };
        settingsMediator.DataCollectionSettings = DataCollectionSettings;

        var readProcesses = ProcessManager.GetField<ReadProcesses>("_readProcesses");
        readProcesses.Read64(true);

        ParseSettings = new ParseSettings()
        {
            DisableDamageShield = configuration.DisableDamageShield,
            DisableCombinePets = configuration.DisableCombinePets,
            LanguageID = clientLanguage,
            ParseFilter = (ParseFilterMode)configuration.ParseFilterMode,
            SimulateIndividualDoTCrits = configuration.SimulateIndividualDoTCrits,
            ShowRealDoTTicks = configuration.ShowRealDoTTicks,
            ShowDebug = configuration.ShowDebug,
            EnableBenchmarks = false
        };
        settingsMediator.ParseSettings = ParseSettings;

        settingsMediator.ProcessException = OnProcessException;

        var logOutput = ffxivActPlugin._dataCollection._logOutput;
        var logFormat = ffxivActPlugin._dataCollection._logFormat;

        var line = logFormat.FormatParseSettings(ParseSettings.DisableDamageShield, ParseSettings.DisableCombinePets,
                                                 ParseSettings.LanguageID, ParseSettings.ParseFilter,
                                                 ParseSettings.SimulateIndividualDoTCrits,
                                                 ParseSettings.ShowRealDoTTicks);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line);

        var line2 = logFormat.FormatMemorySettings(DataCollectionSettings.ProcessID,
                                                   DataCollectionSettings.LogFileFolder,
                                                   DataCollectionSettings.LogAllNetworkData,
                                                   DataCollectionSettings.DisableCombatLog,
                                                   DataCollectionSettings.NetworkIP, DataCollectionSettings.UseWinPCap,
                                                   DataCollectionSettings.UseSocketFilter,
                                                   DataCollectionSettings.UseDeucalion);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line2);

        logOutput.CallMethod("ConfigureLogFile", null);
        ActGlobals.oFormActMain.GetDateTimeFromLog = parseMediator.ParseLogDateTime;

        ProcessManager.Verify();
    }

    private void SetupActWrapper()
    {
        var logOutput = ffxivActPlugin._dataCollection._logOutput;
        var actWrapper = logOutput.GetField<ACTWrapper>("_actWrapper");

        actWrapper.TimeStampLen = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture).Length + 3;
        actWrapper.LogPathHasCharName = false;
        actWrapper.OverrideMainFormVisible = true;

        ACT_UIMods.UpdateACTTables(false);

        ActGlobals.oFormActMain.FfxivPlugin = ffxivActPlugin;
    }

    private void OFormActMain_BeforeLogLineRead(bool isImport, LogLineEventArgs logInfo)
    {
        (logInfo.logLine, logInfo.detectedType) =
            parseMediator.BeforeLogLineRead(isImport, logInfo.detectedTime, logInfo.logLine);
    }

    private void SetupDataSubscription()
    {
        ffxivActPlugin.DataSubscription.ZoneChanged += OnZoneChanged;
    }

    private static void OnZoneChanged(uint ZoneID, string ZoneName)
    {
        ActGlobals.oFormActMain.ChangeZone(ZoneName);
    }

    private static void OnProcessException(DateTime timestamp, string text) { }

    public void Dispose()
    {
        ffxivActPlugin.DeInitPlugin();
        ffxivActPlugin.Dispose();
    }
}
