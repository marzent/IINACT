using System.Globalization;
using Advanced_Combat_Tracker;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.Gui;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Memory.MemoryProcessors;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.MinIoC;
using ACTWrapper = FFXIV_ACT_Plugin.Common.ACTWrapper;

namespace IINACT;

public class FfxivActPluginWrapper : IDisposable
{
    private readonly Configuration configuration;
    private readonly ClientLanguage dalamudClientLanguage;
    private readonly ChatGui chatGui;
    private readonly Framework framework;

    private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivActPlugin;
    private readonly Container iocContainer;
    private ISettingsMediator settingsMediator = null!;
    private readonly ParseMediator parseMediator;
    
    private readonly IServerTimeProcessor serverTimeProcessor;
    private readonly IMobArrayProcessor mobArrayProcessor;
    private readonly IZoneMapProcessor zoneMapProcessor;
    private readonly CombatantManager combatantManager;
    private readonly IPlayerProcessor playerProcessor;
    private readonly IPartyProcessor partyProcessor;

    private readonly Thread scanThread;
    private readonly CancellationTokenSource cancellationTokenSource;
    private readonly CancellationToken cancellationToken;
    
    private readonly ILogOutput logOutput;
    private readonly ILogFormat logFormat;
    private readonly IProcessManager processManager;
    
    public DataCollectionSettingsEventArgs DataCollectionSettings = null!;
    public ParseSettings ParseSettings = null!;
    public readonly IDataRepository Repository;
    public readonly IDataSubscription Subscription;

    public FfxivActPluginWrapper(
        Configuration configuration, ClientLanguage dalamudClientLanguage, ChatGui chatGui, Framework framework)
    {
        this.configuration = configuration;
        this.dalamudClientLanguage = dalamudClientLanguage;
        this.chatGui = chatGui;
        this.framework = framework;

        ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        ffxivActPlugin.ConfigureIOC();
        OpcodeManager.Instance.SetRegion(GameRegion.Global);

        iocContainer = ffxivActPlugin._iocContainer;
        iocContainer.Resolve<ResourceManager>().LoadResources();

        Subscription = iocContainer.Resolve<DataSubscription>();
        ffxivActPlugin.SetProperty("DataSubscription", Subscription);

        parseMediator = iocContainer.Resolve<ParseMediator>();

        ffxivActPlugin._dataCollection = iocContainer.Resolve<DataCollection>();

        logOutput = ffxivActPlugin._dataCollection._logOutput;
        logFormat = ffxivActPlugin._dataCollection._logFormat;
        
        var scanMemory = (ScanMemory)ffxivActPlugin._dataCollection._scanMemory;

        processManager = scanMemory._processManager;
        serverTimeProcessor = scanMemory._serverTimeProcessor;
        mobArrayProcessor = scanMemory._mobArrayProcessor;
        zoneMapProcessor = scanMemory._zoneProcessor;
        combatantManager = (CombatantManager)scanMemory._combatantManager;
        playerProcessor = scanMemory._playerProcessor;
        partyProcessor = scanMemory._partyProcessor;

        cancellationTokenSource = new CancellationTokenSource();
        cancellationToken = cancellationTokenSource.Token;
        scanThread = new Thread(ScanMemory)
        {
            IsBackground = true
        };

        SetupActWrapper();

        SetupDataSubscription();

        SetupSettingsMediator();

        Repository = iocContainer.Resolve<IDataRepository>();
        ffxivActPlugin.SetProperty("DataRepository", Repository);

        ffxivActPlugin._dataCollection.StartMemory();

        this.chatGui.ChatMessage += OnChatMessage;
        ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
        scanThread.Start();
    }

    private Language ClientLanguage =>
        dalamudClientLanguage switch
        {
            Dalamud.ClientLanguage.Japanese => Language.Japanese,
            Dalamud.ClientLanguage.English => Language.English,
            Dalamud.ClientLanguage.German => Language.German,
            Dalamud.ClientLanguage.French => Language.French,
            _ => Language.English
        };

