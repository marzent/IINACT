using System.Diagnostics;

namespace Advanced_Combat_Tracker
{
    public enum AttackTypeTypeEnum
    {
        Melee,
        Spell,
        CombatArt,
        UnknownNonMelee
    }

    public class AttackType : IComparable, IEquatable<AttackType>, IComparable<AttackType>
    {
        public delegate string StringDataCallback(AttackType Data);

        public delegate Color ColorDataCallback(AttackType Data);

        public class ColumnDef
        {
            public StringDataCallback GetCellData;

            public StringDataCallback GetSqlData;

            public Comparison<AttackType> SortComparer;

            public ColorDataCallback GetCellForeColor = (AttackType Data) => Color.Transparent;

            public ColorDataCallback GetCellBackColor = (AttackType Data) => Color.Transparent;

            public string SqlDataType { get; }

            public string SqlDataName { get; }

            public bool DefaultVisible { get; }

            public string Label { get; }

            public ColumnDef(
                string Label, bool DefaultVisible, string SqlDataType, string SqlDataName,
                StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback,
                Comparison<AttackType> SortComparer)
            {
                this.Label = Label;
                this.DefaultVisible = DefaultVisible;
                this.SqlDataType = SqlDataType;
                this.SqlDataName = SqlDataName;
                GetCellData = CellDataCallback;
                GetSqlData = SqlDataCallback;
                this.SortComparer = SortComparer;
            }
        }

        public class DualComparison : IComparer<AttackType>
        {
            private readonly string sort1;

            private readonly string sort2;

            public DualComparison(string Sort1, string Sort2)
            {
                sort1 = Sort1;
                sort2 = Sort2;
            }

            public int Compare(AttackType? Left, AttackType? Right)
            {
                var num = 0;
                Debug.Assert(Left != null, nameof(Left) + " != null");
                Debug.Assert(Right != null, nameof(Right) + " != null");
                if (ColumnDefs.ContainsKey(sort1))
                {
                    num = ColumnDefs[sort1].SortComparer(Left, Right);
                }

                if (num == 0 && ColumnDefs.ContainsKey(sort2))
                {
                    num = ColumnDefs[sort2].SortComparer(Left, Right);
                }

                if (num == 0)
                {
                    num = Left.Damage.CompareTo(Right.Damage);
                }

                return num;
            }
        }

        public static Dictionary<string, ColumnDef> ColumnDefs = new Dictionary<string, ColumnDef>();

        private bool damageCached;

        private bool hitsCached;

        private bool swingsCached;

        private bool missesCached;

        private bool blockedCached;

        private bool medianCached;

        private bool starttimeCached;

        private bool endtimeCached;

        private bool minhitCached;

        private bool maxhitCached;

        private bool crithitsCached;

        private bool resistCached;

        private bool durationCached;

        private bool attacktypetypeCached;

        private bool averageDelayCached;

        private int cHits;

        private int cSwings;

        private int cMisses;

        private int cBlocked;

        private int cCritHits;

        private long cDamage;

        private long cMedian;

        private long cMinHit;

        private long cMaxHit;

        private DateTime cStartTime;

        private DateTime cEndTime;

        private string cResist;

        private TimeSpan cDuration;

        private AttackTypeTypeEnum cAttackTypeType;

        private float cAverageDelay;

        public DamageTypeData Parent { get; }

        public static string[] ColTypeCollection
        {
            get
            {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs)
                {
                    array[num] = columnDef.Value.SqlDataType;
                    num++;
                }

                return array;
            }
        }

