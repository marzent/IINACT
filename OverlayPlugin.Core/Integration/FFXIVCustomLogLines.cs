using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin
{
    class FFXIVCustomLogLines
    {
        private ILogger logger;
        private FFXIVRepository repository;
        private Dictionary<uint, ILogLineRegistryEntry> registry = new Dictionary<uint, ILogLineRegistryEntry>();

        private const uint registeredCustomLogLineID = 256;

        public FFXIVCustomLogLines(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();

            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = assembly.GetManifestResourceNames().Single(str => str.EndsWith("reserved_log_lines.json"));
                string reservedLogLinesJson;
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream)) {
                    reservedLogLinesJson = reader.ReadToEnd();
                }
                var reservedData = JsonConvert.DeserializeObject<List<ConfigReservedLogLine>>(reservedLogLinesJson);
                logger.Log(LogLevel.Warning, $"Parsing {reservedData.Count} reserved log line entries.");   
                foreach (var reservedDataEntry in reservedData)
                {
                    if (reservedDataEntry.Source == null || reservedDataEntry.Version == null)
                    {
                        logger.Log(LogLevel.Warning, $"Reserved log line entry missing Source or Version.");
                        continue;
                    }
                    if (reservedDataEntry.ID == null)
                    {
                        if (reservedDataEntry.StartID == null || reservedDataEntry.EndID == null)
                        {
                            logger.Log(LogLevel.Warning, $"Reserved log line entry missing StartID ({reservedDataEntry.StartID}) or EndID ({reservedDataEntry.EndID}).");
                            continue;
                        }
                        var Source = reservedDataEntry.Source;
                        var Version = reservedDataEntry.Version.Value;
                        var StartID = reservedDataEntry.StartID.Value;
                        var EndID = reservedDataEntry.EndID.Value;
                        logger.Log(LogLevel.Debug, $"Reserving log line entries {StartID}-{EndID} for Source {Source}, Version {Version}.");
                        for (uint ID = StartID; ID < EndID; ++ID)
                        {
                            if (registry.ContainsKey(ID))
                            {
                                logger.Log(LogLevel.Error, $"Reserved log line entry already registered ({ID}).");
                                continue;
                            }
                            registry[ID] = new LogLineRegistryEntry()
                            {
                                ID = ID,
                                Source = Source,
                                Version = Version,
                            };
                        }
                    }
                    else
                    {
                        var ID = reservedDataEntry.ID.Value;
                        if (registry.ContainsKey(ID))
                        {
                            logger.Log(LogLevel.Error, $"Reserved log line entry already registered ({ID}).");
                            continue;
                        }
                        var Source = reservedDataEntry.Source;
                        var Version = reservedDataEntry.Version.Value;
                        logger.Log(LogLevel.Debug, $"Reserving log line entry for ID {ID}, Source {Source}, Version {Version}.");
                        registry[ID] = new LogLineRegistryEntry()
                        {
                            ID = ID,
                            Source = Source,
                            Version = Version,
                        };
                    }
                }
            } catch(Exception ex)
            {
                logger.Log(LogLevel.Error, $"FFXIVCustomLogLines: Failed to load reserved log line: {ex}");
            }
        }

        public Func<string, bool> RegisterCustomLogLine(ILogLineRegistryEntry entry)
        {
            // Don't allow any attempt to write a custom log line with FFXIV_ACT_Plugin as the source.
            // This prevents a downstream plugin from attempting to register e.g. `00` lines by just pretending to be FFXIV_ACT_Plugin.
            if (entry.Source == "FFXIV_ACT_Plugin")
            {
                logger.Log(LogLevel.Warning, $"Attempted to register custom log line with reserved source.");
                return null;
            }
            var ID = entry.ID;
            if (registry.ContainsKey(ID))
            {
                // Allow re-registering the handler if the ID and Source match.
                // Implicitly don't allow re-registering the same handler if the Version changes to prevent log file confusion.
                if (!registry[ID].Equals(entry))
                {
                    logger.Log(LogLevel.Warning, $"Reserved log line entry already registered ({ID}).");
                    return null;
                }
            }
            // Write out that a new log line has been registered. Prevent newlines in the string input for sanity.
            var Source = entry.Source.Replace("\r", "\\r").Replace("\n", "\\n");
            repository.WriteLogLineImpl(registeredCustomLogLineID, $"{ID}|{Source}|{entry.Version}");
            registry[ID] = entry;
            return (line) => {
                if (line.Contains("\r") || line.Contains("\n"))
                {
                    logger.Log(LogLevel.Warning, $"Attempted to write custom log line with CR or LF with ID of {ID}");
                    return false;
                }
                repository.WriteLogLineImpl(ID, line);
                return true;
            };
        }
    }

    interface ILogLineRegistryEntry
    {
        uint ID { get; }
        string Source { get; }
        uint Version { get; }
    }

    class LogLineRegistryEntry : ILogLineRegistryEntry
    {
        public uint ID { get; set; }
        public string Source { get; set; }
        public uint Version { get; set; }

        public override string ToString()
        {
            return Source + "|" + ID + "|" + Version;
        }

        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
            {
                return false;
            }

            var otherEntry = (ILogLineRegistryEntry)obj;

            return ID == otherEntry.ID && Source == otherEntry.Source;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 31 + ID.GetHashCode();
            hash = hash * 31 + Source.GetHashCode();
            return hash;
        }
    }

    internal interface IConfigReservedLogLine
    {
        uint? ID { get; }
        uint? StartID { get; }
        uint? EndID { get; }
        string Source { get; }
        uint? Version { get; }
    }

    [JsonObject(NamingStrategyType = typeof(Newtonsoft.Json.Serialization.DefaultNamingStrategy))]
    internal class ConfigReservedLogLine : IConfigReservedLogLine
    {
        public uint? ID { get; set; }
        public uint? StartID { get; set; }
        public uint? EndID { get; set; }
        public string Source { get; set; }
        public uint? Version { get; set; }
    }
}