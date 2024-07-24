using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.WebSocket;

namespace RainbowMage.OverlayPlugin.EventSources
{
    partial class MiniParseEventSource : EventSourceBase
    {
        private List<string> importedLogs = new List<string>();

        private const string CombatDataEvent = "CombatData";
        private const string ImportedLogLinesEvent = "ImportedLogLines";
        private const string BroadcastMessageEvent = "BroadcastMessage";

        // Event Source

        public BuiltinEventConfig Config { get; set; }

        public MiniParseEventSource(TinyIoCContainer container) : base(container)
        {
            Name = "MiniParse";

            RegisterEventTypes(new List<string>
            {
                CombatDataEvent,
                ImportedLogLinesEvent,
                BroadcastMessageEvent,
            });

            RegisterEventHandler("saveData", (msg) =>
            {
                var key = msg["key"]?.ToString();
                if (key == null)
                    return null;

                Config.OverlayData[key] = msg["data"];
                return null;
            });

            RegisterEventHandler("loadData", (msg) =>
            {
                var key = msg["key"]?.ToString();
                if (key == null)
                    return null;

                if (!Config.OverlayData.ContainsKey(key))
                    return null;

                var ret = new JObject();
                ret["key"] = key;
                ret["data"] = Config.OverlayData[key];
                return ret;
            });

            RegisterEventHandler("say", (msg) =>
            {
                var text = msg["text"]?.ToString();
                if (text == null)
                    return null;

                ActGlobals.oFormActMain.TTS(text);
                return null;
            });

            RegisterEventHandler("playSound", (msg) =>
            {
                var file = msg["file"]?.ToString();
                if (file == null)
                    return null;

                ActGlobals.oFormActMain.PlaySound(file);
                return null;
            });

            RegisterEventHandler("broadcast", (msg) =>
            {
                if (!msg.ContainsKey("msg") || !msg.ContainsKey("source"))
                {
                    Log(LogLevel.Error,
                        "Called broadcast handler without specifying a source or message (\"source\" or \"msg\" property are missing).");
                    return null;
                }

                if (msg["source"].Type != JTokenType.String)
                {
                    Log(LogLevel.Error, "The source passed to the broadcast handler must be a string!");
                    return null;
                }

                DispatchEvent(JObject.FromObject(new
                {
                    type = BroadcastMessageEvent,
                    source = msg["source"],
                    msg = msg["msg"],
                }));

                return null;
            });

            RegisterEventHandler("openWebsiteWithWS", (msg) =>
            {
                var result = new JObject();

                if (!msg.ContainsKey("url"))
                {
                    Log(LogLevel.Error,
                        "Called openWebsiteWithWS handler without specifying a URL (\"url\" property is missing).");
                    result["$error"] =
                        "Called openWebsiteWithWS handler without specifying a URL (\"url\" property is missing).";
                    return result;
                }

                var wsServer = container.Resolve<ServerController>();

                if (!wsServer.Running)
                {
                    result["$error"] = "WSServer is not running";
                    return result;
                }

                try
                {
                    var url = wsServer.GetModernUrl(msg["url"].ToString());
                    var proc = new Process();
                    proc.StartInfo.Verb = "open";
                    proc.StartInfo.FileName = url;
                    proc.Start();
                }
                catch (Exception ex)
                {
                    Log(LogLevel.Error, $"Failed to to open website: {ex}");
                    result["$error"] = $"Failed to to open website: {ex}";
                    return result;
                }

                result["success"] = true;
                return result;
            });

            ActGlobals.oFormActMain.BeforeLogLineRead +=
                (bool isImport, LogLineEventArgs logInfo) =>
                {
                    if (isImport)
                    {
                        lock (importedLogs)
                        {
                            importedLogs.Add(logInfo.originalLogLine);
                        }
                    }
                };
        }

