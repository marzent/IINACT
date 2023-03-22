namespace Advanced_Combat_Tracker;

public enum AttackTypeTypeEnum
{
    Melee,
    Spell,
    CombatArt,
    UnknownNonMelee
}

public class AttackType : IComparable, IEquatable<AttackType>, IComparable<AttackType>
{
    public delegate Color ColorDataCallback(AttackType Data);

    public delegate string StringDataCallback(AttackType Data);

    public static Dictionary<string, ColumnDef> ColumnDefs = new();

    private bool attacktypetypeCached;

    private bool averageDelayCached;

    private bool blockedCached;

    private AttackTypeTypeEnum cachedAttackTypeType;

    private float cachedAverageDelay;

    private int cachedBlocked;

    private int cachedCritHits;

    private long cDamage;

    private TimeSpan cachedDuration;

    private DateTime cachedEndTime;

    private int cachedHits;

    private long cachedMaxHit;

    private long cachedMedian;

    private long cachedMinHit;

    private int cachedMisses;

    private string cResist;

    private bool crithitsCached;

    private DateTime cachedStartTime;

    private int cachedSwings;

    private bool damageCached;

    private bool durationCached;

    private bool endtimeCached;

    private bool hitsCached;

    private bool maxhitCached;

    private bool medianCached;

    private bool minhitCached;

    private bool missesCached;

    private bool resistCached;

    private bool starttimeCached;

    private bool swingsCached;

    public AttackType(string theAttackType, DamageTypeData Parent)
    {
        Items = new List<MasterSwing>();
        Type = theAttackType;
        InvalidateCachedValues();
        this.Parent = Parent;
    }

    public DamageTypeData Parent { get; }

    public static string[] ColTypeCollection
    {
        get
        {
            var types = new string[ColumnDefs.Count];
            var index = 0;
            foreach (var columnDef in ColumnDefs)
            {
                types[index] = columnDef.Value.SqlDataType;
                index++;
            }
            return types;
        }
    }

    public static string[] ColHeaderCollection
    {
        get
        {
            var headers = new string[ColumnDefs.Count];
            var index = 0;
            foreach (var columnDef in ColumnDefs)
            {
                headers[index] = columnDef.Value.SqlDataName;
                index++;
            }

            return headers;
        }
    }

    public static string ColHeaderString => string.Join(",", ColHeaderCollection);

    public string[] ColCollection
    {
        get
        {
            var result = new string[ColumnDefs.Count];
            var index = 0;
            foreach (var columnDef in ColumnDefs)
            {
                result[index] = columnDef.Value.GetSqlData(this);
                index++;
            }
            return result;
        }
    }

    public string Type { get; }

    public string Resist
    {
        get
        {
            if (resistCached) return cResist;

            string result;

            if (Type == ActGlobals.Trans["attackTypeTerm-all"])
            {
                result = ActGlobals.Trans["attackTypeTerm-all"];
            }
            else
            {
                var text = string.Empty;
                var list = new List<string>();

                foreach (var item in Items)
                {
                    var damageType = item.DamageType
                                         .Replace(ActGlobals.Trans["specialAttackTerm-warded"] + "/", string.Empty);

                    if (damageType == ActGlobals.Trans["specialAttackTerm-melee"] ||
                        damageType == ActGlobals.Trans["specialAttackTerm-nonMelee"] ||
                        damageType == ActGlobals.Trans["specialAttackTerm-warded"])
                    {
                        text = damageType;
                    }
                    else if (!list.Contains(damageType))
                    {
                        list.Add(damageType);
                        break;
                    }
                }

                result = list.Count == 1
                             ? list[0]
                             : !string.IsNullOrEmpty(text)
                                 ? text
                                 : ActGlobals.Trans["specialAttackTerm-unknown"];
            }

            cResist = result;
            resistCached = true;
            return result;
        }
    }


