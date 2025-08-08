using System.Globalization;
using System.Runtime.InteropServices;
using Advanced_Combat_Tracker;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin.Services;
using FFXIV_ACT_Plugin;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using FFXIV_ACT_Plugin.Memory.MemoryProcessors;
using FFXIV_ACT_Plugin.Memory.MemoryReader;
using FFXIV_ACT_Plugin.Memory.Models;
using FFXIV_ACT_Plugin.Parse;
using FFXIV_ACT_Plugin.Resource;
using IINACT.Network;
using Machina.FFXIV;
using Machina.FFXIV.Headers.Opcodes;
using Microsoft.MinIoC;
using ACTWrapper = FFXIV_ACT_Plugin.Common.ACTWrapper;

namespace IINACT;

public partial class FfxivActPluginWrapper : IDisposable
{
    private readonly Configuration configuration;
    private readonly ClientLanguage dalamudClientLanguage;
    private readonly IChatGui chatGui;
    private readonly IFramework framework;
    private readonly ICondition condition;

    private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivActPlugin;
    private readonly Container iocContainer;
    private ISettingsMediator settingsMediator = null!;
    private readonly ParseMediator parseMediator;

    private readonly ServerTimeProcessor serverTimeProcessor;
    private readonly MobArrayProcessor mobArrayProcessor;
    private readonly IZoneMapProcessor zoneMapProcessor;
    private readonly CombatantManager combatantManager;
    private readonly IPlayerProcessor playerProcessor;
    private readonly IPartyProcessor partyProcessor;

    private readonly int mobArraySize;
    private readonly int combatantSize;
    private readonly int combatantBufferSize;
    private readonly nint mobData;
    private readonly nint[] mobDataOffsets;
    private readonly SemaphoreSlim refreshSemaphore = new(0);
    private byte mobDataAge = byte.MaxValue;

    private readonly Thread scanThread;
    private readonly CancellationTokenSource cancellationTokenSource;

    private readonly ILogOutput logOutput;
    private readonly ILogFormat logFormat;
    private readonly IProcessManager processManager;

    public DataCollectionSettingsEventArgs DataCollectionSettings = null!;
    public ParseSettings ParseSettings = null!;
    public readonly IDataRepository Repository;
    public readonly IDataSubscription Subscription;

    public unsafe FfxivActPluginWrapper(
        Configuration configuration, ClientLanguage dalamudClientLanguage, IChatGui chatGui, IFramework framework,
        ICondition condition)
    {
        this.configuration = configuration;
        this.dalamudClientLanguage = dalamudClientLanguage;
        this.chatGui = chatGui;
        this.framework = framework;
        this.condition = condition;

        ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        Plugin.Log.Information($"Initializing FFXIV_ACT_Plugin version {typeof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin).Assembly.GetName().Version}");
        ffxivActPlugin.ConfigureIOC();

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
        serverTimeProcessor = (ServerTimeProcessor)scanMemory._serverTimeProcessor;
        mobArrayProcessor = (MobArrayProcessor)scanMemory._mobArrayProcessor;
        zoneMapProcessor = scanMemory._zoneProcessor;
        combatantManager = (CombatantManager)scanMemory._combatantManager;
        playerProcessor = scanMemory._playerProcessor;
        partyProcessor = scanMemory._partyProcessor;

        SetupActWrapper();

        SetupDataSubscription();

        SetupSettingsMediator();

        Repository = iocContainer.Resolve<IDataRepository>();
        ffxivActPlugin.SetProperty("DataRepository", Repository);

        ffxivActPlugin._dataCollection.StartMemory();

        this.chatGui.ChatMessage += OnChatMessage;
        ActGlobals.oFormActMain.BeforeLogLineRead += OFormActMain_BeforeLogLineRead;
        serverTimeProcessor.ServerTime = DateTime.Now;

        cancellationTokenSource = new CancellationTokenSource();
        scanThread = new Thread(() => ScanMemory(cancellationTokenSource.Token))
        {
            IsBackground = true
        };
        scanThread.Start();

        mobArraySize = mobArrayProcessor._internalMmobArray.Length;
        var combatantProcessor = ((CombatantProcessor)combatantManager._combatantProcessor);
        combatantBufferSize = ((ReadCombatant)combatantProcessor._readCombatant)._buffer.Length;
        combatantSize = sizeof(CombatantStruct);
        mobData = Marshal.AllocHGlobal((mobArraySize * combatantSize) + (combatantBufferSize - combatantSize));
        mobDataOffsets = new nint[mobArraySize];
        for (var i = 0; i < mobArraySize; i++)
            mobDataOffsets[i] = mobData + (i * combatantSize);

        this.framework.Update += MobDataRefresh;
    }

