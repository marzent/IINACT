using static System.String;


namespace Advanced_Combat_Tracker;

public class CombatantData : IComparable, IEquatable<CombatantData>, IComparable<CombatantData>
{
    public delegate Color ColorDataCallback(CombatantData Data);

    public delegate string ExportStringDataCallback(CombatantData Data, string ExtraFormat);

    public delegate string StringDataCallback(CombatantData Data);

    public static Dictionary<string, TextExportFormatter> ExportVariables = new();

    public static Dictionary<string, ColumnDef> ColumnDefs = new();

    public static Dictionary<string, DamageTypeDef> OutgoingDamageTypeDataObjects = new();

    public static Dictionary<string, DamageTypeDef> IncomingDamageTypeDataObjects = new();

    public static SortedDictionary<int, List<string>> SwingTypeToDamageTypeDataLinksOutgoing = new();

    public static SortedDictionary<int, List<string>> SwingTypeToDamageTypeDataLinksIncoming = new();

    public static List<int> DamageSwingTypes = new();

    public static List<int> HealingSwingTypes = new();

    public static string DamageTypeDataNonSkillDamage = Empty;

    public static string DamageTypeDataOutgoingDamage = Empty;

    public static string DamageTypeDataOutgoingHealing = Empty;

    public static string DamageTypeDataIncomingDamage = Empty;

    public static string DamageTypeDataIncomingHealing = Empty;

    public static string DamageTypeDataOutgoingPowerReplenish = Empty;

    public static string DamageTypeDataOutgoingPowerDamage = Empty;

    public static string DamageTypeDataOutgoingCures = Empty;

    private readonly DamageTypeData incAll;

    private readonly DamageTypeData outAll;

    private int cachedDeaths;

    private TimeSpan cachedDuration;

    private DateTime cachedEndTime;

    private int cachedKills;

    private DateTime cachedStartTime;

    private long cachedThreatDelta;

    private string cachedThreatStr;

    private bool deathsCached;

    private bool durationCached;

    private bool endTimeCached;

    private bool killsCached;

    private bool startTimeCached;

    private bool threatCached;

    public CombatantData(string combatantName, EncounterData Parent)
    {
        Name = combatantName;
        Items = new Dictionary<string, DamageTypeData>();
        foreach (var outgoingDamageTypeDataObject in OutgoingDamageTypeDataObjects)
        {
            outAll = new DamageTypeData(true, outgoingDamageTypeDataObject.Key, this);
            Items.Add(outgoingDamageTypeDataObject.Key, outAll);
        }

        foreach (var incomingDamageTypeDataObject in IncomingDamageTypeDataObjects)
        {
            incAll = new DamageTypeData(false, incomingDamageTypeDataObject.Key, this);
            Items.Add(incomingDamageTypeDataObject.Key, incAll);
        }

        Allies = new SortedList<string, int>();
        this.Parent = Parent;
        InvalidateCachedValues();
    }

    public EncounterData Parent { get; }

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

    public static string ColHeaderString => Join(",", ColHeaderCollection);

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

