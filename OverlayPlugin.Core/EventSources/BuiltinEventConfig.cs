using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.Overlays;

namespace RainbowMage.OverlayPlugin.EventSources {
    [Serializable]
    public class BuiltinEventConfig {
        public event EventHandler UpdateIntervalChanged;
        public event EventHandler EnmityIntervalChanged;
        public event EventHandler SortKeyChanged;
        public event EventHandler SortDescChanged;
        public event EventHandler UpdateDpsDuringImportChanged;
        public event EventHandler EndEncounterAfterWipeChanged;
        public event EventHandler EndEncounterOutOfCombatChanged;
        public event EventHandler LogLinesChanged;

        private int _updateInterval;
        public int UpdateInterval {
            get => this._updateInterval;
            set
            {
                if (this._updateInterval == value) return;
                this._updateInterval = value;
                UpdateIntervalChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private int _enmityIntervalMs;
        public int EnmityIntervalMs {
            get => this._enmityIntervalMs;
            set
            {
                if (this._enmityIntervalMs == value) return;
                this._enmityIntervalMs = value;
                EnmityIntervalChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private string _sortKey;
        public string SortKey {
            get => this._sortKey;
            set
            {
                if (this._sortKey == value) return;
                this._sortKey = value;
                SortKeyChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _sortDesc;
        public bool SortDesc {
            get => this._sortDesc;
            set
            {
                if (this._sortDesc == value) return;
                this._sortDesc = value;
                SortDescChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _updateDpsDuringImport;
        public bool UpdateDpsDuringImport {
            get => this._updateDpsDuringImport;
            set
            {
                if (this._updateDpsDuringImport == value) return;
                this._updateDpsDuringImport = value;
                UpdateDpsDuringImportChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _endEncounterAfterWipe;
        public bool EndEncounterAfterWipe {
            get => this._endEncounterAfterWipe;
            set
            {
                if (this._endEncounterAfterWipe == value) return;
                this._endEncounterAfterWipe = value;
                EndEncounterAfterWipeChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _endEncounterOutOfCombat;
        public bool EndEncounterOutOfCombat {
            get => this._endEncounterOutOfCombat;
            set
            {
                if (this._endEncounterOutOfCombat == value) return;
                this._endEncounterOutOfCombat = value;
                EndEncounterOutOfCombatChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool _logLines;
        public bool LogLines {
            get => _logLines;
            set
            {
                if (this._logLines == value) return;
                this._logLines = value;
                LogLinesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        // Data that overlays can save/load via event handlers.
        public Dictionary<string, JToken> OverlayData = new Dictionary<string, JToken>();

        public BuiltinEventConfig() {
            this._updateInterval = 1;
            this._enmityIntervalMs = 100;
            this._sortKey = "encdps";
            this._sortDesc = true;
            this._updateDpsDuringImport = false;
            this._endEncounterAfterWipe = true;
            this._endEncounterOutOfCombat = true;
            this._logLines = false;
        }

        public static BuiltinEventConfig LoadConfig(IPluginConfig config) {
            var result = new BuiltinEventConfig();

            if (!config.EventSourceConfigs.ContainsKey("MiniParse")) return result;
            var obj = config.EventSourceConfigs["MiniParse"];

            if (obj.TryGetValue("UpdateInterval", out var value)) {
                result._updateInterval = value.ToObject<int>();
            }

            if (obj.TryGetValue("EnmityIntervalMs", out value)) {
                result._enmityIntervalMs = value.ToObject<int>();
            }

            if (obj.TryGetValue("SortKey", out value)) {
                result._sortKey = value.ToString();
            }

            if (obj.TryGetValue("SortDesc", out value)) {
                result._sortDesc = value.ToObject<bool>();
            }

            if (obj.TryGetValue("UpdateDpsDuringImport", out value)) {
                result._updateDpsDuringImport = value.ToObject<bool>();
            }

            if (obj.TryGetValue("EndEncounterAfterWipe", out value)) {
                result._endEncounterAfterWipe = value.ToObject<bool>();
            }

            if (obj.TryGetValue("EndEncounterOutOfCombat", out value)) {
                result._endEncounterOutOfCombat = value.ToObject<bool>();
            }

            if (obj.TryGetValue("OverlayData", out value)) {
                result.OverlayData = value.ToObject<Dictionary<string, JToken>>();

                // Remove data for overlays that no longer exist.
                var overlayUuiDs = config.Overlays.OfType<MiniParseOverlayConfig>().Select(overlay => (overlay).Uuid.ToString()).ToList();

                var obsoleteKeys = (result.OverlayData.Keys.Where(key => key.StartsWith("overlay#") && key.Length >= 44)
                    .Select(key => new { key, uuid = key.Substring(8, 36) })
                    .Where(@t => !overlayUuiDs.Contains(@t.uuid))
                    .Select(@t => @t.key)).ToList();

                foreach (var key in obsoleteKeys) {
                    result.OverlayData.Remove(key);
                }
            }

            if (obj.TryGetValue("LogLines", out value)) {
                result._logLines = value.ToObject<bool>();
            }

            return result;
        }

        public void SaveConfig(IPluginConfig config) {
            var newObj = JObject.FromObject(this);
            if (config.EventSourceConfigs.ContainsKey("MiniParse") &&
                JToken.DeepEquals(config.EventSourceConfigs["MiniParse"], newObj)) return;
            config.EventSourceConfigs["MiniParse"] = newObj;
            config.MarkDirty();
        }
    }

    public enum MiniParseSortType {
        None,
        StringAscending,
        StringDescending,
        NumericAscending,
        NumericDescending
    }
}
