using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
// For some reason this using is required by the github build?
using System.Runtime.CompilerServices;
using FFXIV_ACT_Plugin.Common;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using PluginCombatant = FFXIV_ACT_Plugin.Common.Models.Combatant;

namespace RainbowMage.OverlayPlugin.EventSources
{
    partial class FFXIVRequiredEventSource : EventSourceBase
    {
        private ReadOnlyCollection<uint> cachedPartyList = null;
        private List<uint> missingPartyMembers = new List<uint>();
        private bool ffxivPluginPresent = false;
        private static Dictionary<uint, string> StatusMap = new Dictionary<uint, string>
        {
            { 0, "Online" },
            { 12, "Busy" },
            { 15, "InCutscene" },
            { 17, "AFK" },
            { 21, "LookingToMeld" },
            { 22, "RP" },
            { 23, "LookingForParty" },
        };

        private const string OnlineStatusChangedEvent = "OnlineStatusChanged";
        private const string PartyChangedEvent = "PartyChanged";

        private FFXIVRepository repository;
        private ICombatantMemory combatantMemory;

        // Event Source

        public BuiltinEventConfig Config { get; set; }

        public FFXIVRequiredEventSource(TinyIoCContainer container) : base(container)
        {
            Name = "FFXIVRequired";
            var haveRepository = container.TryResolve(out repository);
            var haveCombatantMemory = container.TryResolve(out combatantMemory);

            if (haveRepository && haveCombatantMemory)
            {

                // These events need to deliver cached values to new subscribers.
                RegisterCachedEventTypes(new List<string> {
                    OnlineStatusChangedEvent,
                    PartyChangedEvent,
                });

                RegisterEventHandler("getLanguage", (msg) =>
                {
                    var lang = repository.GetLanguage();
                    var region = repository.GetMachinaRegion();
                    return JObject.FromObject(new
                    {
                        language = lang.ToString("g"),
                        languageId = lang.ToString("d"),
                        region = region.ToString("g"),
                        regionId = region.ToString("d"),
                    });
                });

                RegisterEventHandler("getVersion", (msg) =>
                {
                    var version = repository.GetOverlayPluginVersion();
                    return JObject.FromObject(new
                    {
                        version = version.ToString()
                    });
                });

                RegisterEventHandler("getCombatants", (msg) =>
                {
                    List<uint> ids = new List<uint>();

                    if (msg["ids"] != null)
                    {
                        foreach (var id in ((JArray)msg["ids"]))
                        {
                            ids.Add(id.ToObject<uint>());
                        }
                    }

                    List<string> names = new List<string>();

                    if (msg["names"] != null)
                    {
                        foreach (var name in ((JArray)msg["names"]))
                        {
                            names.Add(name.ToString());
                        }
                    }

                    List<string> props = new List<string>();

                    if (msg["props"] != null)
                    {
                        foreach (var prop in ((JArray)msg["props"]))
                        {
                            props.Add(prop.ToString());
                        }
                    }

                    var combatants = GetCombatants(ids, names, props);
                    return JObject.FromObject(new
                    {
                        combatants
                    });
                });

                try
                {
                    InitFFXIVIntegration();
                }
                catch (FileNotFoundException)
                {
                    // The FFXIV plugin hasn't been loaded.
                }

                container.Resolve<NetworkParser>().OnOnlineStatusChanged += (o, e) =>
                {
                    var obj = new JObject();
                    obj["type"] = OnlineStatusChangedEvent;
                    obj["target"] = e.Target;
                    obj["rawStatus"] = e.Status;
                    obj["status"] = StatusMap.ContainsKey(e.Status) ? StatusMap[e.Status] : "Unknown";

                    DispatchAndCacheEvent(obj);
                };
            }
        }

