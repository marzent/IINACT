using System;
using System.ComponentModel;

namespace RainbowMage.OverlayPlugin {
    /// <summary>
    /// ログを記録する機能を提供するクラス。
    /// </summary>
    public class Logger : ILogger {
        /// <summary>
        /// 記録されたログを取得します。
        /// </summary>
        public BindingList<LogEntry> Logs { get; private set; }
        private Action<LogEntry> listener = null;

        public Logger() {
            this.Logs = new BindingList<LogEntry>();
        }

        /// <summary>
        /// メッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">メッセージ。</param>
        public void Log(LogLevel level, string message) {
#if !DEBUG
            if (level == LogLevel.Trace || level == LogLevel.Debug)
            {
                return;
            }
#endif
#if DEBUG
            System.Diagnostics.Trace.WriteLine(string.Format("{0}: {1}: {2}", level, DateTime.Now, message));
#endif

            var entry = new LogEntry(level, DateTime.Now, message);

            lock (Logs) {

                if (listener != null) {
                    listener(entry);
                } else {
                    Logs.Add(entry);
                }
            }
        }

        /// <summary>
        /// 書式指定子を用いたメッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="format">複合書式指定文字列。</param>
        /// <param name="args">書式指定するオブジェクト。</param>
        public void Log(LogLevel level, string format, params object[] args) {
            Log(level, string.Format(format, args));
        }

        public void RegisterListener(Action<LogEntry> listener) {
            lock (Logs) {
                foreach (var entry in Logs) {
                    listener(entry);
                }

                this.listener = listener;
                this.Logs.Clear();
            }
        }

        public void ClearListener() {
            lock (Logs) {
                this.listener = null;
            }
        }
    }
}
