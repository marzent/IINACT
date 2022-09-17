

//produced with ILSpy from ACT v3.6.0.275

namespace Advanced_Combat_Tracker {
    public class EncounterData {
        public delegate string ExportStringDataCallback(EncounterData Data, List<CombatantData> SelectiveAllies, string ExtraFormat);

        public class TextExportFormatter {
            public ExportStringDataCallback GetExportString;

            public string Label { get; }

            public string Description { get; }

            public string Name { get; }

            public TextExportFormatter(string Name, string Label, string Description, ExportStringDataCallback FormatterCallback) {
                this.Name = Name;
                this.Label = Label;
                this.Description = Description;
                GetExportString = FormatterCallback;
            }
        }

        public delegate string StringDataCallback(EncounterData Data);

        public delegate Color ColorDataCallback(EncounterData Data);

        public class ColumnDef {
            public StringDataCallback GetCellData;

            public StringDataCallback GetSqlData;

            public ColorDataCallback GetCellForeColor = (EncounterData Data) => Color.Transparent;

            public ColorDataCallback GetCellBackColor = (EncounterData Data) => Color.Transparent;

            public string SqlDataType { get; }

            public string SqlDataName { get; }

            public bool DefaultVisible { get; }

            public string Label { get; }

            public ColumnDef(string Label, bool DefaultVisible, string SqlDataType, string SqlDataName, StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback) {
                this.Label = Label;
                this.DefaultVisible = DefaultVisible;
                this.SqlDataType = SqlDataType;
                this.SqlDataName = SqlDataName;
                GetCellData = CellDataCallback;
                GetSqlData = SqlDataCallback;
            }
        }

        private class AllyObject {
            public CombatantData cd;

            public int allyVal;

            public AllyObject(CombatantData combatant) {
                cd = combatant;
                allyVal = 0;
            }

            public override string ToString() {
                return allyVal.ToString();
            }
        }

        public static Dictionary<string, TextExportFormatter> ExportVariables = new Dictionary<string, TextExportFormatter>();

        public static Dictionary<string, ColumnDef> ColumnDefs = new Dictionary<string, ColumnDef>();

        private string title = ActGlobals.Trans["encounterData-defaultEncounterName"];

        private bool sParsing;

        private bool ignoreEnemies;

        private bool alliesCached;

        private List<CombatantData> cAllies;

        private DateTime alliesLastCall = DateTime.Now;

        private bool alliesManual;

        private bool encIdCached;

        private string cEncId;

        private string zoneName;

        private List<DateTime> startTimes = new List<DateTime>();

        private List<DateTime> endTimes = new List<DateTime>();

        private HashSet<int> includedTimeSorters = new HashSet<int>();

        public HistoryRecord HistoryRecord { get; set; }

        public bool DuplicateDetection { get; set; }

        public static string[] ColTypeCollection {
            get {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs) {
                    array[num] = columnDef.Value.SqlDataType;
                    num++;
                }
                return array;
            }
        }

