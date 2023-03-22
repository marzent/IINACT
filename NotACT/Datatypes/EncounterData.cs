namespace Advanced_Combat_Tracker;

public class EncounterData
{
    public delegate Color ColorDataCallback(EncounterData Data);

    public delegate string ExportStringDataCallback(
        EncounterData Data, List<CombatantData> SelectiveAllies, string ExtraFormat);

    public delegate string StringDataCallback(EncounterData Data);

    public static Dictionary<string, TextExportFormatter> ExportVariables = new();

    public static Dictionary<string, ColumnDef> ColumnDefs = new();

    private bool alliesCached;

    private DateTime alliesLastCall = DateTime.Now;

    private bool alliesManual;

    private List<CombatantData> cachedAllies;

    private string cachedEncId;

    private bool encIdCached;

    private List<DateTime> endTimes = new();

    private readonly bool ignoreEnemies;

    private readonly HashSet<int> includedTimeSorters = new();

    private readonly bool sParsing;

    private List<DateTime> startTimes = new();

    private string title = ActGlobals.Trans["encounterData-defaultEncounterName"];

    private string zoneName;

    public EncounterData(string CharName, string ZoneName, bool IgnoreEnemies, ZoneData Parent)
    {
        sParsing = true;
        ignoreEnemies = IgnoreEnemies;
        this.CharName = CharName;
        zoneName = ZoneName;
        this.Parent = Parent;
    }

    public EncounterData(string CharName, string ZoneName, ZoneData Parent)
    {
        sParsing = false;
        ignoreEnemies = false;
        this.CharName = CharName;
        zoneName = ZoneName;
        this.Parent = Parent;
    }

    public HistoryRecord HistoryRecord { get; set; }

    public bool DuplicateDetection { get; set; }

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

    public ZoneData Parent { get; set; }

    public string CharName { get; set; }

    public string ZoneName
    {
        get
        {
            if (zoneName == ActGlobals.Trans["mergedEncounterTerm-all"] && Parent != null) return Parent.ZoneName;

            return zoneName;
        }
        set => zoneName = value;
    }

    public bool Active { get; set; }

    public List<DateTime> StartTimes
    {
        get
        {
            for (var i = startTimes.IndexOf(DateTime.MaxValue);
                 i >= 0;
                 i = startTimes.IndexOf(DateTime.MaxValue))
                startTimes.RemoveAt(i);

            return startTimes;
        }
        set => startTimes = value;
    }

    public List<DateTime> EndTimes
    {
        get
        {
            for (var i = endTimes.IndexOf(DateTime.MinValue); i >= 0; i = endTimes.IndexOf(DateTime.MinValue))
                endTimes.RemoveAt(i);

            return endTimes;
        }
        set => endTimes = value;
    }

    public string Title
    {
        get => zoneName == ActGlobals.Trans["mergedEncounterTerm-all"]
                   ? ActGlobals.Trans["mergedEncounterTerm-all"]
                   : title;
        set => title = value;
    }

    public DateTime StartTime
    {
        get
        {
            var dateTime = DateTime.MaxValue;
            for (var i = 0; i < Items.Count; i++)
            {
                var combatantData = Items.Values[i];
                if (combatantData.StartTime < dateTime) dateTime = combatantData.StartTime;
            }

            return dateTime;
        }
    }

    public DateTime EndTime
    {
        get
        {
            if (ActGlobals.longDuration)
            {
                var dateTime = DateTime.MinValue;
                for (var i = 0; i < Items.Count; i++)
                {
                    var combatantData = Items.Values[i];
                    if (combatantData.EndTime > dateTime) dateTime = combatantData.EndTime;
                }

                return dateTime;
            }

            return ShortEndTime;
        }
    }

