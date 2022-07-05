using System.Diagnostics;
using static System.String;

//produced with ILSpy from ACT v3.6.0.275

namespace Advanced_Combat_Tracker {
    public class CombatantData : IComparable, IEquatable<CombatantData>, IComparable<CombatantData> {
        public delegate string ExportStringDataCallback(CombatantData Data, string ExtraFormat);

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

        public delegate string StringDataCallback(CombatantData Data);

        public delegate Color ColorDataCallback(CombatantData Data);

        public class ColumnDef {
            public StringDataCallback GetCellData;

            public StringDataCallback GetSqlData;

            public Comparison<CombatantData> SortComparer;

            public ColorDataCallback GetCellForeColor = (CombatantData Data) => Color.Transparent;

            public ColorDataCallback GetCellBackColor = (CombatantData Data) => Color.Transparent;

            public string SqlDataType { get; }

            public string SqlDataName { get; }

            public bool DefaultVisible { get; }

            public string Label { get; }

            public ColumnDef(string Label, bool DefaultVisible, string SqlDataType, string SqlDataName, StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback, Comparison<CombatantData> SortComparer) {
                this.Label = Label;
                this.DefaultVisible = DefaultVisible;
                this.SqlDataType = SqlDataType;
                this.SqlDataName = SqlDataName;
                GetCellData = CellDataCallback;
                GetSqlData = SqlDataCallback;
                this.SortComparer = SortComparer;
            }
        }

        public class DamageTypeDef {
            public string Label { get; }

            public int AllyValue { get; }

            public Color TypeColor { get; }

            public DamageTypeDef(string Label, int AllyValue, Color TypeColor) {
                this.Label = Label;
                this.AllyValue = AllyValue;
                this.TypeColor = TypeColor;
            }
        }

        public class DualComparison : IComparer<CombatantData> {
            private string sort1;

            private string sort2;

            public DualComparison(string Sort1, string Sort2) {
                sort1 = Sort1;
                sort2 = Sort2;
            }

            public int Compare(CombatantData? Left, CombatantData? Right) {
                var num = 0;
                Debug.Assert(Left != null, nameof(Left) + " != null");
                Debug.Assert(Right != null, nameof(Right) + " != null");
                if (ColumnDefs.ContainsKey(sort1)) {
                    num = ColumnDefs[sort1].SortComparer(Left, Right);
                }
                if (num == 0 && ColumnDefs.ContainsKey(sort2)) {
                    num = ColumnDefs[sort2].SortComparer(Left, Right);
                }
                if (num == 0) {
                    num = Left.Damage.CompareTo(Right.Damage);
                }
                return num;
            }
        }

        public static Dictionary<string, TextExportFormatter> ExportVariables = new Dictionary<string, TextExportFormatter>();

        public static Dictionary<string, ColumnDef> ColumnDefs = new Dictionary<string, ColumnDef>();

        public static Dictionary<string, DamageTypeDef> OutgoingDamageTypeDataObjects = new Dictionary<string, DamageTypeDef>();

        public static Dictionary<string, DamageTypeDef> IncomingDamageTypeDataObjects = new Dictionary<string, DamageTypeDef>();

        public static SortedDictionary<int, List<string>> SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>();

        public static SortedDictionary<int, List<string>> SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>();

        public static List<int> DamageSwingTypes = new List<int>();

        public static List<int> HealingSwingTypes = new List<int>();

        public static string DamageTypeDataNonSkillDamage = Empty;

        public static string DamageTypeDataOutgoingDamage = Empty;

        public static string DamageTypeDataOutgoingHealing = Empty;

        public static string DamageTypeDataIncomingDamage = Empty;

        public static string DamageTypeDataIncomingHealing = Empty;

        public static string DamageTypeDataOutgoingPowerReplenish = Empty;

        public static string DamageTypeDataOutgoingPowerDamage = Empty;

        public static string DamageTypeDataOutgoingCures = Empty;

        private readonly DamageTypeData outAll;

        private readonly DamageTypeData incAll;

        private bool deathsCached;

        private bool killsCached;

        private bool startTimeCached;

        private bool endTimeCached;

        private bool durationCached;

        private bool threatCached;

        private int cDeaths;

        private int cKills;

        private long cThreatDelta;

        private string cThreatStr;

        private DateTime cStartTime;

        private DateTime cEndTime;

        private TimeSpan cDuration;

        public EncounterData Parent { get; }

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

        public static string ColHeaderString => Join(",", ColHeaderCollection);

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

