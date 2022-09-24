using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using System.IO;
using System.Runtime.CompilerServices;

namespace RainbowMage.OverlayPlugin.EventSources {
    public class EnmityEventSource : EventSourceBase {
        private EnmityMemory memory;
        private List<EnmityMemory> memoryCandidates;
        private bool memoryValid = false;

        private const int MEMORY_SCAN_INTERVAL = 3000;

        // General information about the target, focus target, hover target.  Also, enmity entries for main target.
        private const string EnmityTargetDataEvent = "EnmityTargetData";
        // All of the mobs with aggro on the player.  Equivalent of the sidebar aggro list in game.
        private const string EnmityAggroListEvent = "EnmityAggroList";
        // TargetableEnemies
        private const string TargetableEnemiesEvent = "TargetableEnemies";
        // State of combat, both act and game.
        private const string InCombatEvent = "InCombat";

        [Serializable]
        internal class InCombatDataObject {
            public string type = InCombatEvent;
            public bool inACTCombat = false;
            public bool inGameCombat = false;
        };

        private InCombatDataObject sentCombatData;

        // Unlike "sentCombatData" which caches sent data, this variable caches each update.
        private bool lastInGameCombat = false;
        private const int endEncounterOutOfCombatDelayMs = 5000;
        private CancellationTokenSource endEncounterToken;

        public BuiltinEventConfig Config { get; set; }

        public event EventHandler<CombatStatusChangedArgs> CombatStatusChanged;

