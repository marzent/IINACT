namespace Advanced_Combat_Tracker
{
    public class LogLineEntry
    {
        public int GlobalTimeSorter { get; }

        public DateTime Time { get; set; }

        public string LogLine { get; }

        public int Type { get; }

        public bool SearchSelected { get; set; }

        public LogLineEntry(DateTime Time, string LogLine, int ParsedType, int GlobalTimeSorter)
        {
            this.LogLine = LogLine;
            Type = ParsedType;
            SearchSelected = false;
            this.Time = Time;
            this.GlobalTimeSorter = GlobalTimeSorter;
        }
    }
}