        public int Deaths {
            get {
                if (deathsCached) {
                    return cDeaths;
                }
                if (!AllInc.TryGetValue(ActGlobals.Trans["specialAttackTerm-killing"], out var value)) {
                    if (AllInc.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out value)) {
                        foreach (var t in value.Items) {
                            if (t.Damage == Dnum.Death) {
                                cDeaths++;
                            }
                        }
                    } else {
                        cDeaths = 0;
                    }
                } else {
                    cDeaths = value.Items.Count;
                }
                deathsCached = true;
                return cDeaths;
            }
        }

        public int Kills {
            get {
                if (killsCached) {
                    return cKills;
                }

                if (!AllOut.TryGetValue(ActGlobals.Trans["specialAttackTerm-killing"], out var value)) {
                    AllOut.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out value);
                }
                if (value == null) {
                    cKills = 0;
                    killsCached = true;
                    return cKills;
                }
                var flag = false;
                if (Parent.GetAllies(allowLimited: true) != null) {
                    flag = Parent != null && Parent.GetAllies(allowLimited: true).Contains(this);
                }
                cKills = 0;
                foreach (var t in value.Items) {
                    if (t.Damage == Dnum.Death && (flag || !t.Victim.Contains(" "))) {
                        cKills++;
                    }
                }
                killsCached = true;
                return cKills;
            }
        }

        public string Name { get; }

        public DateTime StartTime {
            get {
                if (startTimeCached) {
                    return cStartTime;
                }
                cStartTime = outAll.StartTime;
                startTimeCached = true;
                return cStartTime;
            }
        }

        public DateTime EndTime {
            get {
                if (endTimeCached) {
                    return cEndTime;
                }
                cEndTime = outAll.EndTime;
                endTimeCached = true;
                return cEndTime;
            }
        }

        public DateTime ShortEndTime => Items[DamageTypeDataOutgoingDamage].EndTime;

        public DateTime EncStartTime => Parent.StartTime;

        public DateTime EncEndTime => Parent.EndTime;

        public TimeSpan Duration {
            get {
                if (Parent.StartTimes.Count > 1) {
                    if (durationCached) {
                        return cDuration;
                    }
                    var stopwatch = new Stopwatch();
                    stopwatch.Start();
                    if (!AllOut.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)) {
                        return TimeSpan.Zero;
                    }
                    value.Items.Sort(MasterSwing.CompareTime);
                    var list = new List<DateTime>();
                    var list2 = new List<DateTime>();
                    var list3 = new List<DateTime>(Parent.StartTimes);
                    var list4 = new List<DateTime>(Parent.EndTimes);
                    var num = 0;
                    if (list3.Count > list4.Count) {
                        list4.Add(Parent.EndTime);
                    }
                    for (var i = 0; i < list4.Count; i++) {
                        if (num < 0) {
                            num = 0;
                        }
                        if (i < Parent.EndTimes.Count && Parent.EndTimes[i] < value.Items[num].Time) {
                            continue;
                        }
                        for (var j = num; j < value.Items.Count; j++) {
                            if (list.Count == list2.Count) {
                                if (i == Parent.StartTimes.Count - 1 && Parent.EndTimes.Count + 1 == Parent.StartTimes.Count) {
                                    if (value.Items[j].Time >= Parent.StartTimes[i] && value.Items[j].Time <= Parent.EndTime) {
                                        list.Add(value.Items[j].Time);
                                        num = j;
                                    }
                                } else if (value.Items[j].Time >= Parent.StartTimes[i] && value.Items[j].Time <= Parent.EndTimes[i]) {
                                    list.Add(value.Items[j].Time);
                                    num = j;
                                }
                            }
                            if (list.Count - 1 == list2.Count) {
                                MasterSwing? masterSwing = null;
                                for (var k = j; k < value.Items.Count; k++) {
                                    masterSwing = value.Items[k];
                                    if (k + 1 == value.Items.Count) {
                                        num = k - 1;
                                        break;
                                    }
                                    if (Parent.StartTimes.Count > i + 1 && value.Items[k + 1].Time >= Parent.StartTimes[i + 1]) {
                                        num = k - 1;
                                        break;
                                    }
                                }

                                Debug.Assert(masterSwing != null, nameof(masterSwing) + " != null");
                                list2.Add(masterSwing.Time);
                                break;
                            }
                            if (i < Parent.EndTimes.Count && value.Items[j].Time > Parent.EndTimes[i]) {
                                break;
                            }
                        }
                    }
                    if (list.Count - 1 == list2.Count) {
                        list2.Add(value.Items[^1].Time);
                    }
                    if (list.Count != list2.Count) {
                        throw new Exception(Format("Personal Duration failure.  StartTimes: {0}/{2} EndTimes: {1}/{3}", list.Count, list2.Count, Parent.StartTimes.Count, Parent.EndTimes.Count));
                    }
                    var timeSpan = default(TimeSpan);
                    for (var l = 0; l < list.Count; l++) {
                        timeSpan += list2[l] - list[l];
                    }
                    cDuration = timeSpan;
                    durationCached = true;
                    stopwatch.Stop();
                    return cDuration;
                }
                if (EndTime > StartTime) {
                    return EndTime - StartTime;
                }
                return TimeSpan.Zero;
            }
        }

        public string DurationS => Duration.Hours == 0 ? $"{Duration.Minutes:00}:{Duration.Seconds:00}" : $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

        public long Damage => Items[DamageTypeDataOutgoingDamage].Damage;

        public string DamagePercent {
            get {
                var result = "--";
                if (!Parent.GetAllies().Contains(this) || Parent.Damage <= 0) return result;
                var num = (int)((float)Damage / (float)Parent.Damage * 100f);
                if (num is <= -1 or >= 101) return result;
                result = num + "%";
                return result;
            }
        }

        public long PowerReplenish => Items[DamageTypeDataOutgoingPowerReplenish].Damage;

        public long PowerDamage => Items[DamageTypeDataOutgoingPowerDamage].Damage;

        public int Swings => Items[DamageTypeDataOutgoingDamage].Swings;

        public int CritHits => Items[DamageTypeDataOutgoingDamage].CritHits;

        public float CritDamPerc => Items[DamageTypeDataOutgoingDamage].CritPerc;

        public float CritHealPerc => Items[DamageTypeDataOutgoingHealing].CritPerc;

        public int CritHeals => Items[DamageTypeDataOutgoingHealing].CritHits;

        public int Heals => Items[DamageTypeDataOutgoingHealing].Swings;

        public int CureDispels => Items[DamageTypeDataOutgoingCures].Swings;

        public int Hits => Items[DamageTypeDataOutgoingDamage].Hits;

        public int Misses => Items[DamageTypeDataOutgoingDamage].Misses;

        public int Blocked => Items[DamageTypeDataOutgoingDamage].Blocked;

        public float ToHit {
            get {
                try {
                    float num = Hits;
                    float num2 = Swings;
                    return num / num2 * 100f;
                }
                catch {
                    return 0f;
                }
            }
        }

        public double DPS => (double)Damage / Duration.TotalSeconds;

        public double EncDPS => (double)Damage / Parent.Duration.TotalSeconds;

        public double ExtDPS => EncDPS;

        public double EncHPS {
            get {
                var num = Parent.Duration.TotalSeconds;
                return (double)Healed / num;
            }
        }

        public double ExtHPS => EncHPS;

        public long DamageTaken => Items[DamageTypeDataIncomingDamage].Damage;

        public long Healed => Items[DamageTypeDataOutgoingHealing].Damage;

        public long HealsTaken => Items[DamageTypeDataIncomingHealing].Damage;

        public string HealedPercent {
            get {
                var result = "--";
                if (!Parent.GetAllies().Contains(this) || Parent.Healed <= 0) return result;
                var num = (int)((float)Healed / (float)Parent.Healed * 100f);
                if (num is <= -1 or >= 101) return result;
                result = num + "%";
                return result;
            }
        }

        public SortedList<string, AttackType> AllOut => outAll.Items;

        public SortedList<string, AttackType> AllInc => incAll.Items;

        public Dictionary<string, DamageTypeData> Items { get; set; }

        public SortedList<string, int> Allies { get; set; }

        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        public CombatantData(string combatantName, EncounterData Parent) {
            Name = combatantName;
            Items = new Dictionary<string, DamageTypeData>();
            foreach (var outgoingDamageTypeDataObject in OutgoingDamageTypeDataObjects) {
                outAll = new DamageTypeData(Outgoing: true, outgoingDamageTypeDataObject.Key, this);
                Items.Add(outgoingDamageTypeDataObject.Key, outAll);
            }
            foreach (var incomingDamageTypeDataObject in IncomingDamageTypeDataObjects) {
                incAll = new DamageTypeData(Outgoing: false, incomingDamageTypeDataObject.Key, this);
                Items.Add(incomingDamageTypeDataObject.Key, incAll);
            }
            Allies = new SortedList<string, int>();
            this.Parent = Parent;
            InvalidateCachedValues();
        }

        public void InvalidateCachedValues() {
            durationCached = false;
            deathsCached = false;
            killsCached = false;
            startTimeCached = false;
            endTimeCached = false;
            threatCached = false;
        }

        public void InvalidateCachedValues(bool Recursive) {
            InvalidateCachedValues();
            if (!Recursive) {
                return;
            }
            foreach (var item in Items) {
                item.Value.InvalidateCachedValues(Recursive: true);
            }
        }

        public void Trim() {
            foreach (var item in Items) {
                item.Value.Trim();
            }
        }

        public void AddCombatAction(MasterSwing action) {
            durationCached = false;
            startTimeCached = false;
            endTimeCached = false;
            threatCached = false;
            killsCached = false;
            var combatant = action.Victim.ToUpper();
            if (!SwingTypeToDamageTypeDataLinksOutgoing.ContainsKey(action.SwingType)) {
                return;
            }
            var list = SwingTypeToDamageTypeDataLinksOutgoing[action.SwingType];
            foreach (var t in list) {
                var damageTypeData = Items[t];
                var allyValue = OutgoingDamageTypeDataObjects[damageTypeData.Type].AllyValue;
                ModAlly(combatant, allyValue);
                Items[t].AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);
                if (!ActGlobals.restrictToAll) {
                    Items[t].AddCombatAction(action, action.AttackType);
                }
            }
            outAll.AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);
            outAll.AddCombatAction(action, action.AttackType);
        }

        public void AddReverseCombatAction(MasterSwing action) {
            durationCached = false;
            deathsCached = false;
            startTimeCached = false;
            endTimeCached = false;
            var combatant = action.Attacker.ToUpper();
            if (!SwingTypeToDamageTypeDataLinksIncoming.ContainsKey(action.SwingType)) {
                return;
            }
            var list = SwingTypeToDamageTypeDataLinksIncoming[action.SwingType];
            foreach (var t in list) {
                var damageTypeData = Items[t];
                var allyValue = IncomingDamageTypeDataObjects[damageTypeData.Type].AllyValue;
                ModAlly(combatant, allyValue);
                Items[t].AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);
                if (!ActGlobals.restrictToAll) {
                    Items[t].AddCombatAction(action, action.AttackType);
                }
            }
            incAll.AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);
            incAll.AddCombatAction(action, action.AttackType);
        }

        public string GetMaxHit(bool ShowType = true, bool UseSuffix = true) {
            MasterSwing masterSwing = null!;
            var attackType = GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], DamageTypeDataOutgoingDamage);
            if (attackType != null) {
                foreach (var t in attackType.Items) {
                    if (masterSwing == null || (long)t.Damage > (long)masterSwing.Damage) {
                        masterSwing = t;
                    }
                }
            }
            if (masterSwing == null)
                return Empty;
            return ShowType
                ? $"{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: true)}"
                : $"{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: false)}";
        }

        public string GetMaxHeal(bool ShowType = true, bool CountWards = false, bool UseSuffix = true) {
            MasterSwing masterSwing = null!;
            var attackType = GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], DamageTypeDataOutgoingHealing);
            if (attackType != null) {
                foreach (var swing in attackType.Items.Where(swing =>
                             (CountWards || swing.DamageType != ActGlobals.Trans["specialAttackTerm-wardAbsorb"]) &&
                             (masterSwing == null || (long)swing.Damage > (long)masterSwing.Damage))) {
                    masterSwing = swing;
                }
            }
            if (masterSwing == null) {
                return Empty;
            }
            return ShowType
                ? $"{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: true)}"
                : $"{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, UseDecimals: true)}";
        }

        public int GetCombatantType() {
            if (!Parent.GetAllies().Contains(this)) return 0;
            var damage = Items[DamageTypeDataOutgoingDamage].Damage;
            var damage2 = Items[DamageTypeDataOutgoingHealing].Damage;
            var damage3 = Items[DamageTypeDataNonSkillDamage].Damage;
            var damage4 = Items[DamageTypeDataIncomingHealing].Damage;
            if (damage4 > damage / 3 && damage > damage2) {
                return 1;
            }
            if (damage2 > damage / 3 && damage2 > damage4) {
                return 2;
            }
            return damage3 > damage / 10 ? 3 : 4;
        }

        public long GetMaxHealth() {
            if (!AllInc.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)) {
                return DamageTaken - HealsTaken;
            }
            var list = new List<MasterSwing>(value.Items);
            list.Sort(MasterSwing.CompareTime);
            var num = 0L;
            var num2 = 0L;
            foreach (var swing in list) {
                if (DamageSwingTypes.Contains(swing.SwingType) && (long)swing.Damage > 0) {
                    num2 -= (long)swing.Damage;
                }
                if (HealingSwingTypes.Contains(swing.SwingType) && (long)swing.Damage > 0) {
                    num2 += swing.Damage;
                }
                if (num2 > 0) {
                    num2 = 0L;
                }
                if (num2 < num) {
                    num = num2;
                }
            }
            return Math.Abs(num);
        }

        public string GetColumnByName(string name) => ColumnDefs.ContainsKey(name) ? ColumnDefs[name].GetCellData(this) : Empty;

        public AttackType GetAttackType(string AttackTypeName, string Type) => Items[Type].Items.TryGetValue(AttackTypeName, out var value) ? value : null;

        public long GetThreatDelta(string DamageTypeDataLabel) {
            if (threatCached) {
                return cThreatDelta;
            }
            var num = 0L;
            var num2 = 0L;
            var num3 = 0;
            var num4 = 0;
            if (Items[DamageTypeDataLabel].Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)) {
                foreach (var masterSwing in value.Items) {
                    if ((long)masterSwing.Damage > 0) {
                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"]) {
                            num += masterSwing.Damage;
                        } else {
                            num2 += masterSwing.Damage;
                        }
                    } else if ((int)(long)masterSwing.Damage == (int)(long)Dnum.ThreatPosition) {
                        var length = masterSwing.Damage.DamageString.IndexOf(' ');
                        var num5 = int.Parse(masterSwing.Damage.DamageString[..length]);
                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"]) {
                            num3 += num5;
                        } else {
                            num4 += num5;
                        }
                    }
                }
            }
            cThreatDelta = 0L;
            cThreatDelta += num;
            cThreatDelta -= num2;
            threatCached = true;
            cThreatStr = $@"+({num3}){num}/-({num4}){num2}";
            return cThreatDelta;
        }

        public string GetThreatStr(string DamageTypeDataLabel) {
            if (threatCached) {
                return cThreatStr;
            }
            var num = 0L;
            var num2 = 0L;
            var num3 = 0;
            var num4 = 0;
            if (Items[DamageTypeDataLabel].Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)) {
                foreach (var masterSwing in value.Items) {
                    if ((long)masterSwing.Damage > 0) {
                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"]) {
                            num += masterSwing.Damage;
                        } else {
                            num2 += masterSwing.Damage;
                        }
                    } else if ((int)(long)masterSwing.Damage == (int)(long)Dnum.ThreatPosition) {
                        var length = masterSwing.Damage.DamageString.IndexOf(' ');
                        var num5 = int.Parse(masterSwing.Damage.DamageString[..length]);
                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"]) {
                            num3 += num5;
                        } else {
                            num4 += num5;
                        }
                    }
                }
            }
            cThreatDelta = 0L;
            cThreatDelta += num;
            cThreatDelta -= num2;
            threatCached = true;
            cThreatStr = Format("+({2}){0}/-({3}){1}", num, num2, num3, num4);
            return cThreatStr;
        }

        public int CompareTo(object? obj) => CompareTo((CombatantData?)obj);

        public override bool Equals(object? obj) => Name.ToLower().Equals(((CombatantData)obj!).Name.ToLower());

        public override int GetHashCode() => Items.Values.Aggregate(0L, (current, value) => current + value.GetHashCode()).GetHashCode();

        public override string ToString() {
            return Name;
        }

        public void ModAlly(string Combatant, int Mod) {
            if (Name == "Unknown" || Combatant == "UNKNOWN") return;
            if (!Allies.ContainsKey(Combatant))
                Allies.Add(Combatant, 0);

            if (Mod == 0) return;
            Allies[Combatant] += Mod;
            Parent.SetAlliesUncached();
        }

        public bool Equals(CombatantData? other) => string.Equals(Name, other!.Name, StringComparison.CurrentCultureIgnoreCase);

        public int CompareTo(CombatantData? other) {
            var num = 0;
            Debug.Assert(other != null, nameof(other) + " != null");
            if (ColumnDefs.ContainsKey(ActGlobals.eDSort)) {
                num = ColumnDefs[ActGlobals.eDSort].SortComparer(this, other);
            }
            if (num == 0 && ColumnDefs.ContainsKey(ActGlobals.eDSort2)) {
                num = ColumnDefs[ActGlobals.eDSort2].SortComparer(this, other);
            }
            if (num == 0) {
                num = Damage.CompareTo(other.Damage);
            }
            return num;
        }

        internal static int CompareDamageTakenTime(CombatantData? Left, CombatantData? Right) {
            var num = Left!.DamageTaken.CompareTo(Right!.DamageTaken);
            if (num == 0) {
                num = Left!.Name.CompareTo(Right!.Name);
            }
            return num;
        }
    }
}
