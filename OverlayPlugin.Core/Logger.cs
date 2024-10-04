using System;
using Dalamud.Plugin.Services;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// ログを記録する機能を提供するクラス。
    /// </summary>
    public class Logger : ILogger
    {
        public Logger(IPluginLog pluginLog)
        {
            PluginLog = pluginLog;
        }
        
        private IPluginLog PluginLog { get; }

        /// <summary>
        /// メッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="message">メッセージ。</param>
        public void Log(LogLevel level, string message)
        {
            switch (level)
            {
                case LogLevel.Trace:
                    PluginLog.Verbose($"[OverlayPlugin] {message}");
                    break;
                case LogLevel.Debug:
                    PluginLog.Debug($"[OverlayPlugin] {message}");
                    break;
                case LogLevel.Info:
                    PluginLog.Information($"[OverlayPlugin] {message}");
                    break;
                case LogLevel.Warning:
                    PluginLog.Warning($"[OverlayPlugin] {message}");
                    break;
                case LogLevel.Error:
                    PluginLog.Error($"[OverlayPlugin] {message}");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        /// <summary>
        /// 書式指定子を用いたメッセージを指定してログを記録します。
        /// </summary>
        /// <param name="level">ログレベル。</param>
        /// <param name="format">複合書式指定文字列。</param>
        /// <param name="args">書式指定するオブジェクト。</param>
        public void Log(LogLevel level, string format, params object[] args) => Log(level, string.Format(format, args));

        public void RegisterListener(Action<LogEntry> listener) { }

        public void ClearListener() { }
    }
}
