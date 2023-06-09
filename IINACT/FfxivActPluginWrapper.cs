using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Advanced_Combat_Tracker;
using Dalamud;
using Dalamud.Game;
using Dalamud.Game.ClientState.Conditions;
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
using FFXIV_ACT_Plugin.Memory.MemoryReader;
using FFXIV_ACT_Plugin.Memory.Models;
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
    private readonly Condition condition;

    private readonly FFXIV_ACT_Plugin.FFXIV_ACT_Plugin ffxivActPlugin;
    private readonly Container iocContainer;
    private ISettingsMediator settingsMediator = null!;
    private readonly ParseMediator parseMediator;

    private readonly IServerTimeProcessor serverTimeProcessor;
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
        Configuration configuration, ClientLanguage dalamudClientLanguage, ChatGui chatGui, Framework framework,
        Condition condition)
    {
        this.configuration = configuration;
        this.dalamudClientLanguage = dalamudClientLanguage;
        this.chatGui = chatGui;
        this.framework = framework;
        this.condition = condition;

        ffxivActPlugin = new FFXIV_ACT_Plugin.FFXIV_ACT_Plugin();
        ffxivActPlugin.ConfigureIOC();
        switch (dalamudClientLanguage)
        {
            case Dalamud.ClientLanguage.Japanese:
            case Dalamud.ClientLanguage.English:
            case Dalamud.ClientLanguage.German:
            case Dalamud.ClientLanguage.French:
                OpcodeManager.Instance.SetRegion(GameRegion.Global);
                break;
            default:
                OpcodeManager.Instance.SetRegion(GameRegion.Chinese);
                break;
        }

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

        cancellationTokenSource = new CancellationTokenSource();
        scanThread = new Thread(() => ScanMemory(cancellationTokenSource.Token))
        {
            IsBackground = true
        };
        scanThread.Start();

        mobArraySize = mobArrayProcessor._internalMmobArray.Length;
        var combatantProcessor = ((CombatantProcessor)combatantManager._combatantProcessor);
        combatantBufferSize = ((ReadCombatant)combatantProcessor._readCombatant)._buffer.Length;
        combatantSize = sizeof(Combatant64Struct);
        mobData = Marshal.AllocHGlobal((mobArraySize * combatantSize) + (combatantBufferSize - combatantSize));
        mobDataOffsets = new nint[mobArraySize];
        for (var i = 0; i < mobArraySize; i++)
            mobDataOffsets[i] = mobData + (i * combatantSize);

        this.framework.Update += MobDataRefresh;
    }

    private Language ClientLanguage =>
        dalamudClientLanguage switch
        {
            Dalamud.ClientLanguage.Japanese => Language.Japanese,
            Dalamud.ClientLanguage.English => Language.English,
            Dalamud.ClientLanguage.German => Language.German,
            Dalamud.ClientLanguage.French => Language.French,
            _ => Language.Chinese
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
                                                   DataCollectionSettings.DisableCombatLog,
                                                   DataCollectionSettings.NetworkIP, DataCollectionSettings.UseWinPCap,
                                                   DataCollectionSettings.UseSocketFilter
                                                  , DataCollectionSettings.UseDeucalion);
        logOutput.WriteLine(LogMessageType.Settings, DateTime.MinValue, line2);

        logOutput.CallMethod("ConfigureLogFile", null);
        ActGlobals.oFormActMain.GetDateTimeFromLog = parseMediator.ParseLogDateTime;

        if (!processManager.Verify())
            throw new InvalidOperationException("Game offsets could not be found");
    }
   
    private void OnChatMessage(
        XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        var evenType = (uint)type;
        var player = sender.TextValue;
        var text = message.TextValue.Replace('\r', ' ').Replace('\n', ' ')
                                    .Replace('|', '❘');
        var line = logFormat.FormatChatMessage(evenType, player, text);

        logOutput.WriteLine(LogMessageType.ChatLog, serverTimeProcessor.ServerTime, line);
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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ServerTimeRefresh()
    {
        serverTimeProcessor.Refresh();
        // Machina.FFXIV.Dalamud.DalamudClient.ServerTime = serverTimeProcessor.ServerTime;
    }

    private unsafe void MobDataRefresh(Framework _)
    {
        if (settingsMediator.DataCollectionSettings == null)
            return;

        if (mobDataAge < 3 || (!condition[ConditionFlag.BoundByDuty56] && mobDataAge < 10))
        {
            mobDataAge++;
            if (mobArrayProcessor.PrimaryPlayerPointer == nint.Zero)
            {
                ServerTimeRefresh();
                return;
            }
            refreshSemaphore.Release();
            return;
        }

        mobDataAge = 0;
        var mobArrayAddress = (ulong*)mobArrayProcessor._readMobArray.Read64();
        var mobArray = mobArrayProcessor._internalMmobArray;

        if (*mobArrayAddress == 0)
        {
            Array.Clear(mobArray);
            ServerTimeRefresh();
            return;
        }

        Buffer.MemoryCopy((void*)*mobArrayAddress, mobDataOffsets[0].ToPointer(), combatantSize, combatantSize);
        mobArray[0] = mobDataOffsets[0];

        for (var i = 1; i < mobArraySize; i++)
        {
            if (*(mobArrayAddress + i) == 0)
            {
                mobArray[i] = nint.Zero;
                continue;
            }
            Buffer.MemoryCopy((void*)*(mobArrayAddress + i), mobDataOffsets[i].ToPointer(), combatantSize, combatantSize);
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

                serverTimeProcessor.Refresh();

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
            catch (Exception ex) when (ex is ThreadAbortException or OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, "[FFXIV_ACT_Plugin] ScanMemory failure");
            }
        }
    }
}