        private void InitFFXIVIntegration()
        {
            repository.RegisterPartyChangeDelegate((partyList, partySize) => DispatchPartyChangeEvent(partyList, partySize));
            ffxivPluginPresent = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private List<Dictionary<string, object>> GetCombatants(List<uint> ids, List<string> names, List<string> props)
        {
            List<Dictionary<string, object>> filteredCombatants = new List<Dictionary<string, object>>();
            var pluginCombatants = repository.GetCombatants();

            if (!combatantMemory.IsValid())
                return filteredCombatants;

            var memCombatants = combatantMemory.GetCombatantList();
            foreach (var combatant in memCombatants)
            {
                if (combatant.ID == 0)
                {
                    continue;
                }

                bool include = false;

                var combatantName = combatant.Name;

                if (ids.Count == 0 && names.Count == 0)
                {
                    include = true;
                }
                else
                {
                    foreach (var id in ids)
                    {
                        if (combatant.ID == id)
                        {
                            include = true;
                            break;
                        }
                    }

                    if (!include)
                    {
                        foreach (var name in names)
                        {
                            if (String.Equals(combatantName, name, StringComparison.InvariantCultureIgnoreCase))
                            {
                                include = true;
                                break;
                            }
                        }
                    }
                }

                if (include)
                {
                    var jObjCombatant = JObject.FromObject(combatant).ToObject<Dictionary<string, object>>();
                    var ID = Convert.ToUInt32(jObjCombatant["ID"]);

                    var pluginCombatant = pluginCombatants.FirstOrDefault((PluginCombatant c) => c.ID == ID);
                    if (pluginCombatant != null)
                    {
                        jObjCombatant["PartyType"] = GetPartyType(pluginCombatant);
                    }

                    // Handle 0xFFFE (outofrange1) and 0xFFFF (outofrange2) values for WorldID
                    var WorldID = Convert.ToUInt32(jObjCombatant["WorldID"]);
                    string WorldName = null;
                    if (WorldID < 0xFFFE)
                    {
                        WorldName = GetWorldName(WorldID);
                    }
                    jObjCombatant["WorldName"] = WorldName;

                    // If the request is filtering properties, remove them here
                    if (props.Count > 0)
                    {
                        jObjCombatant.Keys
                            .Where(k => !props.Contains(k))
                            .ToList()
                            .ForEach(k => jObjCombatant.Remove(k));
                    }

                    filteredCombatants.Add(jObjCombatant);
                }
            }

            return filteredCombatants;
        }

        struct PartyMember
        {
            // Player id in hex (for ease in matching logs).
            public string id;
            public string name;
            public uint worldId;
            // Raw job id.
            public int job;
            public int level;
            // In immediate party (true), vs in alliance (false).
            public bool inParty;
        }

        private int GetPartyType(PluginCombatant combatant)
        {
            // The PartyTypeEnum was renamed in 2.6.0.0 to work around that, we use reflection and cast the result to int.
            return (int)combatant.PartyType;
        }

        private string GetWorldName(uint WorldID)
        {
            var dict = repository.GetResourceDictionary(ResourceType.WorldList_EN);
            if (dict == null)
                return null;
            if (dict.TryGetValue(WorldID, out string WorldName))
                return WorldName;
            return null;
        }

        private void DispatchPartyChangeEvent(ReadOnlyCollection<uint> partyList, int partySize)
        {
            cachedPartyList = partyList;
            var combatants = repository.GetCombatants();
            if (combatants == null)
                return;

            // This is a bit of a hack.  The goal is to return a set of party
            // and alliance players, along with their jobs, ids, and names.
            //
            // |partySize| is only the size of your party, but the list of ids
            // contains ids from both party and alliance members.
            //
            // Additionally, there is a race where |combatants| is not updated
            // by the time this function is called.  However, this only seems
            // to happen in the case of disconnects and never when zoning in.
            // As a workaround, we use data retrieved from the NetworkAdd/RemoveCombatant
            // lines and keep track of all combatants which are missing from
            // the memory combatant list (the network lines are missing the
            // party status). Once per second (in Update()) we check if all
            // missing members have appeared and once they do, we dispatch
            // a PartyChangedEvent. This should result in immediate events
            // whenever the party changes and a second delayed event for each
            // change that updates the inParty field.
            //
            // Alternatives:
            // * poll GetCombatants until all party members exist (infinitely?)
            // * find better memory location of party list
            // * make this function only return the values from the delegate
            // * make callers handle this via calling GetCombatants explicitly

            // Build a lookup table of currently known combatants
            var lookupTable = new Dictionary<uint, PluginCombatant>();
            foreach (var c in combatants)
            {
                if (GetPartyType(c) != 0 /* None */)
                {
                    lookupTable[c.ID] = c;
                }
            }

            // Accumulate party members from cached info.  If they don't exist,
            // still send *something*, since it's better than nothing.
            List<PartyMember> result = new List<PartyMember>(24);
            lock (missingPartyMembers) lock (partyList)
                {
                    missingPartyMembers.Clear();

                    foreach (var id in partyList)
                    {
                        PluginCombatant c;
                        if (lookupTable.TryGetValue(id, out c))
                        {
                            result.Add(new PartyMember
                            {
                                id = $"{id:X}",
                                name = c.Name,
                                worldId = c.WorldID,
                                job = c.Job,
                                level = c.Level,
                                inParty = GetPartyType(c) == 1 /* Party */,
                            });
                        }
                        else
                        {
                            missingPartyMembers.Add(id);
                        }
                    }

                    if (missingPartyMembers.Count > 0)
                    {
                        Log(LogLevel.Debug, "Party changed event delayed until members are available");
                        return;
                    }
                }

            Log(LogLevel.Debug, "party list: {0}", JObject.FromObject(new { party = result }).ToString());

            DispatchAndCacheEvent(JObject.FromObject(new
            {
                type = PartyChangedEvent,
                party = result,
            }));
        }

        public override void LoadConfig(IPluginConfig config)
        {
            this.Config = container.Resolve<BuiltinEventConfig>();

            this.Config.UpdateIntervalChanged += (o, e) =>
            {
                this.Start();
            };
        }

        public override void SaveConfig(IPluginConfig config)
        {

        }

        public override void Start()
        {
            this.timer.Change(0, this.Config.UpdateInterval * 1000);
        }

        protected override void Update()
        {
            if (ffxivPluginPresent)
            {
                UpdateMissingPartyMembers();
            }
        }

        private void UpdateMissingPartyMembers()
        {
            lock (missingPartyMembers)
            {
                // If we are looking for missing party members, check if they are present by now.
                if (missingPartyMembers.Count > 0)
                {
                    var combatants = repository.GetCombatants();
                    if (combatants != null)
                    {
                        foreach (var c in combatants)
                        {
                            if (missingPartyMembers.Contains(c.ID))
                            {
                                missingPartyMembers.Remove(c.ID);
                            }
                        }
                    }

                    // Send an update event once all party members have been found.
                    if (missingPartyMembers.Count == 0)
                    {
                        DispatchPartyChangeEvent(cachedPartyList, 0);
                    }
                }
            }
        }
    }
}
