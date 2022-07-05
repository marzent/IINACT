using System;

namespace RainbowMage.OverlayPlugin {
    public interface ILogger {
        void Log(LogLevel level, string message);
        void Log(LogLevel level, string format, params object[] args);
        void RegisterListener(Action<LogEntry> listener);
        void ClearListener();
    }

    public class LogEntry {
        public string Message { get; set; }
        public LogLevel Level { get; set; }
        public DateTime Time { get; set; }

        public LogEntry(LogLevel level, DateTime time, string message) {
            this.Message = message;
            this.Level = level;
            this.Time = time;
        }
    }

    public class LogEventArgs : EventArgs {
        public string Message { get; private set; }
        public LogLevel Level { get; private set; }
        public LogEventArgs(LogLevel level, string message) {
            this.Message = message;
            this.Level = level;
        }
    }

    public enum LogLevel {
        Trace,
        Debug,
        Info,
        Warning,
        Error
    }
}
