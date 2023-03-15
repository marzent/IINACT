namespace Advanced_Combat_Tracker;

public class ZoneData : IComparable<ZoneData>
{
    public ZoneData(DateTime Start, string ZoneName, bool PopulateAll, bool FullSelective, bool IgnoreEnemies)
    {
        StartTime = Start;
        this.ZoneName = ZoneName;
        Items = new List<EncounterData>
        {
            FullSelective
                ? new EncounterData(ActGlobals.charName, ActGlobals.Trans["mergedEncounterTerm-all"], IgnoreEnemies,
                                    this)
                : new EncounterData(ActGlobals.charName, ActGlobals.Trans["mergedEncounterTerm-all"], this)
        };
        this.PopulateAll = PopulateAll;
        if (!this.PopulateAll) return;
    }

    public bool PopulateAll { get; set; }

    public DateTime StartTime { get; set; }

    public string ZoneName { get; set; }

    public List<EncounterData> Items { get; set; }

    public EncounterData ActiveEncounter { get; set; }

    public Dictionary<string, object> Tags { get; set; } = new();

    public int CompareTo(ZoneData? other)
    {
        return StartTime.CompareTo(other?.StartTime);
    }

    public void AddCombatAction(MasterSwing action)
    {
        if (PopulateAll)
        {
            if (!Items[0].Active)
            {
                Items[0].StartTimes.Add(action.Time);
                Items[0].Active = true;
            }

            Items[0].AddCombatAction(action);
        }

        if (!Items[^1].Active)
        {
            Items[^1].StartTimes.Add(action.Time);
            Items[^1].Active = true;
        }

        Items[^1].AddCombatAction(action);
    }

    public override string ToString()
    {
        return ZoneName == ActGlobals.Trans["zoneDataTerm-import"]
                   ? string.Format(ActGlobals.Trans["zoneDataTerm-importMerge"], Items.Count)
                   : !PopulateAll
                       ? string.Format("{0} - [{2}] {1}", ZoneName, StartTime.ToLongTimeString(), Items.Count)
                       : string.Format("{0} - [{2}] {1}", ZoneName, StartTime.ToLongTimeString(), Items.Count - 1);
    }
}
