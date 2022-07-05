namespace Advanced_Combat_Tracker {
	public delegate void LogLineEventDelegate(bool isImport, LogLineEventArgs logInfo);
	public delegate void LogFileChangedDelegate(bool IsImport, string NewLogFileName);

	public class LogLineEventArgs : EventArgs {
		public string logLine;

		public int detectedType;

		public readonly DateTime detectedTime;

		public readonly string detectedZone;

		public readonly bool inCombat;

		public readonly string originalLogLine;

		public readonly string companionLogName;

		public LogLineEventArgs(string LogLine, int DetectedType, DateTime DetectedTime, string DetectedZone, bool InCombat) {
			originalLogLine = LogLine;
			logLine = LogLine;
			detectedType = DetectedType;
			detectedTime = DetectedTime;
			detectedZone = DetectedZone;
			inCombat = InCombat;
			companionLogName = string.Empty;
		}

		public LogLineEventArgs(string LogLine, int DetectedType, DateTime DetectedTime, string DetectedZone, bool InCombat, string CompanionLogName) {
			originalLogLine = LogLine;
			logLine = LogLine;
			detectedType = DetectedType;
			detectedTime = DetectedTime;
			detectedZone = DetectedZone;
			inCombat = InCombat;
			companionLogName = CompanionLogName;
		}
	}
}
