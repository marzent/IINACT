using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
// For some reason this using is required by the github build?
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using FFXIV_ACT_Plugin.Common;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge;
using RainbowMage.OverlayPlugin.MemoryProcessors.Party;
using RainbowMage.OverlayPlugin.NetworkProcessors;
using PluginCombatant = FFXIV_ACT_Plugin.Common.Models.Combatant;

namespace RainbowMage.OverlayPlugin.EventSources
{
    partial class FFXIVRequiredEventSource : EventSourceBase
    {
        private PartyListsStruct cachedPartyList = new PartyListsStruct();

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
        private const string JobGaugeChangedEvent = "JobGaugeChanged";

        private FFXIVRepository repository;
        private ICombatantMemory combatantMemory;
        private IPartyMemory partyMemory;
        private IJobGaugeMemory jobGaugeMemory;

        private CancellationTokenSource cancellationToken;

        // In milliseconds
        private const int PollingRate = 50;

        // Event Source

        public BuiltinEventConfig Config { get; set; }

        public FFXIVRequiredEventSource(TinyIoCContainer container) : base(container)
        {
            Name = "FFXIVRequired";
            var haveRepository = container.TryResolve(out repository);
            var haveCombatantMemory = container.TryResolve(out combatantMemory);

            if (haveRepository && haveCombatantMemory)
            {
                if (!container.TryResolve(out partyMemory))
                {
                    Log(LogLevel.Error, "Could not construct FFXIVRequiredEventSource: Missing partyMemory");
                }
                
                if (!container.TryResolve(out jobGaugeMemory))
                {
                    Log(LogLevel.Error, "Could not construct FFXIVRequiredEventSource: Missing jobGaugeMemory");
                }
                
                // These events need to deliver cached values to new subscribers.
                RegisterCachedEventTypes(new List<string>
                {
                    OnlineStatusChangedEvent,
                    PartyChangedEvent,
                    JobGaugeChangedEvent,
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

                container.Resolve<NetworkParser>().OnOnlineStatusChanged += (o, e) =>
                {
                    var obj = new JObject();
                    obj["type"] = OnlineStatusChangedEvent;
                    obj["target"] = e.Target;
                    obj["rawStatus"] = e.Status;
                    obj["status"] = StatusMap.ContainsKey(e.Status) ? StatusMap[e.Status] : "Unknown";

                    DispatchAndCacheEvent(obj);
                };
                
                cancellationToken = new CancellationTokenSource();

                Task.Run(PollJobGauge, cancellationToken.Token);
            }
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

            foreach (var combatant in memCombatants)
            {
                combatantMemory.ReturnCombatant(combatant);
            }

            return filteredCombatants;
        }
        
        public enum PartyType
        {
            Solo,
            Party,
            AllianceA,
            AllianceB,
            AllianceC,
            AllianceD,
            AllianceE,
            AllianceF,
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

            // @deprecated, please use partyType
            public bool inParty;
            public long contentId;
            // 0x1 = valid/present
            // 0x2 = unknown but set for some alliance members?
            // 0x4 = unknown but always set for current party and alliance members?
            // 0x8 = unknown but always set for current party?
            public byte flags;
            public uint objectId;
            public ushort territoryType;
            public string partyType;
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

        private void DispatchPartyChangeEvent()
        {
            // TODO: We know how to detect alliance A/B/C. Need to verify alliance D/E/F
            List<PartyMember> result = new List<PartyMember>(24);

            List<PartyType> remainingAlliances = new List<PartyType>() {
                PartyType.AllianceA,
                PartyType.AllianceB,
                PartyType.AllianceC,
                PartyType.AllianceD,
                PartyType.AllianceE,
                PartyType.AllianceF,
            };

            PartyType currentAlliance;
            if ((cachedPartyList.allianceFlags & 0x1) == 0)
            {
                if (cachedPartyList.memberCount <= 1)
                {
                    currentAlliance = PartyType.Solo;
                }
                else
                {
                    currentAlliance = PartyType.Party;
                }
            }
            else if ((cachedPartyList.currentPartyFlags & 0x100) == 0x100)
            {
                currentAlliance = remainingAlliances[0];
                remainingAlliances.RemoveAt(0);
            }
            else if ((cachedPartyList.currentPartyFlags & 0x1) == 0x1)
            {
                currentAlliance = remainingAlliances[1];
                remainingAlliances.RemoveAt(1);
            }
            else if ((cachedPartyList.currentPartyFlags & 0x10000) == 0x10000)
            {
                currentAlliance = remainingAlliances[2];
                remainingAlliances.RemoveAt(2);
            }
            else
            {
                currentAlliance = remainingAlliances[2];
                remainingAlliances.RemoveAt(2);
                Log(LogLevel.Warning, $"Could not detect player alliance, value {cachedPartyList.currentPartyFlags:X8}");
            }

            BuildPartyMemberResults(result, cachedPartyList.partyMembers, currentAlliance, true);
            BuildPartyMemberResults(result, cachedPartyList.alliance1Members, remainingAlliances[0], false);
            BuildPartyMemberResults(result, cachedPartyList.alliance2Members, remainingAlliances[1], false);
            BuildPartyMemberResults(result, cachedPartyList.alliance3Members, remainingAlliances[2], false);
            BuildPartyMemberResults(result, cachedPartyList.alliance4Members, remainingAlliances[3], false);
            BuildPartyMemberResults(result, cachedPartyList.alliance5Members, remainingAlliances[4], false);

            Log(LogLevel.Debug, "party list: {0}", JObject.FromObject(new { party = result }).ToString());

            DispatchAndCacheEvent(JObject.FromObject(new
            {
                type = PartyChangedEvent,
                party = result,
                partyInfo = new
                {
                    cachedPartyList.allianceFlags,
                    cachedPartyList.memberCount,
                    cachedPartyList.partyId,
                    cachedPartyList.partyId_2,
                    cachedPartyList.partyLeaderIndex,

                    cachedPartyList.currentPartyFlags,
                },
#if DEBUG
                debugPartyStruct = cachedPartyList,
#endif
            }));
        }

        private void BuildPartyMemberResults(List<PartyMember> result, PartyListEntry[] members, PartyType partyType, bool inParty)
        {
            foreach (var member in members)
            {
                if (member == null || (member.flags & 0x1) != 0x1)
                {
                    continue;
                }

                result.Add(new PartyMember
                {
                    id = $"{member.objectId:X}",
                    name = member.name,
                    worldId = member.homeWorld,
                    job = member.classJob,
                    level = member.level,
                    inParty = inParty,
                    contentId = member.contentId,
                    flags = member.flags,
                    objectId = member.objectId,
                    territoryType = member.territoryType,
                    partyType = partyType.ToString(),
                });
            }
        }

        public override void LoadConfig(IPluginConfig config)
        {
            this.Config = container.Resolve<BuiltinEventConfig>();

            this.Config.UpdateIntervalChanged += (o, e) => { this.Start(); };
        }

        public override void SaveConfig(IPluginConfig config) { }

        public override void Start()
        {
            // Use the config update interval, but clamp an upper limit at 5000ms
            var updateInterval = Math.Min(5000, this.Config.UpdateInterval * 1000);
            this.timer.Change(0, updateInterval);
        }
        
        public override void Stop()
        {
            base.Stop();
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
            }
        }
        
        private void PollJobGauge()
        {
            IJobGauge lastJobGauge = null;
            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;

                if (HasSubscriber(JobGaugeChangedEvent))
                {
                    var jobGauge = jobGaugeMemory.GetJobGauge();

                    if (jobGauge != null)
                    {
                        if (!jobGauge.Equals(lastJobGauge))
                        {
                            lastJobGauge = jobGauge;
                            var obj = JObject.FromObject(jobGauge);
                            obj["type"] = JobGaugeChangedEvent;

                            DispatchAndCacheEvent(obj);
                        }
                    }
                }

                // Wait for next poll
                var delay = PollingRate - (int)Math.Ceiling((DateTime.Now - now).TotalMilliseconds);
                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }
                else
                {
                    // If we're lagging enough to not have a sleep duration, delay by PollingRate to reduce lag
                    Thread.Sleep(PollingRate);
                }
            }
        }

