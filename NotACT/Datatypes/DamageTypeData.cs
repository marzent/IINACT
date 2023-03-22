namespace Advanced_Combat_Tracker;

public class DamageTypeData : IEquatable<DamageTypeData>, IComparable<DamageTypeData>
{
    public delegate Color ColorDataCallback(DamageTypeData Data);

    public delegate string StringDataCallback(DamageTypeData Data);

    public static Dictionary<string, ColumnDef> ColumnDefs = new();

    private readonly string tag;

    private TimeSpan cachedDuration;

    private bool durationCached;

    public DamageTypeData(bool Outgoing, string Tag, CombatantData Parent)
    {
        Items = new SortedList<string, AttackType>();
        this.Parent = Parent;
        tag = Tag;
        this.Outgoing = Outgoing;
        InvalidateCachedValues();
    }

    public CombatantData Parent { get; }

    public bool Outgoing { get; set; }

    public static string[] ColTypeCollection
    {
        get
        {
            var colTypeCollection = new string[ColumnDefs.Count];
            var i = 0;
            foreach (var columnDef in ColumnDefs)
            {
                colTypeCollection[i] = columnDef.Value.SqlDataType;
                i++;
            }

            return colTypeCollection;
        }
    }

    public static string[] ColHeaderCollection
    {
        get
        {
            var colHeaderCollection = new string[ColumnDefs.Count];
            var i = 0;
            foreach (var columnDef in ColumnDefs)
            {
                colHeaderCollection[i] = columnDef.Value.SqlDataName;
                i++;
            }

            return colHeaderCollection;
        }
    }

    public static string ColHeaderString => string.Join(",", ColHeaderCollection);

    public string[] ColCollection
    {
        get
        {
            var colCollection = new string[ColumnDefs.Count];
            var i = 0;
            foreach (var columnDef in ColumnDefs)
            {
                colCollection[i] = columnDef.Value.GetSqlData(this);
                i++;
            }

            return colCollection;
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
            if (Parent == null || Parent.Parent.StartTimes.Count <= 1)
            {
                if (EndTime > StartTime)
                    return EndTime - StartTime;

                return TimeSpan.Zero;
            }

            if (durationCached)
                return cachedDuration;

            Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value);
            if (value == null)
                return TimeSpan.Zero;

            value.Items.Sort(MasterSwing.CompareTime);

            var startTimes = Parent.Parent.StartTimes;
            var endTimes = Parent.Parent.EndTimes;
            var masterSwings = value.Items;
            var swingIndex = 0;
            var swingCount = masterSwings.Count;

            var startTimesCount = startTimes.Count;
            var endTimesCount = endTimes.Count;
            var list = new List<DateTime>();
            var list2 = new List<DateTime>();
            for (var i = 0; i < startTimesCount; i++)
            {
                if (swingIndex < 0)
                    swingIndex = 0;

                if (i < endTimesCount && endTimes[i] < masterSwings[swingIndex].Time)
                    continue;

                for (var j = swingIndex; j < swingCount; j++)
                {
                    var swing = masterSwings[j];
                    if (list.Count == list2.Count)
                    {
                        var isLastStart = i == startTimesCount - 1 && endTimesCount + 1 == startTimesCount;
                        if ((isLastStart && swing.Time >= startTimes[i] && swing.Time <= EndTime)
                            || (!isLastStart && swing.Time >= startTimes[i] && swing.Time <= endTimes[i]))
                        {
                            list.Add(swing.Time);
                            swingIndex = j;
                        }
                    }

                    if (list.Count - 1 == list2.Count)
                    {
                        MasterSwing lastSwing = swing;
                        for (var k = j; k < swingCount; k++)
                        {
                            lastSwing = masterSwings[k];
                            if (k + 1 == swingCount
                                || (startTimesCount > i + 1 && masterSwings[k + 1].Time >= startTimes[i + 1]))
                            {
                                swingIndex = k - 1;
                                break;
                            }
                        }

                        list2.Add(lastSwing.Time);
                        break;
                    }

                    if (i < endTimesCount && swing.Time > endTimes[i])
                        break;
                }
            }

            if (list.Count - 1 == list2.Count)
                list2.Add(masterSwings[^1].Time);

            if (list.Count != list2.Count)
                throw new Exception($"Personal Duration failure. StartTimes: {list.Count}/{startTimesCount} "
                                    + $"EndTimes: {list2.Count}/{endTimesCount}");

            var duration = TimeSpan.Zero;
            for (var l = 0; l < list.Count; l++)
                duration += list2[l] - list[l];

            cachedDuration = duration;
            durationCached = true;
            return cachedDuration;
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
            if (Swings > 0)
                return Hits / Swings * 100f;
            return 0f;
        }
    }

    public double Average => Hits > 0 ? Damage / (double)Hits : 0.0;

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

    public Dictionary<string, object> Tags { get; set; } = new();

    public int CompareTo(DamageTypeData? other) => Damage.CompareTo(other!.Damage);

    public bool Equals(DamageTypeData? other) => Type.Equals(other!.Type);

    public void InvalidateCachedValues() => durationCached = false;

    public void InvalidateCachedValues(bool Recursive)
    {
        InvalidateCachedValues();
        if (!Recursive) return;
        for (var i = 0; i < Items.Count; i++) Items.Values[i].InvalidateCachedValues();
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
        foreach (var attackType in Items.Values) attackType.Trim();
    }

    public string GetColumnByName(string name)
    {
        if (!ColumnDefs.ContainsKey(name)) return string.Empty;
        var col = ColumnDefs[name];
        return col.GetCellData(this);
    }

    public override string ToString() => tag;

    public override int GetHashCode() => 
        Items.Values.Aggregate(0L, (current, t) => current + t.GetHashCode()).GetHashCode();

    public class ColumnDef
    {
        public ColorDataCallback GetCellBackColor = Data => Color.Transparent;
        public StringDataCallback GetCellData;

        public ColorDataCallback GetCellForeColor = Data => Color.Transparent;

        public StringDataCallback GetSqlData;

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

        public string SqlDataType { get; }

        public string SqlDataName { get; }

        public bool DefaultVisible { get; }

        public string Label { get; }
    }
}
