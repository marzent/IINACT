using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using Advanced_Combat_Tracker;
using RainbowMage.OverlayPlugin.EventSources;
using RainbowMage.OverlayPlugin.NetworkProcessors;

namespace RainbowMage.OverlayPlugin.Integration
{
    public class UnstableNewLogLines
    {
        private bool inCutscene = false;
        private FFXIVRepository repo = null;
        private NetworkParser parser = null;
        private EnmityEventSource enmitySource = null;
        private ILogger logger = null;
        private string logPath = null;
        private ConcurrentQueue<string> logQueue = null;
        private Thread logThread = null;

        public UnstableNewLogLines(TinyIoCContainer container)
        {
            repo = container.Resolve<FFXIVRepository>();
            parser = container.Resolve<NetworkParser>();
            enmitySource = container.Resolve<EnmityEventSource>();
            logger = container.Resolve<ILogger>();
            logPath = Path.GetDirectoryName(ActGlobals.oFormActMain.LogFilePath) + "_OverlayPlugin.log";
            var config = container.Resolve<BuiltinEventConfig>();

            config.LogLinesChanged += (o, e) =>
            {
                if (config.LogLines)
                {
                    Enable();
                } else
                {
                    Disable();
                }
            };

            if (config.LogLines)
            {
                Enable();
            }
        }

        public void Enable()
        {
            parser.OnOnlineStatusChanged += OnOnlineStatusChange;
            enmitySource.CombatStatusChanged += OnCombatStatusChange;

            logThread = new Thread(new ThreadStart(WriteBackgroundLog));
            logThread.IsBackground = true;
            logThread.Start();
        }

        public void Disable()
        {
            parser.OnOnlineStatusChanged -= OnOnlineStatusChange;
            enmitySource.CombatStatusChanged -= OnCombatStatusChange;
            logQueue?.Enqueue(null);
        }

        private void WriteBackgroundLog()
        {
            try
            {
                logger.Log(LogLevel.Info, "LogWriter: Opening log file {0}.", logPath);
                var logFile = File.Open(logPath, FileMode.Append, FileAccess.Write, FileShare.Read);
                logQueue = new ConcurrentQueue<string>();

                while (true)
                {
                    if (logQueue.TryDequeue(out var line))
                    {
                        if (line == null) break;

                        var data = Encoding.UTF8.GetBytes(line + "\n");
                        logFile.Write(data, 0, data.Length);
                    } else
                    {
                        Thread.Sleep(500);
                    }
                }

                logger.Log(LogLevel.Info, "LogWriter: Closing log.");
                logFile.Close();
            } catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "LogWriter: {0}", ex);
                logQueue = null;
            }
        }

        public void WriteLogMessage(string msg)
        {
            var time = DateTime.Now;
            var lineParts = new string[] { "00", time.ToString(), "c0fe", "", "OPLine: " + msg, ""};
            var line = string.Join("|", lineParts);
            
            ActGlobals.oFormActMain.ParseRawLogLine(line);
            logQueue?.Enqueue(line);
        }

        private void OnOnlineStatusChange(object sender, OnlineStatusChangedArgs ev)
        {
            if (ev.Target != repo.GetPlayerID())
                return;

            var cutsceneStatus = ev.Status == 15;
            if (cutsceneStatus != inCutscene)
            {
                inCutscene = cutsceneStatus;
                string msg;

                if (cutsceneStatus)
                {
                    msg = "Entered cutscene";
                } else
                {
                    msg = "Left cutscene";
                }

                WriteLogMessage(msg);
            }
        }

        private void OnCombatStatusChange(object sender, CombatStatusChangedArgs ev)
        {
            string msg;
            if (ev.InCombat)
            {
                msg = "Entered combat";
            } else
            {
                msg = "Left combat";
            }

            WriteLogMessage(msg);
        }
    }
}