    public DateTime ShortEndTime
    {
        get
        {
            var dateTime = DateTime.MinValue;
            var allies = !ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values);
            if (allies.Count == 0) allies = new List<CombatantData>(Items.Values);

            foreach (var combatantData in allies)
                if (combatantData.ShortEndTime > dateTime)
                    dateTime = combatantData.ShortEndTime;

            return dateTime;
        }
    }

    public TimeSpan Duration
    {
        get
        {
            if (StartTimes.Count > 1)
            {
                try
                {
                    var duration = default(TimeSpan);
                    for (var i = 0; i < StartTimes.Count; i++)
                        if (EndTimes.Count == i)
                            duration += EndTime - StartTimes[i];
                        else
                            duration += EndTimes[i] - StartTimes[i];

                    return duration;
                }
                catch
                {
                    return TimeSpan.Zero;
                }
            }

            if (EndTime > StartTime) return EndTime - StartTime;

            return TimeSpan.Zero;
        }
    }

    public string DurationS => Duration.Hours == 0
                                   ? $"{Duration.Minutes:00}:{Duration.Seconds:00}"
                                   : $"{Duration.Hours:00}:{Duration.Minutes:00}:{Duration.Seconds:00}";

    public long Damage =>
        (!ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(t => t.Damage);

    public int AlliedKills =>
        (!ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(
            combatantData => combatantData.Kills);

    public int AlliedDeaths => (!ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values))
                               .Where(combatantData => !combatantData.Name.Contains(" "))
                               .Sum(combatantData => combatantData.Deaths);

    public long Healed =>
        (!ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values)).Sum(
            combatantData => combatantData.Healed);

    public double DPS => Damage / Duration.TotalSeconds;

    public string EncId
    {
        get
        {
            if (encIdCached) return cachedEncId;

            try
            {
                cachedEncId = GetHashCode().ToString("x8");
            }
            catch (InvalidOperationException)
            {
                return cachedEncId ?? "";
            }

            encIdCached = true;
            return cachedEncId;
        }
    }

    public int NumCombatants => Items.Count;

    public int NumAllies => GetAllies().Count;

    public int NumEnemies => NumCombatants - NumAllies;

    public SortedList<string, CombatantData> Items { get; set; } = new();

    public List<LogLineEntry> LogLines { get; set; } = new();

    public Dictionary<string, object> Tags { get; set; } = new();

    public string GetColumnByName(string name) => 
        ColumnDefs.TryGetValue(name, out var value) ? value.GetCellData(this) : string.Empty;

    public void Trim()
    {
        Items.TrimExcess();
        for (var i = 0; i < Items.Count; i++) 
            Items.Values[i].Trim();
    }

    public void AddCombatAction(MasterSwing action)
    {
        // Check if we need to avoid duplicate actions
        if (DuplicateDetection && includedTimeSorters.Contains(action.TimeSorter))
        {
            // If this action is already included, do nothing
            return;
        }

        // Add the action's time sorter to the included time sorters collection
        if (DuplicateDetection) 
            includedTimeSorters.Add(action.TimeSorter);

        // Set the parent encounter of the action to the current instance
        action.ParentEncounter = this;

        // Invalidate any cached values
        InvalidateCachedValues();

        // Get the attacker and victim names in uppercase
        var attackerName = action.Attacker.ToUpper();
        var victimName = action.Victim.ToUpper();

        // Check if we should skip parsing based on selective lists and ignoreEnemies setting
        var shouldSkipParsing =
            !sParsing ||
            ActGlobals.oFormActMain.SelectiveListGetSelected(attackerName) ||
            (ActGlobals.oFormActMain.SelectiveListGetSelected(victimName) && !ignoreEnemies);

        // Add the action to the appropriate combatant's collection
        if (shouldSkipParsing)
        {
            if (!Items.TryGetValue(attackerName, out var combatant))
            {
                // If this is a new combatant, create a new CombatantData object and add it to the dictionary
                combatant = new CombatantData(action.Attacker, this);
                Items.Add(attackerName, combatant);
            }

            // Add the action to the combatant's collection
            combatant.AddCombatAction(action);
        }

        // Add the reverse combat action if parsing is not skipped
        if (shouldSkipParsing)
        {
            AddReverseCombatAction(action);
        }
    }


    public void InvalidateCachedValues()
    {
        encIdCached = false;
    }

    public void InvalidateCachedValues(bool Recursive)
    {
        InvalidateCachedValues();
        if (!Recursive) return;
        for (var i = 0; i < Items.Count; i++) 
            Items.Values[i].InvalidateCachedValues(true);
    }

    private void AddReverseCombatAction(MasterSwing action)
    {
        // Get the victim name in uppercase
        var victimName = action.Victim.ToUpper();

        // Look up the victim combatant in the dictionary
        if (!Items.TryGetValue(victimName, out var victimCombatant))
        {
            // If the victim combatant is not found, create a new CombatantData object and add it to the dictionary
            victimCombatant = new CombatantData(action.Victim, this);
            Items.Add(victimName, victimCombatant);
        }

        // Add the reverse combat action to the victim combatant
        victimCombatant.AddReverseCombatAction(action);
    }


    public void EndCombat(bool Finalize)
    {
        lock (ActGlobals.ActionDataLock)
        {
            Active = false;
            EndTimes.Add(StartTimes[EndTimes.Count] < EndTime ? EndTime : StartTimes[EndTimes.Count]);
            if (!Finalize) return;
            Trim();
            Title = GetStrongestEnemy(ActGlobals.charName)!;
        }
    }

    public void SetAlliesUncached()
    {
        if (!alliesManual) alliesCached = false;
    }

    public void SetAllies(List<CombatantData> allies)
    {
        if (allies == null || allies.Count == 0)
        {
            alliesCached = false;
            alliesManual = false;
        }
        else
        {
            cachedAllies = allies;
            alliesCached = true;
            alliesManual = true;
        }
    }

    public List<CombatantData> GetAllies() => GetAllies(false);

    public List<CombatantData> GetAllies(bool allowLimited)
    {
        if (alliesCached || (allowLimited && DateTime.Now.Second == alliesLastCall.Second) ||
            (cachedAllies != null && Active && Title == ActGlobals.Trans["mergedEncounterTerm-all"]))
        {
            return cachedAllies;
        }

        if (GetIgnoreEnemies())
        {
            return new List<CombatantData>(Items.Values);
        }

        var combatant = GetCombatant(CharName);
        if (combatant == null)
        {
            return new List<CombatantData>();
        }

        var sortedAllies = new SortedList<string, AllyObject>
        {
            { combatant.Name.ToUpper(), new AllyObject(combatant) }
        };

        var listChanged = true;
        while (listChanged)
        {
            listChanged = false;

            for (var i = 0; i < sortedAllies.Count; i++)
            {
                foreach (var (name, value) in sortedAllies.Values[i].cd.Allies)
                {
                    if (!sortedAllies.ContainsKey(name))
                    {
                        var combatant2 = GetCombatant(name);
                        if (combatant2 == null)
                        {
                            continue;
                        }

                        sortedAllies.Add(name, new AllyObject(combatant2));
                        listChanged = true;
                    }

                    sortedAllies[name].allyVal += sortedAllies.Values[i].allyVal > 0 ? value : -value;
                }
            }
        }

        var list = new List<CombatantData>();
        var thisCombatantIsEnemy = sortedAllies[combatant.Name.ToUpper()].allyVal < 0;

        foreach (var ally in sortedAllies)
        {
            if (thisCombatantIsEnemy)
            {
                if (ally.Value.allyVal < 0)
                {
                    list.Add(ally.Value.cd);
                }
            }
            else if (ally.Value.allyVal > 0)
            {
                list.Add(ally.Value.cd);
            }
        }

        list.RemoveAll(item => item == null);

        cachedAllies = list;
        alliesCached = true;
        alliesLastCall = DateTime.Now;

        return cachedAllies;
    }


    public CombatantData? GetCombatant(string? Name) => 
        Name == null ? null : Items.TryGetValue(Name.ToUpper(), out var value) ? value : null;

    public int GetEncounterSuccessLevel()
    {
        if (sParsing && ignoreEnemies)
            return 0;

        var allies = GetAllies();
        if (allies.Count == 0)
            return 0;

        var strongestEnemy = GetStrongestEnemy(CharName);
        var strongestEnemyCombatant = GetCombatant(strongestEnemy);
        if (strongestEnemyCombatant == null)
            return 0;

        var isEnemyDefeated = strongestEnemyCombatant.Deaths > 0;
        var areAlliesAlive = allies.Any(combatantData =>
                                            combatantData.Deaths == 0 && combatantData.Name != "Unknown" &&
                                            !combatantData.Name.Contains(" "));

        if (isEnemyDefeated && areAlliesAlive)
            return 1;
        if (isEnemyDefeated || areAlliesAlive)
            return 2;

        return 3;
    }


    public string? GetStrongestEnemy(string combatantName)
    {
        if (sParsing && ignoreEnemies)
        {
            return ActGlobals.Trans["encounterData-defaultEncounterName"];
        }

        var allies = GetAllies();
        if (allies.Count == 0)
        {
            return ActGlobals.Trans["encounterData-defaultEncounterName"];
        }

        var enemies = Items.Values
                           .Where(c => !allies.Contains(c))
                           .Select(c => new
                           {
                               Name = c.Name,
                               DamagePerDeath = c.Deaths > 0 ? c.DamageTaken / c.Deaths : c.DamageTaken
                           })
                           .OrderByDescending(c => c.DamagePerDeath)
                           .ToList();

        return enemies.Count == 0 ? null : enemies[0].Name;
    }


    public string GetMaxHit(bool ShowType = true, bool UseSuffix = true)
    {
        var allies = ignoreEnemies ? new List<CombatantData>(Items.Values) : GetAllies();

        var maxSwing = allies
                       .SelectMany(combatant => combatant.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"],
                                                                        CombatantData.DamageTypeDataOutgoingDamage)?.Items!)
                       .Where(swing => swing.Damage > 0).MaxBy(swing => swing.Damage);

        if (maxSwing == null)
            return string.Empty;

        var arg = allies.FirstOrDefault(combatant => combatant.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"],
                                            CombatantData.DamageTypeDataOutgoingDamage)?.Items.Contains(maxSwing) ?? false)?.Name;

        if (arg == null)
            return string.Empty;

        var damageString = ActGlobals.oFormActMain.CreateDamageString(maxSwing.Damage, UseSuffix, !ShowType);
        return ShowType
                   ? $"{arg}-{maxSwing.AttackType}-{damageString}"
                   : $"{arg}-{damageString}";
    }


    public string GetMaxHeal(bool ShowType = true, bool CountWards = true, bool UseSuffix = true)
    {
        var allies = !ignoreEnemies ? GetAllies() : new List<CombatantData>(Items.Values);
        var maxHealSwing = allies
                           .Where(a => a.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], CombatantData.DamageTypeDataOutgoingHealing) != null)
                           .SelectMany(a => a.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"], CombatantData.DamageTypeDataOutgoingHealing)?.Items!)
                           .Where(s => CountWards || s.DamageType != ActGlobals.Trans["specialAttackTerm-wardAbsorb"]).MaxBy(s => s.Damage);

        if (maxHealSwing == null)
            return string.Empty;

        var combatantName =
            allies.FirstOrDefault(
                a => a.GetAttackType(ActGlobals.Trans["attackTypeTerm-all"],
                                     CombatantData.DamageTypeDataOutgoingHealing)?.Items.Contains(maxHealSwing) ??
                     false)?.Name ?? string.Empty;
        var damageString = ActGlobals.oFormActMain.CreateDamageString(maxHealSwing.Damage, UseSuffix, ShowType);

        return ShowType
                   ? $"{combatantName}-{maxHealSwing.AttackType}-{damageString}"
                   : $"{combatantName}-{damageString}";
    }
    
    public bool GetIsSelective() => sParsing;

    public bool GetIgnoreEnemies() => ignoreEnemies;

    public override string ToString()
    {
        if (StartTime == DateTime.MaxValue) return $"{Title} - [{DurationS}]";

        return (DateTime.Now - StartTime).TotalHours > 12.0
                   ? string.Format("{0} - [{1}] ({3}) {2}", Title, DurationS, StartTime.ToLongTimeString(),
                                   StartTime.ToShortDateString())
                   : $"{Title} - [{DurationS}] {StartTime.ToLongTimeString()}";
    }

    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        var encounterData = (EncounterData)obj;
        return ToString().Equals(encounterData.ToString());
    }


    public override int GetHashCode()
    {
        var items = new List<CombatantData>(Items.Values);
        var num = items.Aggregate(0L, (current, combatantData) => current + combatantData.GetHashCode());
        return num.GetHashCode();
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

    private class AllyObject
    {
        public int allyVal;
        public readonly CombatantData cd;

        public AllyObject(CombatantData combatant)
        {
            cd = combatant;
            allyVal = 0;
        }

        public override string ToString()
        {
            return allyVal.ToString();
        }
    }
}