    public void Dispose()
    {
        chatGui.ChatMessage -= OnChatMessage;
        ActGlobals.oFormActMain.BeforeLogLineRead -= OFormActMain_BeforeLogLineRead;
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        ffxivActPlugin.DeInitPlugin();
        ffxivActPlugin.Dispose();
    }

    private void SetupSettingsMediator()
    {
        settingsMediator = ffxivActPlugin._dataCollection._settingsMediator;

        DataCollectionSettings = new DataCollectionSettingsEventArgs
        {
            LogFileFolder = ActGlobals.oFormActMain.LogFilePath,
            UseSocketFilter = false,
            UseWinPCap = false,
            UseDeucalion = true,
            ProcessID = Environment.ProcessId
        };
        settingsMediator.DataCollectionSettings = DataCollectionSettings;

        ParseSettings = new ParseSettings
        {
            DisableDamageShield = configuration.DisableDamageShield,
            DisableCombinePets = configuration.DisableCombinePets,
            LanguageID = ClientLanguage,
            ParseFilter = (ParseFilterMode)configuration.ParseFilterMode,
            SimulateIndividualDoTCrits = configuration.SimulateIndividualDoTCrits,
            ShowRealDoTTicks = configuration.ShowRealDoTTicks,
            ShowDebug = configuration.ShowDebug,
            EnableBenchmarks = false
        };
        settingsMediator.ParseSettings = ParseSettings;

        settingsMediator.ProcessException = OnProcessException;

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

        processManager.Verify();
    }

    private void OnChatMessage(
        XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var evenType = (uint)type;
        var player = sender.TextValue;
        var text = message.TextValue.Replace('\r', ' ').Replace('\n', ' ')
                                    .Replace('|', '❘');
        var line = logFormat.FormatChatMessage(evenType, player, text);
        
        logOutput.WriteLine(LogMessageType.ChatLog, DateTime.Now, line);
    }

    private void SetupActWrapper()
    {
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

    private static void OnZoneChanged(uint zoneId, string zoneName)
    {
        ActGlobals.oFormActMain.ChangeZone(zoneName);
    }

    private static void OnProcessException(DateTime timestamp, string text)
    {
        PluginLog.Debug(text);
    }
    
    public void CombatantRefresh()
    {
        if (DateTime.UtcNow.Subtract(combatantManager._lastCombatantRefresh).TotalMilliseconds < 100.0)
            return;

        combatantManager._lastCombatantRefresh = DateTime.UtcNow;
        framework.RunOnFrameworkThread(() =>
        {
            lock (combatantManager.CombatantLock)
            {
                mobArrayProcessor.Refresh();
                if (mobArrayProcessor.PrimaryPlayerPointer == nint.Zero)
                    return;
                var mobArray = combatantManager._mobArrayProcessor.MobArray;
                for (var i = 0; i < mobArray.Count; i++)
                {
                    combatantManager._combatantProcessor.RefreshCombatant(
                        combatantManager.Combatants[i], mobArray[i], true);
                }
            }
        }).Wait(cancellationToken);
    }

    private void ScanMemory()
    {
        while (!cancellationToken.WaitHandle.WaitOne(10))
        {
            try
            {
                if (settingsMediator.DataCollectionSettings == null || !processManager.Verify())
                    continue;

                serverTimeProcessor.Refresh();
                mobArrayProcessor.Refresh();
                if (mobArrayProcessor.PrimaryPlayerPointer == nint.Zero)
                    continue;

                var zoneId = zoneMapProcessor.ZoneID;
                zoneMapProcessor.Refresh();
                if (zoneMapProcessor.ZoneID == 0)
                    continue;

                if (zoneId != zoneMapProcessor.ZoneID)
                    framework.RunOnFrameworkThread(() => combatantManager.Rescan()).Wait(cancellationToken);
                else
                    CombatantRefresh();

                playerProcessor.Refresh();
                partyProcessor.Refresh();
            }
            catch (Exception ex) when (ex is ThreadAbortException or OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "[FFXIV_ACT_Plugin] ScanMemory failure");
                if (cancellationToken.WaitHandle.WaitOne(100))
                {
                    return;
                }
            }
        }
    } 
}
