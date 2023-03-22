using System.Diagnostics;

namespace Advanced_Combat_Tracker;

public class MasterSwing : IComparable, IComparable<MasterSwing>
{
    public delegate Color ColorDataCallback(MasterSwing Data);

    public delegate string StringDataCallback(MasterSwing Data);

    public static Dictionary<string, ColumnDef> ColumnDefs = new();

    internal string attacker;

    internal string attackType;

    internal bool critical;

    internal Dnum damage;

    internal string damageType;

    internal string special;

    internal int swingType;

    internal DateTime time;

    internal int timeSorter;

    internal string victim;

    public MasterSwing(
        int SwingType, bool Critical, Dnum damage, DateTime Time, int TimeSorter, string theAttackType,
        string Attacker, string theDamageType, string Victim)
    {
        time = Time;
        this.damage = damage;
        attacker = Attacker;
        victim = Victim;
        attackType = theAttackType;
        damageType = theDamageType;
        critical = Critical;
        timeSorter = TimeSorter;
        swingType = SwingType;
        special = "specialAttackTerm-none";
    }

    public MasterSwing(
        int SwingType, bool Critical, string Special, Dnum damage, DateTime Time, int TimeSorter,
        string theAttackType, string Attacker, string theDamageType, string Victim)
    {
        time = Time;
        this.damage = damage;
        attacker = Attacker;
        victim = Victim;
        attackType = theAttackType;
        damageType = theDamageType;
        critical = Critical;
        timeSorter = TimeSorter;
        swingType = SwingType;
        special = Special;
    }

    public string Special => special;

    public EncounterData ParentEncounter { get; set; }

    public DateTime Time => time;

    public int TimeSorter => timeSorter;

    public int SwingType => swingType;

    public Dnum Damage => damage;

    public string Attacker => attacker;

    public string Victim => victim;

    public string AttackType => attackType;

    public string DamageType => damageType;

    public bool Critical
    {
        get => critical;
        set => critical = value;
    }

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

    public Dictionary<string, object> Tags { get; set; } = new();

    public int CompareTo(object? obj)
    {
        return CompareTo((MasterSwing?)obj);
    }

    public int CompareTo(MasterSwing? other)
    {
        Debug.Assert(other != null, nameof(other) + " != null");

        // Compare based on the sort column defined in ActGlobals.aTSort.
        if (ColumnDefs.TryGetValue(ActGlobals.aTSort, out var sortColumn))
        {
            var result = sortColumn.SortComparer(this, other);
            if (result != 0) return result;
        }

        // Compare based on the secondary sort column defined in ActGlobals.aTSort2.
        if (ColumnDefs.TryGetValue(ActGlobals.aTSort2, out var secondarySortColumn))
        {
            var result = secondarySortColumn.SortComparer(this, other);
            if (result != 0) return result;
        }

        // Compare based on the time sorter.
        var timeSorterResult = TimeSorter.CompareTo(other.TimeSorter);
        if (timeSorterResult != 0) return timeSorterResult;

        // If all else fails, compare based on the time of the swings.
        return Time.CompareTo(other.Time);
    }


    public string GetColumnByName(string name) => 
        ColumnDefs.ContainsKey(name) ? ColumnDefs[name].GetCellData(this) : string.Empty;

    public override string ToString() => 
        $"{Time:s}|{Damage}|{Attacker}|{Special}|{AttackType}|{DamageType}|{Victim}";

    public override bool Equals(object? obj)
    {
        var masterSwing = (MasterSwing)obj!;
        var text = ToString();
        var value = masterSwing.ToString();
        return text.Equals(value);
    }

    public override int GetHashCode() => 
        ToString().GetHashCode();

    internal static int CompareTime(MasterSwing Left, MasterSwing Right)
    {
        var timeSorterComparison = Left.TimeSorter.CompareTo(Right.TimeSorter);
        return timeSorterComparison != 0 ? timeSorterComparison : Left.Time.CompareTo(Right.Time);
    }


    public class ColumnDef
    {
        public ColorDataCallback GetCellBackColor = Data => Color.Transparent;
        public StringDataCallback GetCellData;

        public ColorDataCallback GetCellForeColor = Data => Color.Transparent;

        public StringDataCallback GetSqlData;

        public Comparison<MasterSwing> SortComparer;

        public ColumnDef(
            string Label, bool DefaultVisible, string SqlDataType, string SqlDataName,
            StringDataCallback CellDataCallback, StringDataCallback SqlDataCallback,
            Comparison<MasterSwing> SortComparer)
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

    public class DualComparison : IComparer<MasterSwing>
    {
        private readonly string sort1;

        private readonly string sort2;

        public DualComparison(string Sort1, string Sort2)
        {
            sort1 = Sort1;
            sort2 = Sort2;
        }

        public int Compare(MasterSwing? Left, MasterSwing? Right)
        {
            Debug.Assert(Left != null, nameof(Left) + " != null");
            Debug.Assert(Right != null, nameof(Right) + " != null");

            if (ColumnDefs.TryGetValue(sort1, out var comparer1))
            {
                var result = comparer1.SortComparer(Left, Right);
                if (result != 0) return result;
            }

            if (ColumnDefs.TryGetValue(sort2, out var comparer2))
            {
                var result = comparer2.SortComparer(Left, Right);
                if (result != 0) return result;
            }

            return Left.TimeSorter == Right.TimeSorter
                       ? Left.Time.CompareTo(Right.Time)
                       : Left.TimeSorter.CompareTo(Right.TimeSorter);
        }

    }
}
