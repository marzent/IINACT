namespace Advanced_Combat_Tracker
{
    public class DamageTypeData : IEquatable<DamageTypeData>, IComparable<DamageTypeData>
    {
        public delegate string StringDataCallback(DamageTypeData Data);

        public delegate Color ColorDataCallback(DamageTypeData Data);

        public class ColumnDef
        {
            public StringDataCallback GetCellData;

            public StringDataCallback GetSqlData;

            public ColorDataCallback GetCellForeColor = (DamageTypeData Data) => Color.Transparent;

            public ColorDataCallback GetCellBackColor = (DamageTypeData Data) => Color.Transparent;

            public string SqlDataType { get; }

            public string SqlDataName { get; }

            public bool DefaultVisible { get; }

            public string Label { get; }

            public ColumnDef(
                string Label, bool DefaultVisible, string SqlDataType, string SqlDataName,
                StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback)
            {
                this.Label = Label;
                this.DefaultVisible = DefaultVisible;
                this.SqlDataType = SqlDataType;
                this.SqlDataName = SqlDataName;
                GetCellData = CellDataCallback;
                GetSqlData = SqlDataCallback;
            }
        }

        public static Dictionary<string, ColumnDef> ColumnDefs = new Dictionary<string, ColumnDef>();

        private readonly string tag;

        private TimeSpan cDuration;

        private bool durationCached;

        public CombatantData Parent { get; }

        public bool Outgoing { get; set; }

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

        public DateTime StartTime => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)
                                         ? value.StartTime
                                         : DateTime.MaxValue;

        public DateTime EndTime => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)
                                       ? value.EndTime
                                       : DateTime.MinValue;

        public TimeSpan Duration
        {
            get
            {
                if (Parent != null && Parent.Parent.StartTimes.Count > 1)
                {
                    if (durationCached)
                    {
                        return cDuration;
                    }

                    Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value);
                    if (value == null)
                    {
                        return TimeSpan.Zero;
                    }

                    value.Items.Sort(MasterSwing.CompareTime);
                    var list = new List<DateTime>();
                    var list2 = new List<DateTime>();
                    var num = 0;
                    for (var i = 0; i < Parent.Parent.StartTimes.Count; i++)
                    {
                        if (num < 0)
                        {
                            num = 0;
                        }

                        if (i < Parent.Parent.EndTimes.Count && Parent.Parent.EndTimes[i] < value.Items[num].Time)
                        {
                            continue;
                        }

                        for (var j = num; j < value.Items.Count; j++)
                        {
                            if (list.Count == list2.Count)
                            {
                                if (i == Parent.Parent.StartTimes.Count - 1 && Parent.Parent.EndTimes.Count + 1 ==
                                    Parent.Parent.StartTimes.Count)
                                {
                                    if (value.Items[j].Time >= Parent.Parent.StartTimes[i] &&
                                        value.Items[j].Time <= Parent.Parent.EndTime)
                                    {
                                        list.Add(value.Items[j].Time);
                                        num = j;
                                    }
                                }
                                else if (value.Items[j].Time >= Parent.Parent.StartTimes[i] &&
                                         value.Items[j].Time <= Parent.Parent.EndTimes[i])
                                {
                                    list.Add(value.Items[j].Time);
                                    num = j;
                                }
                            }

                            if (list.Count - 1 == list2.Count)
                            {
                                MasterSwing masterSwing = null!;
                                for (var k = j; k < value.Items.Count; k++)
                                {
                                    masterSwing = value.Items[k];
                                    if (k + 1 == value.Items.Count)
                                    {
                                        num = k - 1;
                                        break;
                                    }

                                    if (Parent.Parent.StartTimes.Count > i + 1 &&
                                        value.Items[k + 1].Time >= Parent.Parent.StartTimes[i + 1])
                                    {
                                        num = k - 1;
                                        break;
                                    }
                                }

                                list2.Add(masterSwing.Time);
                                break;
                            }

                            if (i < Parent.Parent.EndTimes.Count && value.Items[j].Time > Parent.Parent.EndTimes[i])
                            {
                                break;
                            }
                        }
                    }

                    if (list.Count - 1 == list2.Count)
                    {
                        list2.Add(value.Items[^1].Time);
                    }

                    if (list.Count != list2.Count)
                    {
                        throw new Exception(string.Format(
                                                "Personal Duration failure.  StartTimes: {0}/{2} EndTimes: {1}/{3}",
                                                list.Count, list2.Count, Parent.Parent.StartTimes.Count,
                                                Parent.Parent.EndTimes.Count));
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

        public float AverageDelay => Items.ContainsKey(ActGlobals.Trans["attackTypeTerm-all"])
                                         ? Items[ActGlobals.Trans["attackTypeTerm-all"]].AverageDelay
                                         : float.NaN;

        public long Damage =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Damage : 0L;

        public int Swings =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Swings : 0;

        public int Hits => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Hits : 0;

        public int CritHits => Items.TryGetValue("All", out var value) ? value.CritHits : 0;

        public float CritPerc => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)
                                     ? value.CritPerc
                                     : 0f;

        public int Misses =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Misses : 0;

        public int Blocked =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Blocked : 0;

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

        public double Average => Hits > 0 ? (double)Damage / (double)Hits : 0.0;

        public long Median =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.Median : 0L;

        public long MinHit =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.MinHit : 0L;

        public long MaxHit =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.MaxHit : 0L;

        public double EncDPS =>
            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.EncDPS : 0.0;

        public double ExtDPS => EncDPS;

        public double CharDPS => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)
                                     ? value.CharDPS
                                     : 0.0;

        public double DPS => Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value) ? value.DPS : 0.0;

        public string Type => ToString();

        public SortedList<string, AttackType> Items { get; set; }

        public Dictionary<string, object> Tags { get; set; } = new Dictionary<string, object>();

        public DamageTypeData(bool Outgoing, string Tag, CombatantData Parent)
        {
            Items = new SortedList<string, AttackType>();
            this.Parent = Parent;
            tag = Tag;
            this.Outgoing = Outgoing;
            InvalidateCachedValues();
        }

        public void InvalidateCachedValues()
        {
            durationCached = false;
        }

        public void InvalidateCachedValues(bool Recursive)
        {
            InvalidateCachedValues();
            if (!Recursive) return;
            for (var i = 0; i < Items.Count; i++)
            {
                Items.Values[i].InvalidateCachedValues();
            }
        }

        public void AddCombatAction(MasterSwing action, string theAttackTypeListed)
        {
            InvalidateCachedValues();
            if (!Items.TryGetValue(theAttackTypeListed, out var value))
            {
                value = new AttackType(theAttackTypeListed, this);
                Items.Add(theAttackTypeListed, value);
            }

            value.AddCombatAction(action);
        }

        public void Trim()
        {
            Items.TrimExcess();
            foreach (var attackType in Items.Values)
            {
                attackType.Trim();
            }
        }

        public string GetColumnByName(string name)
        {
            if (!ColumnDefs.ContainsKey(name)) return string.Empty;
            var col = ColumnDefs[name];
            return col.GetCellData(this);
        }

        public override string ToString()
        {
            return tag;
        }

        public override int GetHashCode() =>
            Items.Values.Aggregate(0L, (current, t) => current + t.GetHashCode()).GetHashCode();

        public bool Equals(DamageTypeData? other) => Type.Equals(other!.Type);

        public int CompareTo(DamageTypeData? other) => Damage.CompareTo(other!.Damage);
    }
}
