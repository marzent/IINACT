using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.InCombat;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;
using RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;
using RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud;
using RainbowMage.OverlayPlugin.MemoryProcessors.Target;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace RainbowMage.OverlayPlugin.EventSources {
    public class EnmityEventSource : EventSourceBase {
        private IInCombatMemory inCombatMemory;
        private ICombatantMemory combatantMemory;
        private ITargetMemory targetMemory;
        private IEnmityMemory enmityMemory;
        private IAggroMemory aggroMemory;
        private IEnmityHudMemory enmityHudMemory;

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
            inCombatMemory = container.Resolve<IInCombatMemory>();
            combatantMemory = container.Resolve<ICombatantMemory>();
            targetMemory = container.Resolve<ITargetMemory>();
            enmityMemory = container.Resolve<IEnmityMemory>();
            aggroMemory = container.Resolve<IAggroMemory>();
            enmityHudMemory = container.Resolve<IEnmityHudMemory>();

            RegisterEventTypes(new List<string> {
                EnmityTargetDataEvent, EnmityAggroListEvent, TargetableEnemiesEvent
            });
            RegisterCachedEventType(InCombatEvent);
        }

        public override void Start() {
            timer.Change(0, Config.EnmityIntervalMs);
        }

        public override void LoadConfig(IPluginConfig cfg) {
            Config = container.Resolve<BuiltinEventConfig>();

            Config.EnmityIntervalChanged += (o, e) => {
                timer.Change(0, Config.EnmityIntervalMs);
            };
        }

        public override void SaveConfig(IPluginConfig config) {
        }

        private void UpdateInCombat() {
            if (!inCombatMemory.IsValid())
                return;

            // Handle optional "end encounter of combat" logic.
            bool inGameCombat = inCombatMemory.GetInCombat();
            if (inGameCombat != lastInGameCombat) {
                logger.Log(LogLevel.Debug, inGameCombat ? "Entered combat" : "Left combat");
            }

            // If we've transitioned to being out of combat, start a delayed task to end the ACT encounter.
            if (Config.EndEncounterOutOfCombat && lastInGameCombat && !inGameCombat) {
                endEncounterToken = new CancellationTokenSource();
                Task.Run(async delegate {
                    await Task.Delay(endEncounterOutOfCombatDelayMs, endEncounterToken.Token);
                    ActGlobals.oFormActMain.EndCombat(true);
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
                bool inACTCombat = Advanced_Combat_Tracker.ActGlobals.oFormActMain.InCombat;
                if (sentCombatData == null || sentCombatData.inACTCombat != inACTCombat || sentCombatData.inGameCombat != inGameCombat) {
                    if (sentCombatData == null)
                        sentCombatData = new InCombatDataObject();
                    sentCombatData.inACTCombat = inACTCombat;
                    sentCombatData.inGameCombat = inGameCombat;
                    DispatchAndCacheEvent(JObject.FromObject(sentCombatData));
                }
            }
        }

        private void UpdateEnmity() {
            bool targetData = HasSubscriber(EnmityTargetDataEvent);
            bool aggroList = HasSubscriber(EnmityAggroListEvent);
            bool targetableEnemies = HasSubscriber(TargetableEnemiesEvent);
            if (!targetData && !aggroList && !targetableEnemies)
                return;

            var combatants = combatantMemory.GetCombatantList();

            combatants.RemoveAll((c) => c.Type != ObjectType.PC && c.Type != ObjectType.Monster);

            if (targetData) {
                // See CreateTargetData() below
                DispatchEvent(CreateTargetData(combatants));
            }
            if (aggroList) {
                DispatchEvent(CreateAggroList(combatants));
            }
            if (targetableEnemies) {
                DispatchEvent(CreateTargetableEnemyList(combatants));
            }
        }

        protected override void Update() {
            try {
#if TRACE
                var stopwatch = new Stopwatch();
                stopwatch.Start();
#endif
                UpdateInCombat();
                UpdateEnmity();
#if TRACE
                // Log(LogLevel.Trace, "UpdateEnmity: {0}ms", stopwatch.ElapsedMilliseconds);
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
                var mychar = combatantMemory.GetSelfCombatant();
                enmity.Target = targetMemory.GetTargetCombatant();
                if (enmity.Target != null) {
                    if (enmity.Target.TargetID > 0) {
                        enmity.TargetOfTarget = combatants.FirstOrDefault((Combatant x) => x.ID == (enmity.Target.TargetID));
                    }
                    enmity.Target.Distance = mychar.DistanceString(enmity.Target);
                    enmity.Target.EffectiveDistance = mychar.EffectiveDistanceString(enmity.Target);

                    if (enmity.Target.Type == ObjectType.Monster) {
                        enmity.Entries = enmityMemory.GetEnmityEntryList(combatants);
                    }
                }

                enmity.Focus = targetMemory.GetFocusCombatant();
                enmity.Hover = targetMemory.GetHoverCombatant();

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
                enmity.AggroList = aggroMemory.GetAggroList(combatants);
                enmity.EnmityHudList = enmityHudMemory.GetEnmityHudEntries();
            }
            catch (Exception ex) {
                this.logger.Log(LogLevel.Error, "CreateAggroList: {0}", ex);
            }
            return JObject.FromObject(enmity);
        }

        internal JObject CreateTargetableEnemyList(List<Combatant> combatants) {
            var enemies = new TargetableEnemiesObject();
            try {
                enemies.TargetableEnemyList = GetTargetableEnemyList(combatants);
            }
            catch (Exception ex) {
                this.logger.Log(LogLevel.Error, "CreateTargetableEnemyList: {0}", ex);
            }
            return JObject.FromObject(enemies);
        }

        public List<TargetableEnemyEntry> GetTargetableEnemyList(List<Combatant> combatantList) {
            var enemyList = new List<TargetableEnemyEntry>();
            for (int i = 0; i != combatantList.Count; ++i) {
                var combatant = combatantList[i];
                bool isHostile = (combatant.Type == ObjectType.Monster) && (combatant.MonsterType == MonsterType.Hostile);
                if (!isHostile || !combatant.IsTargetable) continue;
                var entry = new TargetableEnemyEntry {
                    ID = combatant.ID,
                    Name = combatant.Name,
                    CurrentHP = combatant.CurrentHP,
                    MaxHP = combatant.CurrentHP,
                    IsEngaged = (combatant.AggressionStatus >= AggressionStatus.EngagedPassive),
                    EffectiveDistance = combatant.RawEffectiveDistance
                };
                enemyList.Add(entry);
            }
            return enemyList;
        }
    }

    public class CombatStatusChangedArgs : EventArgs {
        public bool InCombat { get; private set; }

        public CombatStatusChangedArgs(bool status) {
            InCombat = status;
        }
    }

    [Serializable]
    public class TargetableEnemyEntry {
        public uint ID;
        public string Name;
        public int CurrentHP;
        public int MaxHP;
        public bool IsEngaged;
        public byte EffectiveDistance;
    }
}