    private Language ClientLanguage =>
        dalamudClientLanguage switch
        {
            Dalamud.Game.ClientLanguage.Japanese => Language.Japanese,
            Dalamud.Game.ClientLanguage.English => Language.English,
            Dalamud.Game.ClientLanguage.German => Language.German,
            Dalamud.Game.ClientLanguage.French => Language.French,
            _ => dalamudClientLanguage.ToString() == "ChineseSimplified" ? Language.Chinese : Language.English
        };

    public void Dispose()
    {
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        framework.Update -= MobDataRefresh;
        chatGui.ChatMessage -= OnChatMessage;
        ActGlobals.oFormActMain.BeforeLogLineRead -= OFormActMain_BeforeLogLineRead;
        ffxivActPlugin.DeInitPlugin();
        ffxivActPlugin.Dispose();
        Marshal.FreeHGlobal(mobData);
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
                                                   DataCollectionSettings.DisableCombatLog);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line2);

        logOutput.CallMethod("ConfigureLogFile", null);
        ActGlobals.oFormActMain.GetDateTimeFromLog = parseMediator.ParseLogDateTime;

        if (!processManager.Verify())
            throw new InvalidOperationException("Game offsets could not be found");
    }

    private void OnChatMessage(
        XivChatType type, int senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var evenType = (uint)type;
        var player = sender.TextValue;
        var text = message.TextValue.Replace('\r', ' ').Replace('\n', ' ')
                                    .Replace('|', '❘');
        var line = logFormat.FormatChatMessage(evenType, player, text);

        logOutput.WriteLine(LogMessageType.ChatLog, GameServerTime.CurrentServerTime, line);
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
        Plugin.Log.Debug($"[FFXIV_ACT_Plugin] {text}");
    }

    [SuppressGCTransition]
    [LibraryImport("SafeMemoryReader.dll")]
    private static partial int ReadMemory(nint dest, nint src, int size);

    private unsafe void MobDataRefresh(IFramework _)
    {
        if (settingsMediator.DataCollectionSettings == null)
            return;

        if (mobDataAge < 3 || (!condition[ConditionFlag.BoundByDuty56] && mobDataAge < 10))
        {
            mobDataAge++;
            if (mobArrayProcessor.PrimaryPlayerPointer == nint.Zero)
                return;

            refreshSemaphore.Release();
            return;
        }

        mobDataAge = 0;
        var mobArrayAddress = (ulong*)mobArrayProcessor._readMobArray.Read64();
        var mobArray = mobArrayProcessor._internalMmobArray;

        if (*mobArrayAddress == 0)
        {
            Array.Clear(mobArray);
            return;
        }

        ReadMemory(mobDataOffsets[0], (nint)(void*)*mobArrayAddress, combatantSize);
        mobArray[0] = mobDataOffsets[0];

        for (var i = 1; i < mobArraySize; i++)
        {
            if (*(mobArrayAddress + i) == 0)
            {
                mobArray[i] = nint.Zero;
                continue;
            }
            ReadMemory(mobDataOffsets[i], (nint)(void*)*(mobArrayAddress + i), combatantSize);
            mobArray[i] = mobDataOffsets[i];
        }

        refreshSemaphore.Release();
    }

    private void ScanMemory(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                refreshSemaphore.Wait(token);
                serverTimeProcessor.ServerTime = GameServerTime.CurrentServerTime;

                var zoneId = zoneMapProcessor.ZoneID;
                zoneMapProcessor.Refresh();
                if (zoneMapProcessor.ZoneID == 0)
                    continue;

                if (zoneId != zoneMapProcessor.ZoneID)
                    combatantManager.Rescan();
                else
                    combatantManager.Refresh();

                playerProcessor.Refresh();
                partyProcessor.Refresh();
            }
            catch (Exception ex) when (ex is ThreadAbortException or OperationCanceledException or ObjectDisposedException)
            {
                return;
            }
            catch (Exception ex)
            {
                Plugin.Log.Error(ex, "[FFXIV_ACT_Plugin] ScanMemory failure");
            }
        }
    }
}