        public static string[] ColHeaderCollection
        {
            get
            {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs)
                {
                    array[num] = columnDef.Value.SqlDataName;
                    num++;
                }

                return array;
            }
        }

        public static string ColHeaderString => string.Join(",", ColHeaderCollection);

        public string[] ColCollection
        {
            get
            {
                var array = new string[ColumnDefs.Count];
                var num = 0;
                foreach (var columnDef in ColumnDefs)
                {
                    array[num] = columnDef.Value.GetSqlData(this);
                    num++;
                }

                return array;
            }
        }

        public string Type { get; }

        public string Resist
        {
            get
            {
                if (resistCached)
                {
                    return cResist;
                }

                string empty;
                if (Type == ActGlobals.Trans["attackTypeTerm-all"])
                {
                    empty = ActGlobals.Trans["attackTypeTerm-all"];
                }
                else
                {
                    var text = string.Empty;
                    var list = new List<string>();
                    for (var i = 0; i < Items.Count; i++)
                    {
                        var text2 = Items[i].DamageType
                                            .Replace(ActGlobals.Trans["specialAttackTerm-warded"] + "/", string.Empty);
                        if (text2 == ActGlobals.Trans["specialAttackTerm-melee"] ||
                            text2 == ActGlobals.Trans["specialAttackTerm-nonMelee"] ||
                            text2 == ActGlobals.Trans["specialAttackTerm-warded"])
                        {
                            text = text2;
                        }
                        else if (!list.Contains(text2))
                        {
                            list.Add(text2);
                            break;
                        }
                    }

                    empty = ((list.Count == 1)
                                 ? list[0]
                                 : ((!string.IsNullOrEmpty(text))
                                        ? text
                                        : ActGlobals.Trans["specialAttackTerm-unknown"]));
                }

                cResist = empty;
                resistCached = true;
                return empty;
            }
        }

        public long Damage
        {
            get
            {
                if (damageCached)
                {
                    return cDamage;
                }

                long _damage = 0;
                try
                {
                    _damage = Items.Where(masterSwing => (long)masterSwing.Damage > 0).Aggregate(0L,
                        (current, masterSwing) => (long)(current + masterSwing.Damage));
                }
                catch (InvalidOperationException)
                {
                    return Damage;
                }

                cDamage = _damage;
                damageCached = true;
                return _damage;
            }
        }

        public int Hits
        {
            get
            {
                if (hitsCached)
                {
                    return cHits;
                }

                var num = 0;
                foreach (var masterSwing in Items)
                {
                    if (ActGlobals.blockIsHit)
                    {
                        if ((long)masterSwing.Damage >= 0)
                        {
                            num++;
                        }
                    }
                    else if ((long)masterSwing.Damage > 0)
                    {
                        num++;
                    }
                }

                cHits = num;
                hitsCached = true;
                return num;
            }
        }

        public int CritHits
        {
            get
            {
                if (crithitsCached)
                {
                    return cCritHits;
                }

                var num = 0;
                foreach (var masterSwing in Items)
                {
                    if (ActGlobals.blockIsHit)
                    {
                        if (masterSwing.Critical && (long)masterSwing.Damage >= 0)
                        {
                            num++;
                        }
                    }
                    else if (masterSwing.Critical && (long)masterSwing.Damage > 0)
                    {
                        num++;
                    }
                }

                cCritHits = num;
                crithitsCached = true;
                return num;
            }
        }

        public float CritPerc => (float)CritHits / (float)Hits * 100f;

        public int Swings
        {
            get
            {
                if (swingsCached)
                {
                    return cSwings;
                }

                var num = 0;
                foreach (var t in Items)
                {
                    if (t.Damage != Dnum.Death)
                    {
                        num++;
                    }
                    else if (Type == ActGlobals.Trans["specialAttackTerm-killing"])
                    {
                        num++;
                    }
                }

                cSwings = num;
                swingsCached = true;
                return num;
            }
        }

        public int Misses
        {
            get
            {
                if (missesCached)
                {
                    return cMisses;
                }

                var num = Items.Count(t => t.Damage == Dnum.Miss);
                cMisses = num;
                missesCached = true;
                return num;
            }
        }

        public int Blocked
        {
            get
            {
                if (blockedCached)
                {
                    return cBlocked;
                }

                var num = Items.Count(masterSwing => (long)masterSwing.Damage < -1 && masterSwing.Damage != Dnum.Death);
                cBlocked = num;
                blockedCached = true;
                return num;
            }
        }

        public float ToHit
        {
            get
            {
                try
                {
                    float num = Hits;
                    float num2 = Swings;
                    return num / num2 * 100f;
                }
                catch
                {
                    return 0f;
                }
            }
        }

        public double Average
        {
            get
            {
                if (Hits > 0)
                {
                    return (double)Damage / (double)Hits;
                }

                return double.NaN;
            }
        }

        public long Median
        {
            get
            {
                try
                {
                    if (medianCached)
                    {
                        return cMedian;
                    }

                    var list = (from masterSwing in Items where (long)masterSwing.Damage >= 0 select masterSwing.Damage)
                        .ToList();
                    try
                    {
                        list.Sort();
                    }
                    catch (Exception ex)
                    {
                        ActGlobals.oFormActMain.WriteExceptionLog(ex, string.Empty);
                    }

                    var num = list.Count / 2;
                    if (list.Count > num)
                    {
                        cMedian = list[num];
                    }
                    else
                    {
                        cMedian = 0L;
                    }

                    medianCached = true;
                    return cMedian;
                }
                catch
                {
                    return 0L;
                }
            }
        }

        public DateTime StartTime
        {
            get
            {
                if (starttimeCached)
                {
                    return cStartTime;
                }

                var dateTime = DateTime.MaxValue;
                foreach (var masterSwing in Items)
                {
                    if (masterSwing.Time < dateTime)
                    {
                        dateTime = masterSwing.Time;
                    }
                }

                cStartTime = dateTime;
                starttimeCached = true;
                return dateTime;
            }
        }

        public DateTime EndTime
        {
            get
            {
                if (endtimeCached)
                {
                    return cEndTime;
                }

                var dateTime = DateTime.MinValue;
                foreach (var masterSwing in Items)
                {
                    if (masterSwing.Time > dateTime)
                    {
                        dateTime = masterSwing.Time;
                    }
                }

                cEndTime = dateTime;
                endtimeCached = true;
                return dateTime;
            }
        }

        public long MinHit
        {
            get
            {
                if (minhitCached)
                {
                    return cMinHit;
                }

                var num = long.MaxValue;
                foreach (var masterSwing in Items)
                {
                    if (ActGlobals.blockIsHit)
                    {
                        if ((long)masterSwing.Damage >= 0 && (long)masterSwing.Damage < num)
                        {
                            num = masterSwing.Damage;
                        }
                    }
                    else if ((long)masterSwing.Damage > 0 && (long)masterSwing.Damage < num)
                    {
                        num = masterSwing.Damage;
                    }
                }

                if (num == long.MaxValue)
                {
                    return 0L;
                }

                cMinHit = num;
                minhitCached = true;
                return num;
            }
        }

        public long MaxHit
        {
            get
            {
                if (maxhitCached)
                {
                    return cMaxHit;
                }

                var num = 0L;
                for (var i = 0; i < Items.Count; i++)
                {
                    var masterSwing = Items[i];
                    if ((long)masterSwing.Damage > 0 && (long)masterSwing.Damage > num)
                    {
                        num = masterSwing.Damage;
                    }
                }

                cMaxHit = num;
                maxhitCached = true;
                return num;
            }
        }

        public double ExtDPS => EncDPS;

        public double EncDPS
        {
            get
            {
                if (Parent.Parent == null)
                {
                    return double.NaN;
                }

                var num = Parent.Parent.Parent.Duration.TotalSeconds;
                if (num > 0.0)
                {
                    return (double)Damage / num;
                }

                return 0.0;
            }
        }

        public double CharDPS
        {
            get
            {
                if (Parent.Parent == null)
                {
                    return double.NaN;
                }

                var num = Parent.Parent.Duration.TotalSeconds;
                if (num > 0.0)
                {
                    return (double)Damage / num;
                }

                return 0.0;
            }
        }

        public double DPS
        {
            get
            {
                var num = Duration.TotalSeconds;
                if (num > 0.0)
                {
                    return (double)Damage / num;
                }

                return 0.0;
            }
        }

        public TimeSpan Duration
        {
            get
            {
                if (Parent.Parent != null && Parent.Parent.Parent.StartTimes.Count > 1)
                {
                    if (durationCached)
                    {
                        return cDuration;
                    }

                    if (this == null)
                    {
                        return TimeSpan.Zero;
                    }

                    Items.Sort(MasterSwing.CompareTime);
                    var list = new List<DateTime>();
                    var list2 = new List<DateTime>();
                    var num = 0;
                    for (var i = 0; i < Parent.Parent.Parent.StartTimes.Count; i++)
                    {
                        if (num < 0)
                        {
                            num = 0;
                        }

                        if (i < Parent.Parent.Parent.EndTimes.Count &&
                            Parent.Parent.Parent.EndTimes[i] < Items[num].Time)
                        {
                            continue;
                        }

                        for (var j = num; j < Items.Count; j++)
                        {
                            if (list.Count == list2.Count)
                            {
                                if (i == Parent.Parent.Parent.StartTimes.Count - 1 &&
                                    Parent.Parent.Parent.EndTimes.Count + 1 == Parent.Parent.Parent.StartTimes.Count)
                                {
                                    if (Items[j].Time >= Parent.Parent.Parent.StartTimes[i] &&
                                        Items[j].Time <= Parent.Parent.Parent.EndTime)
                                    {
                                        list.Add(Items[j].Time);
                                        num = j;
                                    }
                                }
                                else if (Items[j].Time >= Parent.Parent.Parent.StartTimes[i] &&
                                         Items[j].Time <= Parent.Parent.Parent.EndTimes[i])
                                {
                                    list.Add(Items[j].Time);
                                    num = j;
                                }
                            }

                            if (list.Count - 1 == list2.Count)
                            {
                                MasterSwing masterSwing = null!;
                                for (var k = j; k < Items.Count; k++)
                                {
                                    masterSwing = Items[k];
                                    if (k + 1 == Items.Count)
                                    {
                                        num = k - 1;
                                        break;
                                    }

                                    if (Parent.Parent.Parent.StartTimes.Count <= i + 1 ||
                                        Items[k + 1].Time < Parent.Parent.Parent.StartTimes[i + 1]) continue;
                                    num = k - 1;
                                    break;
                                }

                                list2.Add(masterSwing.Time);
                                break;
                            }

                            if (i < Parent.Parent.Parent.EndTimes.Count &&
                                Items[j].Time > Parent.Parent.Parent.EndTimes[i])
                            {
                                break;
                            }
                        }
                    }

                    if (list.Count - 1 == list2.Count)
                    {
                        list2.Add(Items[^1].Time);
                    }

                    if (list.Count != list2.Count)
                    {
                        throw new Exception(
                            $"Personal Duration failure.  StartTimes: {list.Count}/{Parent.Parent.Parent.StartTimes.Count} EndTimes: {list2.Count}/{Parent.Parent.Parent.EndTimes.Count}");
                    }

                    var timeSpan = default(TimeSpan);
                    for (var l = 0; l < list.Count; l++)
                    {
                        timeSpan += list2[l] - list[l];
                    }

                    cDuration = timeSpan;
                    durationCached = true;
                    return cDuration;
                }

                if (EndTime > StartTime)
                {
                    return EndTime - StartTime;
                }

                return TimeSpan.Zero;
            }
        }

        public string DurationS => Duration.Hours == 0
                                       ? $"{Duration.Minutes:00}:{Duration.Seconds:00}"
                                       : $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

        public float AverageDelay
        {
            get
            {
                if (averageDelayCached)
                    return cAverageDelay;

                if (!ActGlobals.calcRealAvgDelay) return (float)Duration.TotalSeconds / (float)Swings;
                var list = new List<MasterSwing>(Items);
                var dictionary = new Dictionary<DateTime, DateTime>();
                foreach (var t in list)
                {
                    if (!dictionary.ContainsKey(t.Time))
                    {
                        dictionary.Add(t.Time, t.Time);
                    }
                }

                cAverageDelay = (float)Duration.TotalSeconds / (float)(dictionary.Count - 1);
                return cAverageDelay;
            }
        }

        public AttackTypeTypeEnum AttackTypeType
        {
            get
            {
                if (attacktypetypeCached)
                {
                    return cAttackTypeType;
                }

                var num = -1;
                if (CombatantData.DamageSwingTypes.Count == 1)
                {
                    return AttackTypeTypeEnum.UnknownNonMelee;
                }

                num = CombatantData.DamageSwingTypes[0];
                var swingTypeCounts = GetSwingTypeCounts();
                if (swingTypeCounts.Count == 1 && swingTypeCounts.ContainsKey(num))
                {
                    cAttackTypeType = AttackTypeTypeEnum.Melee;
                }
                else
                {
                    cAttackTypeType = AttackTypeTypeEnum.UnknownNonMelee;
                }

                attacktypetypeCached = true;
                return cAttackTypeType;
            }
        }

        public List<MasterSwing> Items { get; set; }

        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        public AttackType(string theAttackType, DamageTypeData Parent)
        {
            Items = new List<MasterSwing>();
            Type = theAttackType;
            InvalidateCachedValues();
            this.Parent = Parent;
        }

        public void InvalidateCachedValues()
        {
            damageCached = false;
            hitsCached = false;
            swingsCached = false;
            missesCached = false;
            blockedCached = false;
            medianCached = false;
            starttimeCached = false;
            endtimeCached = false;
            minhitCached = false;
            maxhitCached = false;
            crithitsCached = false;
            durationCached = false;
            attacktypetypeCached = false;
            averageDelayCached = false;
        }

        public void AddCombatAction(MasterSwing action)
        {
            InvalidateCachedValues();
            Items.Add(action);
        }

        public void Trim()
        {
            Items.TrimExcess();
        }

        public string GetColumnByName(string name) =>
            ColumnDefs.ContainsKey(name) ? ColumnDefs[name].GetCellData(this) : string.Empty;

        public Dictionary<int, int> GetSwingTypeCounts()
        {
            var dictionary = new Dictionary<int, int>();
            foreach (var masterSwing in Items)
            {
                if (dictionary.ContainsKey(masterSwing.SwingType))
                {
                    dictionary[masterSwing.SwingType]++;
                }
                else
                {
                    dictionary.Add(masterSwing.SwingType, 1);
                }
            }

            return dictionary;
        }

        public override string ToString()
        {
            return Type;
        }

        public int CompareTo(object? obj)
        {
            return CompareTo((AttackType?)obj);
        }

        public int CompareTo(AttackType? other)
        {
            var num = 0;
            Debug.Assert(other != null, nameof(other) + " != null");
            if (ColumnDefs.ContainsKey(ActGlobals.mDSort))
            {
                num = ColumnDefs[ActGlobals.mDSort].SortComparer(this, other);
            }

            if (num == 0 && ColumnDefs.ContainsKey(ActGlobals.mDSort2))
            {
                num = ColumnDefs[ActGlobals.mDSort2].SortComparer(this, other);
            }

            if (num == 0)
            {
                num = Damage.CompareTo(other.Damage);
            }

            return num;
        }

        public override bool Equals(object? obj)
        {
            if (obj == DBNull.Value)
            {
                return false;
            }

            var attackType = (AttackType)obj!;
            var value = attackType.Type;
            return Type.Equals(value);
        }

        public override int GetHashCode()
        {
            try
            {
                return Items.Aggregate(0L, (current, masterSwing) => current + masterSwing.GetHashCode()).GetHashCode();
            }
            catch (InvalidOperationException)
            {
                return GetHashCode();
            }
        }

        public bool Equals(AttackType? other) => Type == other!.Type;

        public Dictionary<string, int> GetAttackSpecials()
        {
            var list = new List<MasterSwing>(Items);
            list.Sort(MasterSwing.CompareTime);
            var list2 = new List<string>();
            var dictionary = new Dictionary<string, int>();
            foreach (var masterSwing in list)
            {
                if (masterSwing.Special == ActGlobals.Trans["specialAttackTerm-none"])
                {
                    list2.Clear();
                }

                if (!list2.Contains(masterSwing.Special))
                {
                    if (dictionary.ContainsKey(masterSwing.Special))
                    {
                        dictionary[masterSwing.Special]++;
                    }
                    else
                    {
                        dictionary.Add(masterSwing.Special, 1);
                    }

                    if (masterSwing.Special != ActGlobals.Trans["specialAttackTerm-none"])
                    {
                        if (dictionary.ContainsKey("ANY"))
                        {
                            dictionary["ANY"]++;
                        }
                        else
                        {
                            dictionary.Add("ANY", 1);
                        }

                        if (!list2.Contains("ONCE"))
                        {
                            if (dictionary.ContainsKey("ONCE"))
                            {
                                dictionary["ONCE"]++;
                            }
                            else
                            {
                                dictionary.Add("ONCE", 1);
                            }

                            list2.Add("ONCE");
                        }
                    }
                }

                list2.Add(masterSwing.Special);
            }

            return dictionary;
        }
    }
}