        public static string[] ColHeaderCollection {
            get {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs) {
                    array[num] = columnDef.Value.SqlDataName;
                    num++;
                }
                return array;
            }
        }

        public static string ColHeaderString => string.Join(",", ColHeaderCollection);

        public string[] ColCollection {
            get {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs) {
                    array[num] = columnDef.Value.GetSqlData(this);
                    num++;
                }
                return array;
            }
        }

        public ZoneData Parent { get; set; }

        public string CharName { get; set; }

        public string ZoneName {
            get {
                if (zoneName == ActGlobals.Trans["mergedEncounterTerm-all"] && Parent != null) {
                    return Parent.ZoneName;
                }
                return zoneName;
            }
            set => zoneName = value;
        }

        public bool Active { get; set; }

        public List<DateTime> StartTimes {
            get {
                for (var num = startTimes.IndexOf(DateTime.MaxValue); num >= 0; num = startTimes.IndexOf(DateTime.MaxValue)) {
                    startTimes.RemoveAt(num);
                }
                return startTimes;
            }
            set => startTimes = value;
        }

        public List<DateTime> EndTimes {
            get {
                for (var num = endTimes.IndexOf(DateTime.MinValue); num >= 0; num = endTimes.IndexOf(DateTime.MinValue)) {
                    endTimes.RemoveAt(num);
                }
                return endTimes;
            }
            set => endTimes = value;
        }

        public string Title {
            get => zoneName == ActGlobals.Trans["mergedEncounterTerm-all"] ? ActGlobals.Trans["mergedEncounterTerm-all"] : title;
            set => title = value;
        }

        public DateTime StartTime {
            get {
                var dateTime = DateTime.MaxValue;
                for (var i = 0; i < Items.Count; i++) {
                    var combatantData = Items.Values[i];
                    if (combatantData.StartTime < dateTime) {
                        dateTime = combatantData.StartTime;
                    }
                }
                return dateTime;
            }
        }

        public DateTime EndTime {
            get {
                if (ActGlobals.longDuration) {
                    var dateTime = DateTime.MinValue;
                    for (var i = 0; i < Items.Count; i++) {
                        var combatantData = Items.Values[i];
                        if (combatantData.EndTime > dateTime) {
                            dateTime = combatantData.EndTime;
                        }
                    }
                    return dateTime;
                }
                return ShortEndTime;
            }
        }

        public DateTime ShortEndTime {
            get {
                var dateTime = DateTime.MinValue;
                List<CombatantData> list = null;
                list = ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values));
                if (list.Count == 0) {
                    list = new List<CombatantData>(Items.Values);
                }
                foreach (var combatantData in list) {
                    if (combatantData.ShortEndTime > dateTime) {
                        dateTime = combatantData.ShortEndTime;
                    }
                }
                return dateTime;
            }
        }

        public TimeSpan Duration {
            get {
                if (StartTimes.Count > 1) {
                    try {
                        var result = default(TimeSpan);
                        for (var i = 0; i < StartTimes.Count; i++) {
                            if (EndTimes.Count == i) {
                                result += EndTime - StartTimes[i];
                            } else {
                                result += EndTimes[i] - StartTimes[i];
                            }
                        }
                        return result;
                    }
                    catch {
                        return TimeSpan.Zero;
                    }
                }
                if (EndTime > StartTime) {
                    return EndTime - StartTime;
                }
                return TimeSpan.Zero;
            }
        }

        public string DurationS => Duration.Hours == 0 ? $"{Duration.Minutes:00}:{Duration.Seconds:00}" : $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

        public long Damage => ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(t => t.Damage);

        public int AlliedKills => ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(combatantData => combatantData.Kills);

        public int AlliedDeaths => ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values))
            .Where(combatantData => !combatantData.Name.Contains(" ")).Sum(combatantData => combatantData.Deaths);

        public long Healed => ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(combatantData => combatantData.Healed);

        public double DPS => (double)Damage / Duration.TotalSeconds;

        public string EncId {
            get {
                if (encIdCached) {
                    return cEncId;
                }

                try {
                    cEncId = GetHashCode().ToString("x8");
                }
                catch (InvalidOperationException) {
                    return cEncId ?? "";
                }

                encIdCached = true;
                return cEncId;
            }
        }

        public int NumCombatants => Items.Count;

        public int NumAllies => GetAllies().Count;

        public int NumEnemies => NumCombatants - NumAllies;

        public SortedList<string, CombatantData> Items { get; set; } = new SortedList<string, CombatantData>();

        public List<LogLineEntry> LogLines { get; set; } = new List<LogLineEntry>();

        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        public string GetColumnByName(string name) => ColumnDefs.ContainsKey(name) ? ColumnDefs[name].GetCellData(this) : string.Empty;

        public EncounterData(string CharName, string ZoneName, bool IgnoreEnemies, ZoneData Parent) {
            sParsing = true;
            ignoreEnemies = IgnoreEnemies;
            this.CharName = CharName;
            zoneName = ZoneName;
            this.Parent = Parent;
        }

        public EncounterData(string CharName, string ZoneName, ZoneData Parent) {
            sParsing = false;
            ignoreEnemies = false;
            this.CharName = CharName;
            zoneName = ZoneName;
            this.Parent = Parent;
        }

        public void Trim() {
            Items.TrimExcess();
            for (var i = 0; i < Items.Count; i++) {
                Items.Values[i].Trim();
            }
        }

        public void AddCombatAction(MasterSwing action) {
            if (DuplicateDetection) {
                if (includedTimeSorters.Contains(action.TimeSorter)) {
                    return;
                }
                includedTimeSorters.Add(action.TimeSorter);
            }
            action.ParentEncounter = this;
            InvalidateCachedValues();
            var text = action.Attacker.ToUpper();
            var player = action.Victim.ToUpper();
            if (!sParsing || ActGlobals.oFormActMain.SelectiveListGetSelected(text) || (ActGlobals.oFormActMain.SelectiveListGetSelected(player) && !ignoreEnemies)) {
                if (!Items.TryGetValue(text, out var value)) {
                    value = new CombatantData(action.Attacker, this);
                    Items.Add(text, value);
                }
                value.AddCombatAction(action);
            }
            if (!sParsing || ActGlobals.oFormActMain.SelectiveListGetSelected(text) || (ActGlobals.oFormActMain.SelectiveListGetSelected(player) && !ignoreEnemies)) {
                AddReverseCombatAction(action);
            }
        }

        public void InvalidateCachedValues() {
            encIdCached = false;
        }

        public void InvalidateCachedValues(bool Recursive) {
            InvalidateCachedValues();
            if (!Recursive) return;
            for (var i = 0; i < Items.Count; i++) {
                Items.Values[i].InvalidateCachedValues(Recursive: true);
            }
        }

        private void AddReverseCombatAction(MasterSwing action) {
            var key = action.Victim.ToUpper();
            if (!Items.TryGetValue(key, out var value)) {
                value = new CombatantData(action.Victim, this);
                Items.Add(key, value);
            }
            value.AddReverseCombatAction(action);
        }

        public void EndCombat(bool Finalize) {
            lock (ActGlobals.ActionDataLock) {
                Active = false;
                EndTimes.Add(StartTimes[EndTimes.Count] < EndTime ? EndTime : StartTimes[EndTimes.Count]);
                if (!Finalize) return;
                Trim();
                Title = GetStrongestEnemy(ActGlobals.charName);
            }
        }

        public void SetAlliesUncached() {
            if (!alliesManual) {
                alliesCached = false;
            }
        }

        public void SetAllies(List<CombatantData> allies) {
            if (allies == null || allies.Count == 0) {
                alliesCached = false;
                alliesManual = false;
            } else {
                cAllies = allies;
                alliesCached = true;
                alliesManual = true;
            }
        }

        public List<CombatantData> GetAllies() {
            return GetAllies(allowLimited: false);
        }

        public List<CombatantData> GetAllies(bool allowLimited) {
            if (alliesCached || (DateTime.Now.Second == alliesLastCall.Second && allowLimited) || (cAllies != null && Active && Title == ActGlobals.Trans["mergedEncounterTerm-all"])) {
                return cAllies;
            }
            if (GetIgnoreEnemies()) {
                return new List<CombatantData>(Items.Values);
            }
            var combatant = GetCombatant(CharName);
            if (combatant == null) {
                return new List<CombatantData>();
            }
            var sortedList = new SortedList<string, AllyObject> { { combatant.Name.ToUpper(), new AllyObject(combatant) } };
            var flag = true;
            while (flag) {
                flag = false;
                for (var i = 0; i < sortedList.Count; i++) {
                    for (var j = 0; j < sortedList.Values[i].cd.Allies.Count; j++) {
                        var text = sortedList.Values[i].cd.Allies.Keys[j];
                        var num = sortedList.Values[i].cd.Allies.Values[j];
                        if (!sortedList.ContainsKey(text)) {
                            var combatant2 = GetCombatant(text);
                            if (combatant2 == null) {
                                continue;
                            }
                            sortedList.Add(text, new AllyObject(combatant2));
                            flag = true;
                        }
                        if (sortedList.Values[i].allyVal > 0) {
                            sortedList[text].allyVal += num;
                        } else {
                            sortedList[text].allyVal -= num;
                        }
                    }
                }
            }
            var list = new List<CombatantData>();
            var flag2 = sortedList[combatant.Name.ToUpper()].allyVal < 0;
            foreach (var item in sortedList) {
                if (flag2) {
                    if (item.Value.allyVal < 0) {
                        list.Add(item.Value.cd);
                    }
                } else if (item.Value.allyVal > 0) {
                    list.Add(item.Value.cd);
                }
            }
            for (var num2 = list.Count - 1; num2 >= 0; num2--) {
                if (list[num2] == null) {
                    list.RemoveAt(num2);
                }
            }
            cAllies = list;
            alliesCached = true;
            alliesLastCall = DateTime.Now;
            return cAllies;
        }

        public CombatantData? GetCombatant(string? Name) =>
            Name == null ? null : Items.TryGetValue(Name.ToUpper(), out var value) ? value : null;

        public int GetEncounterSuccessLevel() {
            if (sParsing && ignoreEnemies) {
                return 0;
            }
            var allies = GetAllies();
            if (allies.Count == 0) {
                return 0;
            }
            var combatant = GetCombatant(GetStrongestEnemy(CharName));
            if (combatant == null) {
                return 0;
            }
            var flag = combatant.Deaths > 0;
            var flag2 = allies.Any(combatantData => combatantData.Deaths == 0 && combatantData.Name != "Unknown" && !combatantData.Name.Contains(" "));
            if (flag && flag2) {
                return 1;
            }
            if (flag || flag2) {
                return 2;
            }
            return 3;
        }

        public string GetStrongestEnemy(string combatant) {
            if (sParsing && ignoreEnemies) {
                return ActGlobals.Trans["encounterData-defaultEncounterName"];
            }
            var list = new List<CombatantData>(Items.Values);
            var allies = GetAllies();
            if (allies.Count == 0) {
                return ActGlobals.Trans["encounterData-defaultEncounterName"];
            }
            for (var num = list.Count - 1; num >= 0; num--) {
                var item = list[num];
                if (allies.Contains(item)) {
                    list.RemoveAt(num);
                }
            }
            var list2 = list.Select(combatantData =>
                    new StrDouble(
                        Val: (combatantData.Deaths <= 0)
                            ? ((float)combatantData.DamageTaken)
                            : ((float)(combatantData.DamageTaken / combatantData.Deaths)), Name: combatantData.Name))
                .ToList();
            list2.Sort();
            list2.Reverse();
            return list2.Count > 0 ? list2[0].Name : null;
        }

        public string GetMaxHit(bool ShowType = true, bool UseSuffix = true) {
            var list = ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values));
            MasterSwing masterSwing = null!;
            var arg = string.Empty;
            foreach (var combatantData in list) {
                var attackType = combatantData.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], CombatantData.DamageTypeDataOutgoingDamage);
                if (attackType == null) {
                    continue;
                }
                foreach (var swing in attackType.Items) {
                    if (masterSwing != null && (long)swing.Damage <= (long)masterSwing.Damage) continue;
                    masterSwing = swing;
                    arg = combatantData.Name;
                }
            }
            if (masterSwing == null) {
                return string.Empty;
            }
            return ShowType
                ? $"{arg}-{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: true)}"
                : $"{arg}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: false)}";
        }

        public string GetMaxHeal(bool ShowType = true, bool CountWards = true, bool UseSuffix = true) {
            var list = ((!ignoreEnemies) ? GetAllies() : new List<CombatantData>(Items.Values));
            MasterSwing masterSwing = null;
            var arg = string.Empty;
            foreach (var combatantData in list) {
                var attackType = combatantData.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], CombatantData.DamageTypeDataOutgoingHealing);
                if (attackType == null) {
                    continue;
                }
                foreach (var swing in attackType.Items) {
                    if ((!CountWards && swing.DamageType == ActGlobals.Trans["specialAttackTerm-wardAbsorb"]) ||
                        (masterSwing != null && (long)swing.Damage <= (long)masterSwing.Damage)) continue;
                    masterSwing = swing;
                    arg = combatantData.Name;
                }
            }
            if (masterSwing == null) {
                return string.Empty;
            }
            return ShowType
                ? $"{arg}-{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: true)}"
                : $"{arg}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: false)}";
        }

        public bool GetIsSelective() {
            return sParsing;
        }

        public bool GetIgnoreEnemies() {
            return ignoreEnemies;
        }

        public override string ToString() {
            if (StartTime == DateTime.MaxValue) {
                return $"{Title} - [{DurationS}]";
            }
            return (DateTime.Now - StartTime).TotalHours > 12.0
                ? string.Format("{0} - [{1}] ({3}) {2}", Title, DurationS, StartTime.ToLongTimeString(),
                    StartTime.ToShortDateString())
                : $"{Title} - [{DurationS}] {StartTime.ToLongTimeString()}";
        }

        public override bool Equals(object? obj) {
            var encounterData = (EncounterData)obj!;
            var text = ToString();
            var value = encounterData.ToString();
            return text.Equals(value);
        }

        public override int GetHashCode() {
            var list = new List<CombatantData>(Items.Values);
            var num = list.Aggregate(0L, (current, combatantData) => current + combatantData.GetHashCode());
            return num.GetHashCode();
        }
    }

}
