using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings;
using MachinaRegion = System.String;
using OpcodeName = System.String;
using OpcodeVersion = System.String;


namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    using Opcodes = Dictionary<MachinaRegion, Dictionary<OpcodeVersion, Dictionary<OpcodeName, OpcodeConfigEntry>>>;

    class OverlayPluginLogLines
    {
        public OverlayPluginLogLines(TinyIoCContainer container)
        {
            container.Register(new OverlayPluginLogLineConfig(container));
            container.Register(new LineMapEffect(container));
            container.Register(new LineFateControl(container));
            container.Register(new LineCEDirector(container));
            container.Register(new LineInCombat(container));
            container.Register(new LineCombatant(container));
            container.Register(new LineRSV(container));
            container.Register(new LineActorCastExtra(container));
            container.Register(new LineAbilityExtra(container));
            container.Register(new LineContentFinderSettings(container));
            container.Register(new LineNpcYell(container));
            container.Register(new LineBattleTalk2(container));
            container.Register(new LineCountdown(container));
            container.Register(new LineCountdownCancel(container));
            container.Register(new LineActorMove(container));
            container.Register(new LineActorSetPos(container));
            container.Register(new LineSpawnNpcExtra(container));
            container.Register(new LineActorControlExtra(container));
            container.Register(new LineActorControlSelfExtra(container));
        }
    }

    class OverlayPluginLogLineConfig
    {
        private Opcodes config = new();

        private ILogger logger;
        private FFXIVRepository repository;

        private int exceptionCount = 0;
        private const int maxExceptionsLogged = 3;

        public OverlayPluginLogLineConfig(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("opcodes.jsonc"));
                string jsonData;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    jsonData = reader.ReadToEnd();
                }

                config = JsonConvert.DeserializeAnonymousType(jsonData, config);
            }
            catch (Exception ex)
            {
                LogException($"FFXIVCustomLogLines: Failed to load reserved log line: {ex}");
            }
        }
        
        private void LogException(string message)
        {
            if (exceptionCount >= maxExceptionsLogged)
                return;
            exceptionCount++;
            logger.Log(LogLevel.Error, message);
        }

        private IOpcodeConfigEntry GetOpcode(string name, Opcodes opcodes, string version, string opcodeType, MachinaRegion machinaRegion)
        {
            if (opcodes == null)
                return null;

            if (opcodes.TryGetValue(machinaRegion, out var regionOpcodes))
            {
                if (regionOpcodes.TryGetValue(version, out var versionOpcodes))
                {
                    if (versionOpcodes.TryGetValue(name, out var opcode))
                    {
                        return opcode;
                    }

                    LogException($"No {opcodeType} opcode for game region {machinaRegion}, version {version}, opcode name {name}");
                }
                else
                {
                    if (repository.GetMachinaRegion().ToString() == machinaRegion)
                        LogException($"No {opcodeType} opcodes for game region {machinaRegion}, version {version}");
                }
            }
            else
            {
                LogException($"No {opcodeType} opcodes for game region {machinaRegion}");
            }

            return null;
        }
        
        public IOpcodeConfigEntry this[string name]
        {
            get
            {
                var machinaRegion = repository.GetMachinaRegion().ToString();
                return this[name, machinaRegion];
            }
        }

        public IOpcodeConfigEntry this[string name, MachinaRegion machinaRegion]
        {
            get
            {
                var version = repository.GetGameVersion();
                if (version == null)
                {
                    LogException("Could not detect game version from FFXIV_ACT_Plugin");
                    return null;
                }
                
                return GetOpcode(name, config, version, "resource", machinaRegion);
            }
        }
    }

    interface IOpcodeConfigEntry
    {
        uint opcode { get; }
        uint size { get; }
    }

    [JsonObject(NamingStrategyType = typeof(Newtonsoft.Json.Serialization.DefaultNamingStrategy))]
    class OpcodeConfigEntry : IOpcodeConfigEntry
    {
        public uint opcode { get; set; }
        public uint size { get; set; }
    }
}
