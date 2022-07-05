using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Advanced_Combat_Tracker {

    public partial class FormActMain : Form {
        private volatile bool inCombat;
        private HistoryRecord lastZoneRecord;
        private DateTime lastSetEncounter;
        private DateTime _lastKnownTime;
        private readonly ConcurrentQueue<MasterSwing> _afterActionsQueue = new();
        private Thread _afterActionQueueThread;
        private Thread _logReaderThread;
        private Thread _logWriterThread;
        private readonly object _ttsLock = new();

        internal volatile bool refreshTree;

        public bool ReadThreadLock { get; set; }
        public int GlobalTimeSorter { get; set; }
        public List<ZoneData> ZoneList { get; set; } = new List<ZoneData>();
        public string LogFileFilter { get; set; } = "notact*.txt";
        public string LogFilePath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "IINACT");
        public DirectoryInfo AppDataFolder { get; private set; }
        public ConcurrentQueue<string> LogQueue { get; private set; } = new ConcurrentQueue<string>();
        public ConcurrentQueue<string> LogFileQueue { get; private set; } = new ConcurrentQueue<string>();
        public string CurrentZone { get; set; }
        public FFXIV_ACT_Plugin.FFXIV_ACT_Plugin FfxivPlugin { get; set; }
        public object OverlayPluginContainer { get; set; }
        public DateTimeLogParser GetDateTimeFromLog;


        public DateTime LastHostileTime { get; private set; }
        public object AfterCombatActionDataLock => ActGlobals.ActionDataLock;

        public event LogLineEventDelegate BeforeLogLineRead;
        public event LogLineEventDelegate OnLogLineRead;
        public event LogFileChangedDelegate LogFileChanged;
        public event CombatActionDelegate AfterCombatAction;
        public delegate DateTime DateTimeLogParser(string logLine);

        public FormActMain() {
            InitializeComponent();
            AppDataFolder = new DirectoryInfo(Path.Combine(LogFilePath, "Data"));
            ActGlobals.ActLocalization.Init();
            ActGlobals.ActLocalization.AddPrebuild();
            Resources.NotActMainFormatter.SetupEnvironment();
            LastKnownTime = DateTime.Now;
            StartAfterCombatActionThread();
        }

        public void WriteExceptionLog(Exception ex, string MoreInfo) {
            var value = $"***** {DateTime.Now.ToString("s")} - {MoreInfo}\n{ex}\n{Environment.StackTrace}\n*****";
            Trace.WriteLine(value);
        }

        public void OpenLog(bool GetCurrentZone, bool GetCharNameFromFile) {
        }

        public void ParseRawLogLine(string LogLine) {
            if (BeforeLogLineRead == null || GetDateTimeFromLog == null)
                return;
            var parsedLogTime = GetDateTimeFromLog(LogLine);
            LastKnownTime = parsedLogTime;
            var logLineEventArgs = new LogLineEventArgs(LogLine, 0, parsedLogTime, CurrentZone, inCombat, "Plugin");
            BeforeLogLineRead(false, logLineEventArgs);
            if (OnLogLineRead == null)
                return;
            var logLineEventArgs2 = new LogLineEventArgs(logLineEventArgs.logLine, logLineEventArgs.detectedType, parsedLogTime, CurrentZone, inCombat, "Plugin");
            OnLogLineRead(false, logLineEventArgs2);
        }


        public void TTS(string message, string binary = "/usr/bin/say", string args = "") {
            lock (_ttsLock) {
                if (new FileInfo(binary).Exists) {
                    try {
                        var ttsProcess = new Process {
                            StartInfo = {
                                FileName = binary,
                                CreateNoWindow = true,
                                UseShellExecute = false,
                                Arguments = args + " \"" + Regex.Replace(Regex.Replace(message, @"(\\*)"+"\"", @"$1$1\"+"\""), @"(\\+)$", @"$1$1") + "\""
                            }
                        };
                        ttsProcess.Start();
                        Thread.Sleep(500 * message.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
                    }
                    catch (Exception ex) {
                        WriteExceptionLog(ex, $"TTS failed to play back {message}");
                    }
                } else {
                    Trace.WriteLine($"TTS binary {binary} not found");
                }
            }
        }

        public Regex ZoneChangeRegex { get; set; }

        public bool LogPathHasCharName { get; set; }

        public bool InCombat {
            get => inCombat;
            set => inCombat = value;
        }

        public int TimeStampLen { get; set; }

        public DateTime LastKnownTime {
            get => _lastKnownTime;
            set {
                if (!(value == DateTime.MinValue)) {
                    _lastKnownTime = value;
                }
            }
        }

        public void ChangeZone(string ZoneName) {
            if (lastZoneRecord != null) {
                lastZoneRecord.EndTime = LastKnownTime;
            }
            CurrentZone = ZoneName;
            var lastLastRecord = lastZoneRecord;
            lastZoneRecord = new HistoryRecord(0, LastKnownTime, LastKnownTime.AddDays(1.0), CurrentZone, ActGlobals.charName);

            if (lastLastRecord == null) { //first run after parser init
                StartLogReaderThread();
                StartLogWriterThread();
            }

            if (ActiveZone != null) return;
            ActiveZone = new ZoneData(DateTime.Now, CurrentZone, true, false, false);
            ZoneList.Add(ActiveZone);

        }

        public void ActCommands(string commandText) {
            if (commandText != "end") return;
            if (inCombat) {
                EndCombat(export: true);
            }
        }

        public void EndCombat(bool export) {
            if (inCombat) inCombat = false;
        }

        public bool SelectiveListGetSelected(string Player) {
            var key = Player.ToUpper();
            return ActGlobals.selectiveList.ContainsKey(key) && ActGlobals.selectiveList[key];
        }

        public ZoneData ActiveZone { get; set; }

        public bool SetEncounter(DateTime Time, string Attacker, string Victim) {
            if (!inCombat) {
                if (lastZoneRecord.Label != ActiveZone.ZoneName || CurrentZone != ActiveZone.ZoneName || lastZoneRecord.StartTime != ActiveZone.StartTime) {
                    var flag2 = false;
                    foreach (var t in ZoneList) {
                        if (t.StartTime != lastZoneRecord.StartTime || lastZoneRecord.Label != t.ZoneName) continue;
                        flag2 = true;
                        ActiveZone = t;
                        break;
                    }
                    if (!flag2) {
                        var start = ((!(lastZoneRecord.Label == CurrentZone)) ? Time : lastZoneRecord.StartTime);
                        ActiveZone = new ZoneData(start, CurrentZone, true, false, false);
                        var index = ZoneList.Count;
                        for (var j = 1; j < ZoneList.Count; j++) {
                            if (ZoneList[j].StartTime > Time) {
                                index = j;
                                break;
                            }
                        }
                        ZoneList.Insert(index, ActiveZone);
                    }
                }
                ActiveZone.ActiveEncounter = new EncounterData(ActGlobals.charName, CurrentZone, ActiveZone);
                ActiveZone.Items.Add(ActiveZone.ActiveEncounter);
                lastSetEncounter = LastKnownTime;
            }
            if (ActiveZone.ActiveEncounter.GetIsSelective()) {
                if (SelectiveListGetSelected(Attacker) || SelectiveListGetSelected(Victim)) {
                    refreshTree = true;
                    LastHostileTime = Time;
                    inCombat = true;
                    return true;
                }
                return false;
            }
            refreshTree = true;
            LastHostileTime = Time;
            inCombat = true;
            return true;
        }

        public void AddCombatAction(MasterSwing Action) {
            if (!ActGlobals.oFormActMain.InCombat) {
                throw new InvalidOperationException("Do not add combat actions while ActGlobals.oFormActMain.InCombat is false");
            }
            if (string.IsNullOrWhiteSpace(Action.Special) || Action.Special == "hit" || Action.Special == "hits") {
                Action.special = "specialAttackTerm-none";
            }
            Action.attacker = Action.Attacker.Trim();
            Action.victim = Action.Victim.Trim();
            Action.attackType = Action.AttackType.Trim();
            var combatActionEventArgs = new CombatActionEventArgs(Action);

            Action.swingType = combatActionEventArgs.swingType;
            Action.critical = combatActionEventArgs.critical;
            Action.special = string.Intern(combatActionEventArgs.special);
            Action.damage = combatActionEventArgs.damage;
            Action.time = combatActionEventArgs.time;
            Action.timeSorter = combatActionEventArgs.timeSorter;
            Action.attackType = string.Intern(combatActionEventArgs.theAttackType);
            Action.attacker = string.Intern(combatActionEventArgs.attacker);
            Action.damageType = string.Intern(combatActionEventArgs.theDamageType);
            Action.victim = string.Intern(combatActionEventArgs.victim);
            _afterActionsQueue.Enqueue(Action);
        }

        private void StartLogWriterThread() {
            _logWriterThread = new Thread(LogWriter) {
                IsBackground = true,
                Name = "LogWriterThread",
                Priority = ThreadPriority.BelowNormal
            };
            _logWriterThread.Start();
        }

        private void LogWriter() {
            try {
                using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
                using var outputWriter = new StreamWriter(stream);
                while (true) {
                    while (LogFileQueue.TryDequeue(out var line))
                        outputWriter.WriteLine(line);

                    outputWriter.Flush();
                    Thread.Sleep(500);
                }
            }
            catch (ObjectDisposedException) {
            }
            catch (ThreadAbortException) {
            }
            catch (Exception ex5) {
                WriteExceptionLog(ex5, "StartLogReaderThread failed, restarting thread");
                StartLogWriterThread();
            }
        }

        private void StartLogReaderThread() {
            _logReaderThread = new Thread(LogReader) {
                IsBackground = true,
                Name = "LogReaderThread",
                Priority = ThreadPriority.Normal
            };
            _logReaderThread.Start();
        }

        private void LogReader() {
            try {
                while (true) {
                    while (LogQueue.TryDequeue(out var line))
                        ParseRawLogLine(line);

                    Thread.Sleep(1);
                }
            }
            catch (ObjectDisposedException) {
            }
            catch (ThreadAbortException) {
            }
            catch (Exception ex5) {
                WriteExceptionLog(ex5, "StartLogReaderThread failed, restarting thread");
                StartLogReaderThread();
            }
        }

        private void StartAfterCombatActionThread() {
            _afterActionQueueThread = new Thread(ThreadAfterCombatAction) {
                IsBackground = true,
                Name = "AfterActionQueueThread",
                Priority = ThreadPriority.Normal
            };
            _afterActionQueueThread.Start();
        }

        private void ThreadAfterCombatAction() {
            try {
                while (true) {
                    while (_afterActionsQueue.TryDequeue(out var masterSwing)) {
                        ActiveZone.AddCombatAction(masterSwing);
                        if (this.AfterCombatAction == null) {
                            continue;
                        }
                        var actionInfo = new CombatActionEventArgs(masterSwing);
                        try {
                            this.AfterCombatAction(false, actionInfo);
                        }
                        catch (Exception ex2) {
                            WriteExceptionLog(ex2, "AddCombatAction->AfterCombatAction event\n");
                        }
                    }

                    Thread.Sleep(2);
                }
            }
            catch (ObjectDisposedException) {
            }
            catch (ThreadAbortException) {
            }
            catch (Exception ex5) {
                WriteExceptionLog(ex5, "AfterCombatActionDequeue failed, restarting thread");
                StartAfterCombatActionThread();
            }
        }

        public void ValidateLists() {
        }

        public void ValidateTableSetup() {
        }

        public string CreateDamageString(long Damage, bool UseSuffix, bool UseDecimals) {
            switch (Damage) {
                case long.MinValue:
                    return float.NaN.ToString(CultureInfo.InvariantCulture);
                case long.MaxValue:
                    return float.PositiveInfinity.ToString(CultureInfo.InvariantCulture);
                default:
                    if (UseSuffix) {
                        if (UseDecimals) {
                            if (Damage >= 1000000000000000L) {
                                return $"{(double)Damage / 1E+15:0.00}Q";
                            }
                            if (Damage >= 1000000000000L) {
                                return $"{(double)Damage / 1000000000000.0:0.00}T";
                            }
                            if (Damage >= 1000000000) {
                                return $"{(double)Damage / 1000000000.0:0.00}B";
                            }
                            if (Damage >= 1000000) {
                                return $"{(double)Damage / 1000000.0:0.00}M";
                            }
                            if (Damage >= 1000) {
                                return $"{(double)Damage / 1000.0:0.00}K";
                            }
                        } else {
                            if (Damage >= 10000000000000000L) {
                                return $"{Damage / 1000000000000000L}Q";
                            }
                            if (Damage >= 10000000000000L) {
                                return $"{Damage / 1000000000000L}T";
                            }
                            if (Damage >= 10000000000L) {
                                return $"{Damage / 1000000000}B";
                            }
                            if (Damage >= 10000000) {
                                return $"{Damage / 1000000}M";
                            }
                            if (Damage >= 10000) {
                                return $"{Damage / 1000}K";
                            }
                        }
                    }
                    return $"{Damage}";
            }
        }
    }
}