        protected override void Update()
        {
            if (partyMemory != null)
            {
                UpdateParty();
            }
        }

        private void UpdateParty()
        {
            if (!partyMemory.IsValid())
            {
                return;
            }
            var newParty = partyMemory.GetPartyLists();

            // If we don't have anyone in the party, "correct" it by making sure the current player is in the party
            if (newParty.memberCount == 0)
            {
                var currentPlayer = combatantMemory.GetSelfCombatant();
                if (currentPlayer == null)
                {
                    return;
                }
                newParty.memberCount = 1;
                newParty.partyMembers = new PartyListEntry[] {
                    new PartyListEntry() {
                        x = currentPlayer.PosX,
                        y = currentPlayer.PosY,
                        z = currentPlayer.PosZ,
                        objectId = currentPlayer.ID,
                        currentHP = (uint) currentPlayer.CurrentHP,
                        maxHP = (uint) currentPlayer.MaxHP,
                        currentMP = (ushort) currentPlayer.CurrentMP,
                        maxMP = (ushort) currentPlayer.MaxMP,
                        homeWorld = currentPlayer.WorldID,
                        name = currentPlayer.Name,
                        classJob = currentPlayer.Job,
                        level = currentPlayer.Level,
                        flags = 0x13,
                    },
                };
                newParty.partyLeaderIndex = 0;
                combatantMemory.ReturnCombatant(currentPlayer);
            }

            var dispatchEvent = false;
            // If the party member count has changed, dispatch the event
            if (newParty.memberCount != cachedPartyList.memberCount)
            {
                dispatchEvent = true;
            }
            // Check each of party and alliances 1-5
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.partyMembers, newParty.partyMembers);
            }
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance1Members, newParty.alliance1Members);
            }
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance2Members, newParty.alliance2Members);
            }
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance3Members, newParty.alliance3Members);
            }
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance4Members, newParty.alliance4Members);
            }
            if (!dispatchEvent)
            {
                dispatchEvent = HasPartyCompChanged(cachedPartyList.alliance5Members, newParty.alliance5Members);
            }
            cachedPartyList = newParty;
            if (dispatchEvent)
            {
                DispatchPartyChangeEvent();
            }
        }

        private bool HasPartyCompChanged(PartyListEntry[] oldList, PartyListEntry[] newList)
        {
            // If the old list was null and the new list isn't null, they've changed, dispatch the event
            if (oldList == null && oldList != newList)
            {
                return true;
            }

            for (var i = 0; i < newList.Length; ++i)
            {
                var newMember = newList[i];
                var oldMember = oldList[i];
                if (newMember == null || oldMember == null)
                {
                    // If one of these is null and the other isn't, they've changed, dispatch the event
                    if (newMember != oldMember)
                    {
                        return true;
                    }
                    // Otherwise they're both null, we don't need to check them, continue
                    else
                    {
                        continue;
                    }
                }
                // If the party list is in a different order, dispatch the event
                if (newMember.objectId != oldMember.objectId)
                {
                    return true;
                }
                // If the party member's job or level changed, dispatch the event
                if (newMember.classJob != oldMember.classJob || newMember.level != oldMember.level)
                {
                    return true;
                }
                // If the member's flags have changed, dispatch the event. This covers players jumping zones properly.
                if (newMember.flags != oldMember.flags)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
