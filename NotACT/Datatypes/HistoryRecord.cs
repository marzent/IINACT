namespace Advanced_Combat_Tracker;

public class HistoryRecord : IComparable<HistoryRecord>, IEquatable<HistoryRecord>
{
    public HistoryRecord(
        int Type, DateTime StartTime, DateTime EndTime, string Label, string CharName, string FolderHint = "")
    {
        this.Type = Type;
        this.StartTime = StartTime;
        this.EndTime = EndTime;
        this.Label = Label;
        this.CharName = CharName;
        this.FolderHint = FolderHint;
    }

    public TimeSpan Duration => EndTime - StartTime;

    public string CharName { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Type { get; set; }

    public string Label { get; set; }

    public string FolderHint { get; set; }

    public int CompareTo(HistoryRecord? other)
    {
        return StartTime.CompareTo(other!.StartTime);
    }

    public bool Equals(HistoryRecord? other)
    {
        return StartTime.Equals(other!.StartTime);
    }

    public override bool Equals(object? obj)
    {
        if (obj == DBNull.Value) return false;

        if (obj == null) return false;

        var historyRecord = (HistoryRecord)obj;
        return StartTime.Equals(historyRecord.StartTime);
    }

    public override string ToString()
    {
        var timeSpan = EndTime - StartTime;
        var text = !(timeSpan.TotalHours > 1.0)
                       ? $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
                       : $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
        var text2 = Type != 1 ? string.Empty : "     ";
        var text3 = ActGlobals.oFormActMain.LogFilePath.ToLower() != FolderHint.ToLower()
                        ? FolderHint
                        : string.Empty;
        return $"{text2}{Label} - {StartTime.ToShortDateString()} {StartTime.ToLongTimeString()} [{text}]  {text3}";
    }

    public override int GetHashCode()
    {
        return ToString().GetHashCode();
    }
}