        public EnmityEventSource(TinyIoCContainer container) : base(container) {
            var repository = container.Resolve<FFXIVRepository>();

            try {
                PickMemoryCandidates(repository);
            }
            catch (FileNotFoundException) {
                // The FFXIV plugin isn't present.
            }

            RegisterEventTypes(new List<string> {
                EnmityTargetDataEvent, EnmityAggroListEvent, TargetableEnemiesEvent
            });
            RegisterCachedEventType(InCombatEvent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void PickMemoryCandidates(FFXIVRepository repository) {
            if (repository.GetLanguage() == FFXIV_ACT_Plugin.Common.Language.Chinese) {
                memoryCandidates = new List<EnmityMemory>() { new EnmityMemory61(container) };
            } else if (repository.GetLanguage() == FFXIV_ACT_Plugin.Common.Language.Korean) {
                memoryCandidates = new List<EnmityMemory>() { new EnmityMemory60(container) };
            } else {
                memoryCandidates = new List<EnmityMemory>() { new EnmityMemory62(container) };
            }
        }

        public override void LoadConfig(IPluginConfig cfg) {
            this.Config = container.Resolve<BuiltinEventConfig>();

            this.Config.EnmityIntervalChanged += (o, e) => {
                if (memory != null)
                    timer.Change(0, this.Config.EnmityIntervalMs);
            };
        }

        public override void Start() {
            // If we don't have anything to scan for, don't start.
            if (memoryCandidates == null) return;

            memoryValid = false;
            timer.Change(0, MEMORY_SCAN_INTERVAL);
        }

        public override void SaveConfig(IPluginConfig config) {
        }

        protected override void Update() {
            try {
#if TRACE
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif

                if (memory == null) {
                    foreach (var candidate in memoryCandidates) {
                        if (candidate.IsValid()) {
                            memory = candidate;
                            memoryCandidates = null;
                            break;
                        }
                    }
                }

                if (memory == null || !memory.IsValid()) {
                    if (memoryValid) {
                        timer.Change(MEMORY_SCAN_INTERVAL, MEMORY_SCAN_INTERVAL);
                        memoryValid = false;
                    }

                    return;
                } else if (!memoryValid) {
                    // Increase the update interval now that we found our memory
                    timer.Change(this.Config.EnmityIntervalMs, this.Config.EnmityIntervalMs);
                    memoryValid = true;
                }

                // Handle optional "end encounter of combat" logic.
                var inGameCombat = memory.GetInCombat();
                if (inGameCombat != lastInGameCombat) {
                    logger.Log(LogLevel.Debug, inGameCombat ? "Entered combat" : "Left combat");
                }

                // If we've transitioned to being out of combat, start a delayed task to end the ACT encounter.
                if (Config.EndEncounterOutOfCombat && lastInGameCombat && !inGameCombat) {
                    endEncounterToken = new CancellationTokenSource();
                    Task.Run(async delegate {
                        await Task.Delay(endEncounterOutOfCombatDelayMs, endEncounterToken.Token);
                        ActGlobals.oFormActMain.Invoke((Action)(() => {
                            ActGlobals.oFormActMain.EndCombat(true);
                        }));
                    });
                }
                // If combat starts again, cancel any outstanding tasks to stop the ACT encounter.
                // If the task has already run, this will not do anything.
                if (inGameCombat && endEncounterToken != null) {
                    endEncounterToken.Cancel();
                    endEncounterToken = null;
                }
                if (lastInGameCombat != inGameCombat) {
                    CombatStatusChanged?.Invoke(this, new CombatStatusChangedArgs(inGameCombat));
                }
                lastInGameCombat = inGameCombat;

                if (HasSubscriber(InCombatEvent)) {
                    var inACTCombat = Advanced_Combat_Tracker.ActGlobals.oFormActMain.InCombat;
                    if (sentCombatData == null || sentCombatData.inACTCombat != inACTCombat || sentCombatData.inGameCombat != inGameCombat) {
                        if (sentCombatData == null)
                            sentCombatData = new InCombatDataObject();
                        sentCombatData.inACTCombat = inACTCombat;
                        sentCombatData.inGameCombat = inGameCombat;
                        this.DispatchAndCacheEvent(JObject.FromObject(sentCombatData));
                    }
                }

                var targetData = HasSubscriber(EnmityTargetDataEvent);
                var aggroList = HasSubscriber(EnmityAggroListEvent);
                var targetableEnemies = HasSubscriber(TargetableEnemiesEvent);
                if (!targetData && !aggroList && !targetableEnemies)
                    return;

                var combatants = memory.GetCombatantList();

                combatants.RemoveAll((c) => c.Type != ObjectType.PC && c.Type != ObjectType.Monster);

                if (targetData) {
                    // See CreateTargetData() below
                    this.DispatchEvent(CreateTargetData(combatants));
                }
                if (aggroList) {
                    this.DispatchEvent(CreateAggroList(combatants));
                }
                if (targetableEnemies) {
                    this.DispatchEvent(CreateTargetableEnemyList(combatants));
                }
#if TRACE
                Log(LogLevel.Trace, "UpdateEnmity: {0}ms", stopwatch.ElapsedMilliseconds);
#endif
            }
            catch (Exception ex) {
                Log(LogLevel.Error, "UpdateEnmity: {0}", ex.ToString());
            }
        }

        [Serializable]
        internal class EnmityTargetDataObject {
            public string type = EnmityTargetDataEvent;
            public Combatant Target;
            public Combatant Focus;
            public Combatant Hover;
            public Combatant TargetOfTarget;
            public List<EnmityEntry> Entries;
        }

        [Serializable]
        internal class EnmityAggroListObject {
            public string type = EnmityAggroListEvent;
            public List<AggroEntry> AggroList;
            public List<EnmityHudEntry> EnmityHudList;
        }

        [Serializable]
        internal class TargetableEnemiesObject {
            public string type = TargetableEnemiesEvent;
            public List<TargetableEnemyEntry> TargetableEnemyList;
        }

        internal JObject CreateTargetData(List<Combatant> combatants) {
            var enmity = new EnmityTargetDataObject();
            try {
                var mychar = memory.GetSelfCombatant();
                enmity.Target = memory.GetTargetCombatant();
                if (enmity.Target != null) {
                    if (enmity.Target.TargetID > 0) {
                        enmity.TargetOfTarget = combatants.FirstOrDefault((Combatant x) => x.ID == (enmity.Target.TargetID));
                    }
                    enmity.Target.Distance = mychar.DistanceString(enmity.Target);
                    enmity.Target.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Target);

                    if (enmity.Target.Type == ObjectType.Monster) {
                        enmity.Entries = memory.GetEnmityEntryList(combatants);
                    }
                }

                enmity.Focus = memory.GetFocusCombatant();
                enmity.Hover = memory.GetHoverCombatant();

                if (mychar != null) {
                    if (enmity.Focus != null) {
                        enmity.Focus.Distance = mychar.DistanceString(enmity.Focus);
                        enmity.Focus.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Focus);
                    }
                    if (enmity.Hover != null) {
                        enmity.Hover.Distance = mychar.DistanceString(enmity.Hover);
                        enmity.Hover.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Hover);
                    }
                    if (enmity.TargetOfTarget != null) {
                        enmity.TargetOfTarget.Distance = mychar.DistanceString(enmity.TargetOfTarget);
                        enmity.TargetOfTarget.EffectiveDistance = mychar.EffectiveDistanceString(enmity.TargetOfTarget);
                    }
                }
            }
            catch (Exception ex) {
                this.logger.Log(LogLevel.Error, "CreateTargetData: {0}", ex);
            }
            return JObject.FromObject(enmity);
        }

        internal JObject CreateAggroList(List<Combatant> combatants) {
            var enmity = new EnmityAggroListObject();
            try {
                enmity.AggroList = memory.GetAggroList(combatants);
                enmity.EnmityHudList = memory.GetEnmityHudEntries();
            }
            catch (Exception ex) {
                this.logger.Log(LogLevel.Error, "CreateAggroList: {0}", ex);
            }
            return JObject.FromObject(enmity);
        }

        internal JObject CreateTargetableEnemyList(List<Combatant> combatants) {
            var enemies = new TargetableEnemiesObject();
            try {
                enemies.TargetableEnemyList = memory.GetTargetableEnemyList(combatants);
            }
            catch (Exception ex) {
                this.logger.Log(LogLevel.Error, "CreateTargetableEnemyList: {0}", ex);
            }
            return JObject.FromObject(enemies);
        }
    }

    public class CombatStatusChangedArgs : EventArgs {
        public bool InCombat { get; private set; }

        public CombatStatusChangedArgs(bool status) {
            InCombat = status;
        }
    }
}