        private void StopACTCombat()
        {
            ActGlobals.oFormActMain.Invoke((Action)(() => { ActGlobals.oFormActMain.EndCombat(true); }));
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

        protected override void Update()
        {
            var importing = false;

            if (CheckIsActReady() && (!importing || this.Config.UpdateDpsDuringImport))
            {
                if (!HasSubscriber(CombatDataEvent))
                {
                    return;
                }

                // There used to be logic here to skip updating if the encounter info hasn't changed, but that's been commented
                // out since https://github.com/ngld/OverlayPlugin/commit/a56c44e85f0a5608d4185d67e13690dbad461523
                // Probably an unintentional change but it didn't break anything :shrug:

                DispatchEvent(this.CreateCombatData());
            }

            if (importing && HasSubscriber(ImportedLogLinesEvent))
            {
                List<string> logs = null;

                lock (importedLogs)
                {
                    if (importedLogs.Count > 0)
                    {
                        logs = importedLogs;
                        importedLogs = new List<string>();
                    }
                }

                if (logs != null)
                {
                    DispatchEvent(JObject.FromObject(new
                    {
                        type = ImportedLogLinesEvent,
                        logLines = logs
                    }));
                }
            }
        }

        internal JObject CreateCombatData()
        {
            if (!CheckIsActReady())
            {
                return new JObject();
            }

#if DEBUG
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var allies = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter.GetAllies();
            Dictionary<string, string> encounter = null;
            List<KeyValuePair<CombatantData, Dictionary<string, string>>> combatant = null;


            encounter = GetEncounterDictionary(allies);
            combatant = GetCombatantList(allies);

            if (encounter == null || combatant == null) return new JObject();

            JObject obj = new JObject();

            obj["type"] = "CombatData";
            obj["Encounter"] = JObject.FromObject(encounter);
            obj["Combatant"] = new JObject();

            if (this.Config.SortKey != null && this.Config.SortKey != "")
            {
                int factor = this.Config.SortDesc ? -1 : 1;
                var key = this.Config.SortKey;

                try
                {
                    combatant.Sort((a, b) =>
                    {
                        try
                        {
                            var aValue = float.Parse(a.Value[key]);
                            var bValue = float.Parse(b.Value[key]);

                            return factor * aValue.CompareTo(bValue);
                        }
                        catch (FormatException)
                        {
                            return 0;
                        }
                        catch (KeyNotFoundException)
                        {
                            return 0;
                        }
                    });
                }
                catch (Exception e)
                {
                    Log(LogLevel.Error, Resources.ListSortFailed, key, e);
                }
            }

            foreach (var pair in combatant)
            {
                JObject value = new JObject();
                foreach (var pair2 in pair.Value)
                {
                    value.Add(pair2.Key, Util.ReplaceNaNString(pair2.Value, "---"));
                }

                obj["Combatant"][pair.Key.Name] = value;
            }

            obj["isActive"] = ActGlobals.oFormActMain.ActiveZone.ActiveEncounter?.Active == true ? "true" : "false";

#if DEBUG
            stopwatch.Stop();
            Log(LogLevel.Trace, "CreateUpdateScript: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif
            return obj;
        }

        private List<KeyValuePair<CombatantData, Dictionary<string, string>>> GetCombatantList(
            IEnumerable<CombatantData> allies)
        {
#if TRACE
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            Dictionary<string, string> BuildCombatantDict(CombatantData ally)
            {
                var valueDict = new Dictionary<string, string>();
                foreach (var exportValuePair in CombatantData.ExportVariables)
                {
                    try
                    {
                        // NAME タグには {NAME:8} のようにコロンで区切られたエクストラ情報が必要で、
                        // プラグインの仕組み的に対応することができないので除外する
                        if (exportValuePair.Key.StartsWith("NAME"))
                        {
                            continue;
                        }

                        // ACT_FFXIV_Plugin が提供する LastXXDPS は、
                        // ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
                        // プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
                        if (exportValuePair.Key == "Last10DPS" || exportValuePair.Key == "Last30DPS" || exportValuePair.Key == "Last60DPS" || exportValuePair.Key == "Last180DPS")
                        {
                            if (!ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items.ContainsKey("All"))
                            {
                                valueDict.Add(exportValuePair.Key, "");
                                continue;
                            }
                        }

                        var value = exportValuePair.Value.GetExportString(ally, "");
                        valueDict.Add(exportValuePair.Key, value);
                    }
                    catch (Exception e)
                    {
                        Log(LogLevel.Debug, "GetCombatantList: {0}: {1}: {2}", ally.Name, exportValuePair.Key, e);
                    }
                }
                return valueDict;
            }

            var combatantList = allies.AsParallel()
                  .Select(combatantData => new KeyValuePair<CombatantData, Dictionary<string, string>>(
                              combatantData, BuildCombatantDict(combatantData)))
                  .ToList();

#if TRACE
            stopwatch.Stop();
            Log(LogLevel.Trace, "GetCombatantList: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif

            return combatantList;
        }

        private Dictionary<string, string> GetEncounterDictionary(List<CombatantData> allies)
        {
#if TRACE
            var stopwatch = new Stopwatch();
            stopwatch.Start();
#endif

            var encounterDict = new Dictionary<string, string>();
            foreach (var exportValuePair in EncounterData.ExportVariables)
            {
                try
                {
#if TRACE
                    stopwatch.Reset();
                    stopwatch.Start();
#endif

                    // ACT_FFXIV_Plugin が提供する LastXXDPS は、
                    // ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items に All キーが存在しない場合に、
                    // プラグイン内で例外が発生してしまい、パフォーマンスが悪化するので代わりに空の文字列を挿入する
                    if (exportValuePair.Key == "Last10DPS" ||
                        exportValuePair.Key == "Last30DPS" ||
                        exportValuePair.Key == "Last60DPS" ||
                        exportValuePair.Key == "Last180DPS")
                    {
                        if (!allies.All((ally) => ally.Items[CombatantData.DamageTypeDataOutgoingDamage].Items
                                                      .ContainsKey("All")))
                        {
                            encounterDict.Add(exportValuePair.Key, "");
                            continue;
                        }
                    }

                    var value = exportValuePair.Value.GetExportString(
                        ActGlobals.oFormActMain.ActiveZone.ActiveEncounter,
                        allies,
                        "");
                    encounterDict.Add(exportValuePair.Key, value);
                }
                catch (Exception e)
                {
                    Log(LogLevel.Debug, "GetEncounterDictionary: {0}: {1}", exportValuePair.Key, e);
                }
            }

#if TRACE
            stopwatch.Stop();
            Log(LogLevel.Trace, "GetEncounterDictionary: {0} msec", stopwatch.Elapsed.TotalMilliseconds);
#endif

            return encounterDict;
        }

        private static bool CheckIsActReady()
        {
            if (ActGlobals.oFormActMain?.ActiveZone?.ActiveEncounter != null &&
                EncounterData.ExportVariables != null &&
                CombatantData.ExportVariables != null)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
