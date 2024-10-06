using Advanced_Combat_Tracker;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Logfile;
using Machina.FFXIV;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Machina.FFXIV.Headers.Opcodes;

namespace RainbowMage.OverlayPlugin
{
    /* Taken from FFIXV_ACT_Plugin.Logfile. Copy&pasted to avoid issues if the FFXIV plugin ever changes this enum. */
    public enum LogMessageType
    {
        LogLine,
        ChangeZone,
        ChangePrimaryPlayer,
        AddCombatant,
        RemoveCombatant,
        AddBuff,
        RemoveBuff,
        FlyingText,
        OutgoingAbility,
        IncomingAbility = 10,
        PartyList,
        PlayerStats,
        CombatantHP,
        ParsedPartyMember,
        NetworkStartsCasting = 20,
        NetworkAbility,
        NetworkAOEAbility,
        NetworkCancelAbility,
        NetworkDoT,
        NetworkDeath,
        NetworkBuff,
        NetworkTargetIcon,
        NetworkTargetMarker = 29,
        NetworkBuffRemove,
        NetworkGauge,
        NetworkWorld,
        Network6D,
        NetworkNameToggle,
        NetworkTether,
        NetworkLimitBreak,
        NetworkEffectResult,
        NetworkStatusList,
        NetworkUpdateHp,
        ChangeMap,
        Settings = 249,
        Process,
        Debug,
        PacketDump,
        Version,
        Error,
        Timer,

        // OverlayPlugin lines
        RegisterLogLine = 256,
        MapEffect,
        FateDirector,
        CEDirector,
        InCombat,
    }

    internal class FFXIVRepository
    {
        private readonly ILogger logger;
        private IDataRepository repository;
        private IDataSubscription subscription;

        public FFXIVRepository(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
        }

        internal static FFXIV_ACT_Plugin.FFXIV_ACT_Plugin GetPluginData()
        {
            return ActGlobals.oFormActMain.FfxivPlugin;
        }

        private IDataRepository GetRepository()
        {
            if (repository != null)
                return repository;

            var FFXIV = GetPluginData();
            if (FFXIV != null)
            {
                try
                {
                    repository = FFXIV.DataRepository;
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, Resources.FFXIVDataRepositoryException, ex);
                }
            }

            return repository;
        }

        private IDataSubscription GetSubscription()
        {
            if (subscription != null)
                return subscription;

            var FFXIV = GetPluginData();
            if (FFXIV != null)
            {
                try
                {
                    subscription = FFXIV.DataSubscription;
                }
                catch (Exception ex)
                {
                    logger.Log(LogLevel.Error, Resources.FFXIVDataSubscriptionException, ex);
                }
            }

            return subscription;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private Process GetCurrentFFXIVProcessImpl()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetCurrentFFXIVProcess();
        }

