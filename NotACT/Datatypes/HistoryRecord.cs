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
        var duration = EndTime - StartTime;
        var durationString = duration.ToString(duration.TotalHours >= 1 ? @"hh\:mm\:ss" : @"mm\:ss");
        var folderHint = ActGlobals.oFormActMain.LogFilePath.Equals(FolderHint, StringComparison.OrdinalIgnoreCase)
                             ? string.Empty
                             : FolderHint.ToLower();
        var labelPrefix = Type == 1 ? "     " : string.Empty;
        return $"{labelPrefix}{Label} - {StartTime:MM/dd/yyyy h:mm:ss tt} [{durationString}] {folderHint}";
    }


    public override int GetHashCode() => ToString().GetHashCode();
}