    public int Deaths
    {
        get
        {
            if (deathsCached) return cachedDeaths;

            if (!AllInc.TryGetValue(ActGlobals.Trans["specialAttackTerm-killing"], out var value))
            {
                if (AllInc.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out value))
                {
                    foreach (var swing in value.Items)
                        if (swing.Damage == Dnum.Death)
                            cachedDeaths++;
                }
                else
                    cachedDeaths = 0;
            }
            else
                cachedDeaths = value.Items.Count;

            deathsCached = true;
            return cachedDeaths;
        }
    }

    public int Kills
    {
        get
        {
            // Check if the kills have already been cached
            if (killsCached)
                return cachedKills;

            // Try to get the special "killing" attack type; if it doesn't exist, use the "all" attack type instead
            if (!AllOut.TryGetValue(ActGlobals.Trans["specialAttackTerm-killing"], out var value))
            {
                AllOut.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out value);
            }

            // If the value is still null, set the cached kills to 0 and return
            if (value == null)
            {
                cachedKills = 0;
                killsCached = true;
                return cachedKills;
            }

            // Check if the current entity is an ally
            var flag = false;
            if (Parent.GetAllies(true) != null)
            {
                flag = Parent != null && Parent.GetAllies(true).Contains(this);
            }

            // Iterate through the attack types and count the number of kills
            cachedKills = 0;
            try
            {
                foreach (var attackType in value.Items)
                {
                    if (attackType.Damage == Dnum.Death && (flag || !attackType.Victim.Contains(" ")))
                    {
                        cachedKills++;
                    }
                }
            }
            catch (InvalidOperationException)
            {
                // If an exception occurs while iterating, recursively call the Kills property until the value is retrieved
                return Kills;
            }

            // Set the killsCached flag to true and return the cachedKills value
            killsCached = true;
            return cachedKills;
        }
    }


    public string Name { get; }

    public DateTime StartTime
    {
        get
        {
            if (startTimeCached) return cachedStartTime;

            cachedStartTime = outAll.StartTime;
            startTimeCached = true;
            return cachedStartTime;
        }
    }

    public DateTime EndTime
    {
        get
        {
            if (endTimeCached) return cachedEndTime;

            cachedEndTime = outAll.EndTime;
            endTimeCached = true;
            return cachedEndTime;
        }
    }

    public DateTime ShortEndTime => Items[DamageTypeDataOutgoingDamage].EndTime;

    public DateTime EncStartTime => Parent.StartTime;

    public DateTime EncEndTime => Parent.EndTime;

    public TimeSpan Duration
    {
        get
        {
            if (Parent.StartTimes.Count <= 1)
            {
                if (EndTime > StartTime) return EndTime - StartTime;

                return TimeSpan.Zero;
            }

            if (durationCached) return cachedDuration;

            if (!AllOut.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var value)) return TimeSpan.Zero;

            value.Items.Sort(MasterSwing.CompareTime);

            var fightStartTimes = new List<DateTime>(Parent.StartTimes);
            var fightEndTimes = new List<DateTime>(Parent.EndTimes);
            if (fightStartTimes.Count < fightEndTimes.Count)
            {
                fightStartTimes.Add(StartTime);
                fightEndTimes.Add(EndTime);
            }

            var masterSwingTimes = new List<DateTime>(value.Items.Select(i => i.Time));

            var fightDuration = TimeSpan.Zero;
            var lastMasterSwingTime = DateTime.MinValue;

            for (var i = 0; i < fightStartTimes.Count; i++)
            {
                if (i >= fightEndTimes.Count) continue;
                var start = fightStartTimes[i];
                var end = fightEndTimes[i];

                var relevantMasterSwings = masterSwingTimes
                                           .SkipWhile(t => t < start)
                                           .TakeWhile(t => t <= end);

                foreach (var masterSwingTime in relevantMasterSwings)
                {
                    if (lastMasterSwingTime != DateTime.MinValue)
                    {
                        fightDuration += masterSwingTime - lastMasterSwingTime;
                    }

                    lastMasterSwingTime = masterSwingTime;
                }
            }

            cachedDuration = fightDuration;
            durationCached = true;
            return cachedDuration;
        }
    }


    public string DurationS => Duration.Hours == 0
                                   ? $"{Duration.Minutes:00}:{Duration.Seconds:00}"
                                   : $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

    public long Damage => Items[DamageTypeDataOutgoingDamage].Damage;

    public string DamagePercent
    {
        get
        {
            var result = "--";

            // Check if the entity is an ally and the total damage done is greater than zero
            if (!Parent.GetAllies().Contains(this) || Parent.Damage <= 0)
            {
                return result;
            }

            // Calculate the percentage of the total damage done
            var percent = (int)(Damage / (float)Parent.Damage * 100f);

            // Check if the percentage is valid (between 0 and 100)
            if (percent is < 0 or > 100)
            {
                return result;
            }

            result = percent + "%";
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

    public double DPS => Damage / Duration.TotalSeconds;

    public double EncDPS => Damage / Parent.Duration.TotalSeconds;

    public double ExtDPS => EncDPS;

    public double EncHPS => Healed / Parent.Duration.TotalSeconds;

    public double ExtHPS => EncHPS;

    public long DamageTaken => Items[DamageTypeDataIncomingDamage].Damage;

    public long Healed => Items[DamageTypeDataOutgoingHealing].Damage;

    public long HealsTaken => Items[DamageTypeDataIncomingHealing].Damage;

    public string HealedPercent
    {
        get
        {
            var result = "--";
            if (!Parent.GetAllies().Contains(this) || Parent.Healed <= 0)
            {
                return result;
            }

            var percentage = (int)(Healed / (float)Parent.Healed * 100f);
            if (percentage is < 0 or > 100)
            {
                return result;
            }

            result = percentage + "%";
            return result;
        }
    }

    public SortedList<string, AttackType> AllOut => outAll.Items;

    public SortedList<string, AttackType> AllInc => incAll.Items;

    public Dictionary<string, DamageTypeData> Items { get; set; }

    public SortedList<string, int> Allies { get; set; }

    public Dictionary<string, object> Tags { get; set; } = new();

    public int CompareTo(object? obj)
    {
        return CompareTo((CombatantData?)obj);
    }

    public int CompareTo(CombatantData? other)
    {
        // Initialize the comparison result to 0
        var comparisonResult = 0;

        // If the sorting column is defined, use its comparer to compare this object with the other one
        if (ColumnDefs.TryGetValue(ActGlobals.eDSort, out var value))
        {
            comparisonResult = value.SortComparer(this, other!);
        }

        // If the comparison result is still 0 and the secondary sorting column is defined, use its comparer
        if (comparisonResult == 0 && ColumnDefs.TryGetValue(ActGlobals.eDSort2, out var value2))
        {
            comparisonResult = value2.SortComparer(this, other!);
        }

        // If the comparison result is still 0, compare the damage dealt by each object
        if (comparisonResult == 0)
        {
            comparisonResult = Damage.CompareTo(other!.Damage);
        }

        return comparisonResult;
    }


    public bool Equals(CombatantData? other)
    {
        return string.Equals(Name, other!.Name, StringComparison.CurrentCultureIgnoreCase);
    }

    public void InvalidateCachedValues()
    {
        durationCached = false;
        deathsCached = false;
        killsCached = false;
        startTimeCached = false;
        endTimeCached = false;
        threatCached = false;
    }

    public void InvalidateCachedValues(bool Recursive)
    {
        InvalidateCachedValues();
        if (!Recursive) return;

        foreach (var item in Items)
            item.Value.InvalidateCachedValues(true);
    }

    public void Trim()
    {
        foreach (var item in Items)
            item.Value.Trim();
    }

    public void AddCombatAction(MasterSwing action)
    {
        // Reset cached values
        durationCached = false;
        startTimeCached = false;
        endTimeCached = false;
        threatCached = false;
        killsCached = false;

        // Get the name of the combatant who was attacked
        var combatant = action.Victim.ToUpper();

        // Check if the swing type is supported
        if (SwingTypeToDamageTypeDataLinksOutgoing.ContainsKey(action.SwingType))
        {
            // Loop through the damage types linked to the swing type
            foreach (var damageTypeLink in SwingTypeToDamageTypeDataLinksOutgoing[action.SwingType])
            {
                // Get the damage type data
                var damageTypeData = Items[damageTypeLink];

                // Get the ally value for the damage type
                var allyValue = OutgoingDamageTypeDataObjects[damageTypeData.Type].AllyValue;

                // Modify the ally status of the combatant
                ModAlly(combatant, allyValue);

                // Add the combat action to the damage type data for the "all" attack type
                damageTypeData.AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);

                // Add the combat action to the damage type data for the specific attack type (if allowed)
                if (!ActGlobals.restrictToAll)
                {
                    damageTypeData.AddCombatAction(action, action.AttackType);
                }
            }

            // Add the combat action to the "all" damage type data for the "all" attack type
            outAll.AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);

            // Add the combat action to the "all" damage type data for the specific attack type (if allowed)
            if (!ActGlobals.restrictToAll)
            {
                outAll.AddCombatAction(action, action.AttackType);
            }
        }
    }

    public void AddReverseCombatAction(MasterSwing action)
    {
        // Reset cached values
        durationCached = false;
        deathsCached = false;
        startTimeCached = false;
        endTimeCached = false;

        // Get the name of the attacker and make it uppercase
        var combatant = action.Attacker.ToUpper();

        // Check if there are any damage type links for this swing type
        if (!SwingTypeToDamageTypeDataLinksIncoming.ContainsKey(action.SwingType))
            return;

        // For each damage type link, add the combat action to the corresponding damage type and modify the ally value
        var links = SwingTypeToDamageTypeDataLinksIncoming[action.SwingType];
        foreach (var link in links)
        {
            var damageTypeData = Items[link];
            var allyValue = IncomingDamageTypeDataObjects[damageTypeData.Type].AllyValue;
            ModAlly(combatant, allyValue);
            Items[link].AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);

            // If the "restrict to all" option is disabled, add the combat action to the specific attack type as well
            if (!ActGlobals.restrictToAll)
                Items[link].AddCombatAction(action, action.AttackType);
        }

        // Add the combat action to the incoming damage for all damage types and the specific attack type
        incAll.AddCombatAction(action, ActGlobals.Trans["attackTypeTerm-all"]);
        incAll.AddCombatAction(action, action.AttackType);
    }


    public string GetMaxHit(bool ShowType = true, bool UseSuffix = true)
    {
        MasterSwing masterSwing = null!;
        var attackType = GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], DamageTypeDataOutgoingDamage);
        if (attackType != null)
        {
            foreach (var t in attackType.Items)
                if (masterSwing == null || (long)t.Damage > (long)masterSwing.Damage)
                    masterSwing = t;
        }

        if (masterSwing == null)
            return Empty;
        return ShowType
                   ? $"{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, true)}"
                   : $"{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, false)}";
    }

    public string GetMaxHeal(bool ShowType = true, bool CountWards = false, bool UseSuffix = true)
    {
        MasterSwing masterSwing = null!;
        var attackType = GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], DamageTypeDataOutgoingHealing);
        if (attackType != null)
        {
            foreach (var swing in attackType.Items.Where(swing =>
                                                             (CountWards || swing.DamageType !=
                                                              ActGlobals.Trans["specialAttackTerm-wardAbsorb"]) &&
                                                             (masterSwing == null || (long)swing.Damage >
                                                              (long)masterSwing.Damage)))
                masterSwing = swing;
        }

        if (masterSwing == null) return Empty;

        return ShowType
                   ? $"{masterSwing.AttackType}-{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, true)}"
                   : $"{ActGlobals.oFormActMain.CreateDamageString(masterSwing.Damage, UseSuffix, true)}";
    }

    public int GetCombatantType()
    {
        if (!Parent.GetAllies().Contains(this))
            return 0; // Combatant is an enemy

        var damage = Items[DamageTypeDataOutgoingDamage].Damage;
        var healing = Items[DamageTypeDataOutgoingHealing].Damage;
        var nonSkillDamage = Items[DamageTypeDataNonSkillDamage].Damage;
        var incomingHealing = Items[DamageTypeDataIncomingHealing].Damage;

        if (incomingHealing > damage / 3 && damage > healing)
            return 1; // Combatant is a healer

        if (healing > damage / 3 && healing > incomingHealing)
            return 2; // Combatant is a damage dealer with healing abilities

        if (nonSkillDamage > damage / 10)
            return 3; // Combatant deals significant non-skill damage

        return 4; // Combatant is a regular damage dealer
    }


    public long GetMaxHealth()
    {
        var damageTaken = DamageTaken;
        var healsTaken = HealsTaken;

        if (AllInc.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var allItems))
        {
            // Get a list of all the swings in the 'all' attack type category
            List<MasterSwing> allSwings = new List<MasterSwing>(allItems.Items);
            allSwings.Sort(MasterSwing.CompareTime);

            var maxNegativeDamage = 0L;
            var runningTotal = 0L;

            foreach (var swing in allSwings)
            {
                long swingDamage = swing.Damage;

                if (DamageSwingTypes.Contains(swing.SwingType) && swingDamage > 0) runningTotal -= swingDamage;

                if (HealingSwingTypes.Contains(swing.SwingType) && swingDamage > 0) runningTotal += swingDamage;

                if (runningTotal > 0) runningTotal = 0;

                if (runningTotal < maxNegativeDamage) maxNegativeDamage = runningTotal;
            }

            healsTaken += maxNegativeDamage;
        }

        return Math.Abs(damageTaken - healsTaken);
    }


    public string GetColumnByName(string name)
    {
        return ColumnDefs.TryGetValue(name, out var value) ? value.GetCellData(this) : Empty;
    }

    public AttackType? GetAttackType(string AttackTypeName, string Type)
    {
        return Items[Type].Items.TryGetValue(AttackTypeName, out var value) ? value : null;
    }

    public long GetThreatDelta(string DamageTypeDataLabel)
    {
        if (threatCached)
        {
            // Return the cached value if it's available
            return cachedThreatDelta;
        }

        // Initialize variables
        long increaseThreat = 0;
        long decreaseThreat = 0;
        int increaseThreatPosition = 0;
        int decreaseThreatPosition = 0;

        // Get the swings in the specified damage type category
        if (Items[DamageTypeDataLabel].Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var swings))
        {
            foreach (var swing in swings.Items)
            {
                long swingDamage = swing.Damage;

                if (swingDamage > 0)
                {
                    // The swing was a damage swing
                    if (swing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"])
                    {
                        // The swing increased threat
                        increaseThreat += swingDamage;
                    }
                    else
                    {
                        // The swing decreased threat
                        decreaseThreat += swingDamage;
                    }
                }
                else if ((int)swing.Damage == (int)Dnum.ThreatPosition)
                {
                    // The swing was a threat swing
                    var length = swing.Damage.DamageString.IndexOf(' ');
                    var threatValue = int.Parse(swing.Damage.DamageString[..length]);

                    if (swing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"])
                    {
                        // The swing increased threat position
                        increaseThreatPosition += threatValue;
                    }
                    else
                    {
                        // The swing decreased threat position
                        decreaseThreatPosition += threatValue;
                    }
                }
            }
        }

        // Calculate the threat delta and cache the value
        cachedThreatDelta = increaseThreat - decreaseThreat;
        threatCached = true;

        // Format the threat string and return the threat delta
        cachedThreatStr = $@"+({increaseThreatPosition}){increaseThreat}/-({decreaseThreatPosition}){decreaseThreat}";
        return cachedThreatDelta;
    }


    public string GetThreatStr(string DamageTypeDataLabel)
    {
        if (threatCached)
            return cachedThreatStr;

        var increase = 0L;
        var decrease = 0L;
        var increaseCount = 0;
        var decreaseCount = 0;

        if (Items.TryGetValue(DamageTypeDataLabel, out var damageTypeData))
        {
            if (damageTypeData.Items.TryGetValue(ActGlobals.Trans["attackTypeTerm-all"], out var allInc))
            {
                foreach (var masterSwing in allInc.Items)
                {
                    if ((long)masterSwing.Damage > 0)
                    {
                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"])
                        {
                            increase += masterSwing.Damage;
                        }
                        else
                        {
                            decrease += masterSwing.Damage;
                        }
                    }
                    else if ((int)(long)masterSwing.Damage == (int)(long)Dnum.ThreatPosition)
                    {
                        var length = masterSwing.Damage.DamageString.IndexOf(' ');
                        var value = int.Parse(masterSwing.Damage.DamageString[..length]);

                        if (masterSwing.DamageType == ActGlobals.Trans["specialAttackTerm-increase"])
                        {
                            increaseCount += value;
                        }
                        else
                        {
                            decreaseCount += value;
                        }
                    }
                }
            }
        }

        cachedThreatDelta = increase - decrease;
        cachedThreatStr = $@"+({increaseCount}){increase}/-({decreaseCount}){decrease}";
        threatCached = true;

        return cachedThreatStr;
    }


    public override bool Equals(object? obj)
    {
        return Name.ToLower().Equals(((CombatantData)obj!).Name.ToLower());
    }

    public override int GetHashCode()
    {
        try
        {
            return Items.Values.Aggregate(0L, (current, value) => current + value.GetHashCode()).GetHashCode();
        }
        catch (InvalidOperationException)
        {
            return GetHashCode();
        }
    }

    public override string ToString() => Name;

    public void ModAlly(string Combatant, int Mod)
    {
        if (Name == "Unknown" || Combatant == "UNKNOWN") return;
        if (!Allies.ContainsKey(Combatant))
            Allies.Add(Combatant, 0);

        if (Mod == 0) return;
        Allies[Combatant] += Mod;
        Parent.SetAlliesUncached();
    }

    internal static int CompareDamageTakenTime(CombatantData? Left, CombatantData? Right)
    {
        var compareTo = Left?.DamageTaken.CompareTo(Right?.DamageTaken) ?? 0;
        if (compareTo == 0)
            compareTo = Compare(Left?.Name, Right?.Name, StringComparison.Ordinal);
        return compareTo;
    }

    public class TextExportFormatter
    {
        public ExportStringDataCallback GetExportString;

        public TextExportFormatter(
            string Name, string Label, string Description, ExportStringDataCallback FormatterCallback)
        {
            this.Name = Name;
            this.Label = Label;
            this.Description = Description;
            GetExportString = FormatterCallback;
        }

        public string Label { get; }

        public string Description { get; }

        public string Name { get; }
    }

    public class ColumnDef
    {
        public ColorDataCallback GetCellBackColor = Data => Color.Transparent;
        public StringDataCallback GetCellData;

        public ColorDataCallback GetCellForeColor = Data => Color.Transparent;

        public StringDataCallback GetSqlData;

        public Comparison<CombatantData> SortComparer;

        public ColumnDef(
            string Label, bool DefaultVisible, string SqlDataType, string SqlDataName,
            StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback,
            Comparison<CombatantData> SortComparer)
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

    public class DamageTypeDef
    {
        public DamageTypeDef(string Label, int AllyValue, Color TypeColor)
        {
            this.Label = Label;
            this.AllyValue = AllyValue;
            this.TypeColor = TypeColor;
        }

        public string Label { get; }

        public int AllyValue { get; }

        public Color TypeColor { get; }
    }
}