        [Obsolete("Subscribe to the ProcessChanged event instead (See RegisterProcessChangedHandler())")]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Process GetCurrentFFXIVProcess()
        {
            try
            {
                return GetCurrentFFXIVProcessImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        private bool IsFFXIVPluginPresentImpl()
        {
            return GetRepository() != null;
        }
        
        [MethodImpl(MethodImplOptions.NoInlining)]
        private uint? GetCurrentTerritoryIDImpl()
        {
            return GetRepository()?.GetCurrentTerritoryID();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint? GetCurrentTerritoryID()
        {
            try
            {
                return GetCurrentTerritoryIDImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool IsFFXIVPluginPresent()
        {
            try
            {
                return IsFFXIVPluginPresentImpl();
            }
            catch (FileNotFoundException)
            {
                return false;
            }
        }


        public Version GetOverlayPluginVersion()
        {
            return Assembly.GetExecutingAssembly().GetName().Version;
        }

        public Version GetPluginVersion()
        {
            return typeof(FFXIV_ACT_Plugin.FFXIV_ACT_Plugin).Assembly.GetName().Version;
        }

        public string GetPluginPath()
        {
            return typeof(IDataRepository).Assembly.Location;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private string GetGameVersionImpl()
        {
            return GetRepository()?.GetGameVersion();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetGameVersion()
        {
            try
            {
                return GetGameVersionImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint GetPlayerIDImpl()
        {
            var repo = GetRepository();
            if (repo == null) return 0;

            return repo.GetCurrentPlayerID();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public uint GetPlayerID()
        {
            try
            {
                return GetPlayerIDImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return 0;
            }
        }

        public string GetPlayerNameImpl()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            var playerId = repo.GetCurrentPlayerID();

            var playerInfo = repo.GetCombatantList().FirstOrDefault(x => x.ID == playerId);
            if (playerInfo == null) return null;

            return playerInfo.Name;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public IDictionary<uint, string> GetResourceDictionary(ResourceType resourceType)
        {
            try
            {
                return GetResourceDictionaryImpl(resourceType);
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        public IDictionary<uint, string> GetResourceDictionaryImpl(ResourceType resourceType)
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetResourceDictionary(resourceType);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public string GetPlayerName()
        {
            try
            {
                return GetPlayerNameImpl();
            }
            catch (FileNotFoundException)
            {
                // The FFXIV plugin isn't loaded
                return null;
            }
        }

        public ReadOnlyCollection<FFXIV_ACT_Plugin.Common.Models.Combatant> GetCombatants()
        {
            var repo = GetRepository();
            if (repo == null) return null;

            return repo.GetCombatantList();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public Language GetLanguage()
        {
            var repo = GetRepository();
            if (repo == null)
                return Language.English;
            return repo.GetSelectedLanguageID();
        }

        public string GetLocaleString()
        {
            switch (GetLanguage())
            {
                case Language.English:
                    return "en";
                case Language.French:
                    return "fr";
                case Language.German:
                    return "de";
                case Language.Japanese:
                    return "ja";
                case Language.Chinese:
                    return "cn";
                case Language.Korean:
                    return "ko";
                default:
                    return null;
            }
        }
        
        public static Dictionary<GameRegion, Dictionary<string, ushort>> GetMachinaOpcodes() =>
            (Dictionary<GameRegion, Dictionary<string, ushort>>)typeof(OpcodeManager).GetField(
                "_opcodes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(OpcodeManager.Instance);

        public GameRegion GetMachinaRegion() =>
            Machina.FFXIV.Headers.Opcodes.OpcodeManager.Instance.GameRegion;

        public DateTime EpochToDateTime(long epoch) =>
            Machina.Infrastructure.ConversionUtility.EpochToDateTime(epoch).ToLocalTime();
        
        /**
         * Convert a coordinate expressed as a uint16 to a float.
         *
         * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298
         */
        public static float ConvertUInt16Coordinate(ushort value)
        {
            return (value - 0x7FFF) / 32.767f;
        }

        /**
         * Convert a packet heading to an in-game headiung.
         *
         * When a heading is sent in certain packets, the heading is expressed as a uint16, where
         * 0=north and each increment is 1/65536 of a turn in the CCW direction.
         *
         * See https://github.com/ravahn/FFXIV_ACT_Plugin/issues/298
         */
        public static double ConvertHeading(ushort heading)
        {
            return heading
                   // Normalize to turns
                   / 65536.0
                   // Normalize to radians
                   * 2 * Math.PI
                   // Flip from 0=north to 0=south like the game uses
                   - Math.PI;
        }

        /**
         * Reinterpret a float as a UInt16. Some fields in Machina, such as Server_ActorCast.Rotation, are
         * marked as floats when they really should be UInt16.
         */
        public static ushort InterpretFloatAsUInt16(float value)
        {
            return BitConverter.ToUInt16(BitConverter.GetBytes(value), 0);
        }

        private ILogOutput _logOutput;

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal bool WriteLogLineImpl(uint ID, DateTime timestamp, string line)
        {
            if (_logOutput == null)
            {
                var plugin = GetPluginData();
                _logOutput = (ILogOutput)plugin._iocContainer.GetService(typeof(ILogOutput));
            }

            _logOutput?.WriteLine((FFXIV_ACT_Plugin.Logfile.LogMessageType)(int)ID, timestamp, line);
            return true;
        }

        // LogLineDelegate(uint EventType, uint Seconds, string logline);
        public void RegisterLogLineHandler(Action<uint, uint, string> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.LogLine += new LogLineDelegate(handler);
        }

        // NetworkReceivedDelegate(string connection, long epoch, byte[] message)
        public void RegisterNetworkParser(Action<string, long, byte[]> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.NetworkReceived += new NetworkReceivedDelegate(handler);
        }

        // PartyListChangedDelegate(ReadOnlyCollection<uint> partyList, int partySize)
        //
        // Details: partySize may differ from partyList.Count.
        // In non-cross world parties, players who are not in the same
        // zone count in the partySize but do not appear in the partyList.
        // In cross world parties, nobody will appear in the partyList.
        // Alliance data members show up in partyList but not in partySize.
        public void RegisterPartyChangeDelegate(Action<ReadOnlyCollection<uint>, int> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.PartyListChanged += new PartyListChangedDelegate(handler);
        }

        // ProcessChangedDelegate(Process process)
        public void RegisterProcessChangedHandler(Action<Process> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
            {
                sub.ProcessChanged += new ProcessChangedDelegate(handler);
                var repo = GetRepository();
                if (repo != null)
                {
                    var process = repo.GetCurrentFFXIVProcess();
                    if (process != null) handler(process);
                }
            }
        }
        
        public void RegisterZoneChangeDelegate(Action<uint, string> handler)
        {
            var sub = GetSubscription();
            if (sub != null)
                sub.ZoneChanged += new ZoneChangedDelegate(handler);
        }
        
        public DateTime GetServerTimestamp()
        {
            return GetRepository()?.GetServerTimestamp() ?? DateTime.Now;
        }
    }
}
