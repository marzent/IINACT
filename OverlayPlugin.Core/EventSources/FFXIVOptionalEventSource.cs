using System;
using System.Collections.Generic;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.EventSources
{
    /**
     * This EventSource contains lines that should work without FFXIV, but add additional information when FFXIV is present
     */
    partial class FFXIVOptionalEventSource : EventSourceBase
    {
        private const string LogLineEvent = "LogLine";
        private const string ChangeZoneEvent = "ChangeZone";
        private const string ChangeMapEvent = "ChangeMap";
        private const string GameVersionEvent = "GameVersion";
        private const string ChangePrimaryPlayerEvent = "ChangePrimaryPlayer";

        private FFXIVRepository repository;

        // Event Source

        public BuiltinEventConfig Config { get; set; }

        public FFXIVOptionalEventSource(TinyIoCContainer container) : base(container)
        {
            Name = "FFXIVOptional";
            var haveRepository = container.TryResolve(out repository);

            RegisterEventTypes(new List<string>
            {
                LogLineEvent,
            });

            if (haveRepository)
            {
                // These events need to deliver cached values to new subscribers.
                RegisterCachedEventTypes(new List<string>
                {
                    ChangePrimaryPlayerEvent,
                    ChangeZoneEvent,
                    ChangeMapEvent,
                    GameVersionEvent,
                });
            }

            ActGlobals.oFormActMain.BeforeLogLineRead += LogLineHandler;
        }

        private void StopACTCombat()
        {
            ActGlobals.oFormActMain.Invoke((Action)(() => { ActGlobals.oFormActMain.EndCombat(true); }));
        }

        private void LogLineHandler(bool isImport, LogLineEventArgs args)
        {
            if (isImport)
            {
                try
                {
                    LogMessageType lineType = (LogMessageType)args.detectedType;

                    // If an imported log has split the encounter, also split it while importing.
                    // TODO: should we also consider the current user's wipe config option here for splitting,
                    // even if the original log writer did not have it set to true?
                    if (lineType == LogMessageType.InCombat)
                    {
                        // @TODO: Should this be a customizable setting so that it can be changed per game, or somehow allow
                        // downstream plugins to override it to change it per game?
                        var line = args.originalLogLine.Split('|');

                        var inACTCombat = Convert.ToUInt32(line[2]);
                        if (inACTCombat == 0)
                        {
                            StopACTCombat();
                        }
                    }
                }
                catch
                {
                    return;
                }

                return;
            }

            try
            {
                LogMessageType lineType = (LogMessageType)args.detectedType;
                var line = args.originalLogLine.Split('|');
                
                switch (lineType)
                {
                    case LogMessageType.ChangeZone:
                        if (line.Length < 3) return;

                        var zoneID = Convert.ToUInt32(line[2], 16);
                        var zoneName = line[3];

                        DispatchAndCacheEvent(JObject.FromObject(new
                        {
                            type = ChangeZoneEvent,
                            zoneID,
                            zoneName,
                        }));
                        break;

                    case LogMessageType.ChangeMap:
                        if (line.Length < 6) return;

                        var mapID = Convert.ToUInt32(line[2], 10);
                        var regionName = line[3];
                        var placeName = line[4];
                        var placeNameSub = line[5];

                        DispatchAndCacheEvent(JObject.FromObject(new
                        {
                            type = ChangeMapEvent,
                            mapID,
                            regionName,
                            placeName,
                            placeNameSub
                        }));
                        break;

                    case LogMessageType.ChangePrimaryPlayer:
                        if (line.Length < 4) return;

                        var charID = Convert.ToUInt32(line[2], 16);
                        var charName = line[3];

                        DispatchAndCacheEvent(JObject.FromObject(new
                        {
                            type = ChangePrimaryPlayerEvent,
                            charID,
                            charName,
                        }));
                        break;

                    case LogMessageType.Network6D:
                        if (!Config.EndEncounterAfterWipe) break;
                        if (line.Length < 4) break;

                        // 4000000F is the new value for 6.2, 40000010 is the pre-6.2 value.
                        // When CN/KR is on 6.2, this can be removed.
                        if (line[3] == "40000010" || line[3] == "4000000F")
                        {
                            StopACTCombat();
                        }

                        break;
                    case LogMessageType.Process:
                        var gameVersion = repository.GetGameVersion();
                        DispatchAndCacheEvent(JObject.FromObject(new
                        {
                            type = GameVersionEvent,
                            gameVersion
                        }));
                        break;
                }

                // @TODO: Technically this event should work fine for any game, not just FFXIV.
                // Should it be moved to emit right after attempting the split instead?
                DispatchEvent(JObject.FromObject(new
                {
                    type = LogLineEvent,
                    line,
                    rawLine = args.originalLogLine,
                }));
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, "Failed to process log line: " + e.ToString());
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
            this.timer.Change(0, this.Config.UpdateInterval * 1000);
        }

        protected override void Update() { }
    }
}