    public long Damage
    {
        get
        {
            if (damageCached) return cDamage;

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
                return cachedHits;

            var numHits = 0;
            foreach (var swing in Items)
            {
                if (ActGlobals.blockIsHit && swing.Damage >= 0)
                {
                    numHits++;
                }
                else if (swing.Damage > 0)
                {
                    numHits++;
                }
            }

            cachedHits = numHits;
            hitsCached = true;

            return numHits;
        }
    }

    private static bool IsCritHit(MasterSwing swing)
    {
        return ActGlobals.blockIsHit 
                   ? swing.Critical && (long)swing.Damage >= 0 
                   : swing.Critical && (long)swing.Damage > 0;
    }

    public int CritHits
    {
        get
        {
            if (crithitsCached) return cachedCritHits;

            var count = 0;
            foreach (var swing in Items)
                if (IsCritHit(swing))
                    count++;

            cachedCritHits = count;
            crithitsCached = true;
            return count;
        }
    }


    public float CritPerc => CritHits / (float)Hits * 100f;

    public int Swings
    {
        get
        {
            if (swingsCached) return cachedSwings;

            var swings = 0;
            foreach (var t in Items)
                if (t.Damage != Dnum.Death)
                    swings++;
                else if (Type == ActGlobals.Trans["specialAttackTerm-killing"]) swings++;

            cachedSwings = swings;
            swingsCached = true;
            return swings;
        }
    }

    public int Misses
    {
        get
        {
            if (missesCached) return cachedMisses;

            var misses = Items.Count(t => t.Damage == Dnum.Miss);
            cachedMisses = misses;
            missesCached = true;
            return misses;
        }
    }

    public int Blocked
    {
        get
        {
            if (blockedCached) return cachedBlocked;

            var blocked = Items.Count(masterSwing => (long)masterSwing.Damage < -1 && masterSwing.Damage != Dnum.Death);
            cachedBlocked = blocked;
            blockedCached = true;
            return blocked;
        }
    }

    public float ToHit
    {
        get
        {
            try
            {
                return Hits / Swings * 100f;
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
            if (Hits > 0) return Damage / (double)Hits;
            return double.NaN;
        }
    }

    public long Median
    {
        get
        {
            try
            {
                if (medianCached) return cachedMedian;

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

                var median = list.Count / 2;
                if (list.Count > median)
                    cachedMedian = list[median];
                else
                    cachedMedian = 0L;

                medianCached = true;
                return cachedMedian;
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
            if (starttimeCached) return cachedStartTime;

            var dateTime = DateTime.MaxValue;
            foreach (var masterSwing in Items)
                if (masterSwing.Time < dateTime)
                    dateTime = masterSwing.Time;

            cachedStartTime = dateTime;
            starttimeCached = true;
            return dateTime;
        }
    }

    public DateTime EndTime
    {
        get
        {
            if (endtimeCached) return cachedEndTime;

            var dateTime = DateTime.MinValue;
            foreach (var masterSwing in Items)
                if (masterSwing.Time > dateTime)
                    dateTime = masterSwing.Time;

            cachedEndTime = dateTime;
            endtimeCached = true;
            return dateTime;
        }
    }

    public long MinHit
    {
        get
        {
            if (minhitCached) return cachedMinHit;

            var minHit = long.MaxValue;
            foreach (var masterSwing in Items)
                if (ActGlobals.blockIsHit)
                {
                    if ((long)masterSwing.Damage >= 0 && (long)masterSwing.Damage < minHit) 
                        minHit = masterSwing.Damage;
                }
                else if ((long)masterSwing.Damage > 0 && (long)masterSwing.Damage < minHit) 
                    minHit = masterSwing.Damage;

            if (minHit == long.MaxValue) return 0L;

            cachedMinHit = minHit;
            minhitCached = true;
            return minHit;
        }
    }

    public long MaxHit
    {
        get
        {
            if (maxhitCached) return cachedMaxHit;

            var maxHit = 0L;
            foreach (var masterSwing in Items)
            {
                if ((long)masterSwing.Damage > 0 && (long)masterSwing.Damage > maxHit) 
                    maxHit = masterSwing.Damage;
            }

            cachedMaxHit = maxHit;
            maxhitCached = true;
            return maxHit;
        }
    }

    public double ExtDPS => EncDPS;

    public double EncDPS
    {
        get
        {
            if (Parent.Parent == null) return double.NaN;

            var totalSeconds = Parent.Parent.Parent.Duration.TotalSeconds;
            if (totalSeconds > 0.0) return Damage / totalSeconds;

            return 0.0;
        }
    }

    public double CharDPS
    {
        get
        {
            if (Parent.Parent == null) return double.NaN;

            var totalSeconds = Parent.Parent.Duration.TotalSeconds;
            if (totalSeconds > 0.0) return Damage / totalSeconds;

            return 0.0;
        }
    }

    public double DPS
    {
        get
        {
            var totalSeconds = Duration.TotalSeconds;
            if (totalSeconds > 0.0) return Damage / totalSeconds;

            return 0.0;
        }
    }

    public TimeSpan Duration
    {
        get
        {
            // If there are multiple start times, calculate duration based on item times.
            if (Parent?.Parent?.Parent?.StartTimes.Count > 1)
            {
                if (durationCached) return cachedDuration;

                // Sort items by time.
                Items.Sort(MasterSwing.CompareTime);

                var startTimes = Parent.Parent.Parent.StartTimes;
                var endTimes = Parent.Parent.Parent.EndTimes;

                var itemIndex = 0;
                var startList = new List<DateTime>();
                var endList = new List<DateTime>();
            
                // For each time range, find the items that fall within it.
                for (var i = 0; i < startTimes.Count; i++)
                {
                    var startTime = startTimes[i];
                    var endTime = i < endTimes.Count ? endTimes[i] : DateTime.MaxValue;

                    while (itemIndex < Items.Count && Items[itemIndex].Time < startTime)
                    {
                        itemIndex++;
                    }

                    while (itemIndex < Items.Count && Items[itemIndex].Time <= endTime)
                    {
                        startList.Add(Items[itemIndex].Time);
                        itemIndex++;
                    }

                    endList.Add(endTime);
                }

                // Calculate the duration based on the start and end times.
                var duration = TimeSpan.Zero;
                for (var i = 0; i < startList.Count; i++)
                {
                    duration += endList[i] - startList[i];
                }

                cachedDuration = duration;
                durationCached = true;
                return cachedDuration;
            }

            // If there is only one start time, use the start and end times to calculate duration.
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
                return cachedAverageDelay;

            if (!ActGlobals.calcRealAvgDelay)
                return (float)Duration.TotalSeconds / Swings;

            var uniqueTimes = new HashSet<DateTime>();
            foreach (var swing in Items)
                uniqueTimes.Add(swing.Time);

            cachedAverageDelay = (float)Duration.TotalSeconds / (uniqueTimes.Count - 1);
            averageDelayCached = true;
            return cachedAverageDelay;
        }
    }


    public AttackTypeTypeEnum AttackTypeType
    {
        get
        {
            if (attacktypetypeCached)
                return cachedAttackTypeType;

            if (CombatantData.DamageSwingTypes.Count == 1)
            {
                // If there is only one damage swing type, it's probably not melee.
                return AttackTypeTypeEnum.UnknownNonMelee;
            }

            // Get the most common damage swing type.
            var damageSwingType = CombatantData.DamageSwingTypes[0];
            var swingTypeCounts = GetSwingTypeCounts();

            if (swingTypeCounts.Count == 1 && swingTypeCounts.ContainsKey(damageSwingType))
            {
                // If the most common damage swing type is the only one and it matches the swing type of the combatant,
                // then it's probably melee.
                cachedAttackTypeType = AttackTypeTypeEnum.Melee;
            }
            else
            {
                // Otherwise, it's probably not melee.
                cachedAttackTypeType = AttackTypeTypeEnum.UnknownNonMelee;
            }

            attacktypetypeCached = true;
            return cachedAttackTypeType;
        }
    }


    public List<MasterSwing> Items { get; set; }

    public Dictionary<string, object> Tags { get; set; } = new();

    public int CompareTo(object? obj)
    {
        return CompareTo((AttackType?)obj);
    }

    public int CompareTo(AttackType? other)
    {
        var comparisonResult = 0;
    
        // Check if a primary sort column is specified
        if (ColumnDefs.TryGetValue(ActGlobals.mDSort, out var value))
        {
            // Compare the current object with the other object using the primary sort column
            comparisonResult = value.SortComparer(this, other!);
        }

        // If objects are still equivalent, check if a secondary sort column is specified
        if (comparisonResult == 0 && ColumnDefs.TryGetValue(ActGlobals.mDSort2, out var value2))
        {
            // Compare the current object with the other object using the secondary sort column
            comparisonResult = value2.SortComparer(this, other!);
        }

        // If objects are still equivalent, compare them based on their damage
        if (comparisonResult == 0)
        {
            comparisonResult = Damage.CompareTo(other!.Damage);
        }

        // Return the comparison result
        return comparisonResult;
    }


    public bool Equals(AttackType? other)
    {
        return Type == other!.Type;
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

    public void Trim() => Items.TrimExcess();

    public string GetColumnByName(string name)
    {
        return ColumnDefs.TryGetValue(name, out var value) ? value.GetCellData(this) : string.Empty;
    }

    public Dictionary<int, int> GetSwingTypeCounts()
    {
        var dictionary = new Dictionary<int, int>();
        foreach (var masterSwing in Items)
            if (dictionary.ContainsKey(masterSwing.SwingType))
                dictionary[masterSwing.SwingType]++;
            else
                dictionary.Add(masterSwing.SwingType, 1);

        return dictionary;
    }

    public override string ToString()
    {
        return Type;
    }

    public override bool Equals(object? obj)
    {
        if (obj == DBNull.Value) return false;

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

    public Dictionary<string, int> GetAttackSpecials()
    {
        // Get a sorted list of all MasterSwing items for this combatant
        var swings = new List<MasterSwing>(Items);
        swings.Sort(MasterSwing.CompareTime);

        // Keep track of the attack specials encountered so far
        var previousSpecials = new List<string>();

        // Count the occurrence of each attack special
        var specialCounts = new Dictionary<string, int>();
        foreach (var swing in swings)
        {
            // If the current swing has no special attack, clear the list of previous specials
            if (swing.Special == ActGlobals.Trans["specialAttackTerm-none"])
                previousSpecials.Clear();

            // If the current swing has a new special attack, update the counts
            if (!previousSpecials.Contains(swing.Special))
            {
                if (specialCounts.ContainsKey(swing.Special))
                    specialCounts[swing.Special]++;
                else
                    specialCounts.Add(swing.Special, 1);

                // If the special attack is not "none", also update the "ANY" and "ONCE" counts
                if (swing.Special != ActGlobals.Trans["specialAttackTerm-none"])
                {
                    if (specialCounts.ContainsKey("ANY"))
                        specialCounts["ANY"]++;
                    else
                        specialCounts.Add("ANY", 1);

                    if (!previousSpecials.Contains("ONCE"))
                    {
                        if (specialCounts.ContainsKey("ONCE"))
                            specialCounts["ONCE"]++;
                        else
                            specialCounts.Add("ONCE", 1);

                        previousSpecials.Add("ONCE");
                    }
                }
            }

            // Add the current special to the list of previous specials
            previousSpecials.Add(swing.Special);
        }

        // Return the dictionary of attack specials and their counts
        return specialCounts;
    }


    public class ColumnDef
    {
        public ColorDataCallback GetCellBackColor = Data => Color.Transparent;
        public StringDataCallback GetCellData;

        public ColorDataCallback GetCellForeColor = Data => Color.Transparent;

        public StringDataCallback GetSqlData;

        public Comparison<AttackType> SortComparer;

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

        public string SqlDataType { get; }

        public string SqlDataName { get; }

        public bool DefaultVisible { get; }

        public string Label { get; }
    }
}
