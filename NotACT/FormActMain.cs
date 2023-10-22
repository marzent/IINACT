using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Advanced_Combat_Tracker.Resources;
using Dalamud.Logging;
using FFXIV_ACT_Plugin.Logfile;

namespace Advanced_Combat_Tracker;

public partial class FormActMain : Form, ISynchronizeInvoke
{
    public delegate DateTime DateTimeLogParser(string logLine);

    private readonly ConcurrentQueue<MasterSwing> afterActionsQueue = new();
    private Thread afterActionQueueThread;
    public DateTimeLogParser GetDateTimeFromLog;
    private volatile bool inCombat;
    private DateTime lastKnownTime;
    private DateTime lastSetEncounter;
    private HistoryRecord lastZoneRecord;
    private Thread logReaderThread;
    private Thread logWriterThread;

    internal volatile bool refreshTree;

    public FormActMain()
    {
        InitializeComponent();
        AppDataFolder = new DirectoryInfo(".");
        ActGlobals.ActLocalization.Init();
        ActGlobals.ActLocalization.AddPrebuild();
        NotActMainFormatter.SetupEnvironment();
        LastKnownTime = DateTime.Now;
        StartAfterCombatActionThread();
    }

    public bool ReadThreadLock { get; set; }
    public bool WriteLogFile { get; set; } = true;
    public int GlobalTimeSorter { get; set; }
    public List<ZoneData> ZoneList { get; set; } = new();
    public string LogFileFilter { get; set; } = "notact*.txt";

    public string LogFilePath { get; set; }

    public DirectoryInfo AppDataFolder { get; private set; }
    public ConcurrentQueue<string> LogQueue { get; private set; } = new();
    public string CurrentZone { get; set; }
    public FFXIV_ACT_Plugin.FFXIV_ACT_Plugin FfxivPlugin { get; set; }
    public object OverlayPluginContainer { get; set; }


    public DateTime LastHostileTime { get; private set; }
    public object AfterCombatActionDataLock => ActGlobals.ActionDataLock;

    public Regex ZoneChangeRegex { get; set; }

    public bool LogPathHasCharName { get; set; }

    public bool InCombat
    {
        get => inCombat;
        set => inCombat = value;
    }

    public int TimeStampLen { get; set; }

    public DateTime LastKnownTime
    {
        get => lastKnownTime;
        set
        {
            if (!(value == DateTime.MinValue)) lastKnownTime = value;
        }
    }

    public ZoneData ActiveZone { get; set; }

    // Don't run anything on the non existing WinForms UI thread
    public new object? Invoke(Delegate method, object?[]? args)
    {
        return method.DynamicInvoke(args);
    }

    public new IAsyncResult BeginInvoke(Delegate method, object?[]? args)
    {
        return Task.FromResult(Invoke(method, args));
    }

    public new object? EndInvoke(IAsyncResult result)
    {
        return ((Task<object?>)result).Result;
    }

    public new void Invoke(Action method)
    {
        _ = Invoke(method, null);
    }

    public new object? Invoke(Delegate method)
    {
        return Invoke(method, null);
    }

    public event LogLineEventDelegate BeforeLogLineRead;
    public event LogLineEventDelegate OnLogLineRead;

    public event LogFileChangedDelegate LogFileChanged
    {
        add { }
        remove { }
    }

    public event CombatActionDelegate AfterCombatAction;

    public delegate void TextToSpeechDelegate(string text);

    public event TextToSpeechDelegate TextToSpeech;


    public void WriteExceptionLog(Exception ex, string MoreInfo) => 
        PluginLog.Error(ex, MoreInfo);

    public void OpenLog(bool GetCurrentZone, bool GetCharNameFromFile) { }
    public static IntPtr delta0 = IntPtr.Zero;
    public static IntPtr delta4 = IntPtr.Zero;
    public static IntPtr deltaC = IntPtr.Zero;
    public void ParseRawLogLine(string logLine)
    {
        if (WriteLogFile)
            LogQueue.Enqueue(logLine);
        if (BeforeLogLineRead == null || GetDateTimeFromLog == null)
            return;
        var log= logLine.Split(new char[] { '|'});
        var logMesssageType = Int32.TryParse(log[0],out var a);
        if (a!=0&&a!=254)
        {
            var abcd = 1;
        }
        if (a ==27)
        {
            var 计算数1 = Marshal.ReadInt32(delta0);
            var 计算数2 = Marshal.ReadInt32(delta4);
            var 计算数3 = Marshal.ReadInt32(deltaC);
            var objectID = Convert.ToUInt32(log[2],16);
            if (objectID==0x102CECA1)
            {
                var abcdef = 123;
            }
            var abc = Math.Min(计算数3 + 计算数1 - 计算数2, 0);
            var id= Convert.ToUInt32(log[6], 16);
 
            var trueID = id + abc;
            logLine=logLine.Replace(log[6],$"{trueID:X4}");
        }
        var parsedLogTime = GetDateTimeFromLog(logLine);
        LastKnownTime = parsedLogTime;

        var logLineEventArgs = new LogLineEventArgs(logLine, 0, parsedLogTime, CurrentZone, inCombat, "Plugin");
        if (inCombat) { 
            this.ActiveZone.ActiveEncounter.LogLines.Add(new LogLineEntry(parsedLogTime, logLine, logLineEventArgs.detectedType, this.GlobalTimeSorter)); }
        BeforeLogLineRead(false, logLineEventArgs);
        if (OnLogLineRead == null)
            return;
        var logLineEventArgs2 = new LogLineEventArgs(logLineEventArgs.logLine, logLineEventArgs.detectedType,
                                                     parsedLogTime, CurrentZone, inCombat, "Plugin");
        OnLogLineRead(false, logLineEventArgs2);
    }


    public void TTS(string message) => TextToSpeech(message);

    public void ChangeZone(string ZoneName)
    {
        if (lastZoneRecord != null) lastZoneRecord.EndTime = LastKnownTime;

        CurrentZone = ZoneName;
        var lastLastRecord = lastZoneRecord;
        lastZoneRecord = new HistoryRecord(0, LastKnownTime, LastKnownTime.AddDays(1.0), CurrentZone,
                                           ActGlobals.charName);

        if (lastLastRecord == null)
        {
            //first run after parser init
            StartLogReaderThread();
            StartLogWriterThread();
        }

        if (ActiveZone != null) return;
        ActiveZone = new ZoneData(DateTime.Now, CurrentZone, true, false, false);
    }

    public void ActCommands(string commandText)
    {
        if (commandText != "end") return;
        if (inCombat) EndCombat(true);
    }

    public void EndCombat(bool export)
    {
        if (inCombat) inCombat = false;
        if (ActiveZone.ActiveEncounter.Active)
        {
            if (ActiveZone.PopulateAll)
                ActiveZone.Items[0].EndCombat(Finalize: false);

            ActiveZone.ActiveEncounter.EndCombat(Finalize: true);
        }
    }

    public bool SelectiveListGetSelected(string Player)
    {
        var key = Player.ToUpper();
        return ActGlobals.selectiveList.ContainsKey(key) && ActGlobals.selectiveList[key];
    }

    public bool SetEncounter(DateTime Time, string Attacker, string Victim)
    {
        // Check if not already in combat
        if (!inCombat)
        {
            // Check if a new zone or session has started
            if (lastZoneRecord.Label != ActiveZone.ZoneName || CurrentZone != ActiveZone.ZoneName ||
                lastZoneRecord.StartTime != ActiveZone.StartTime)
            {
                // Look for the last active zone
                var zoneFound = false;
                foreach (var zone in ZoneList)
                {
                    if (zone.StartTime != lastZoneRecord.StartTime || lastZoneRecord.Label != zone.ZoneName)
                        continue;

                    // Found the active zone
                    zoneFound = true;
                    ActiveZone = zone;
                    break;
                }

                // If the last active zone is not found, create a new zone
                if (!zoneFound)
                {
                    var start = lastZoneRecord.Label != CurrentZone ? Time : lastZoneRecord.StartTime;
                    ActiveZone = new ZoneData(start, CurrentZone, true, false, false);

                    // Insert the new zone into the list of zones
                    var index = ZoneList.Count;
                    for (var i = 1; i < ZoneList.Count; i++)
                    {
                        if (ZoneList[i].StartTime > Time)
                        {
                            index = i;
                            break;
                        }
                    }

                    ZoneList.Insert(index, ActiveZone);
                }
            }

            // Set the active encounter
            ActiveZone.ActiveEncounter = new EncounterData(ActGlobals.charName, CurrentZone, ActiveZone);
            ActiveZone.Items.Add(ActiveZone.ActiveEncounter);
            lastSetEncounter = LastKnownTime;
        }

        // Check if the encounter is selective
        if (ActiveZone.ActiveEncounter.GetIsSelective())
        {
            if (SelectiveListGetSelected(Attacker) || SelectiveListGetSelected(Victim))
            {
                // The encounter is selective and either the attacker or the victim is selected
                refreshTree = true;
                LastHostileTime = Time;
                inCombat = true;
                return true;
            }

            // The encounter is selective and neither the attacker nor the victim is selected
            return false;
        }

        // The encounter is not selective
        refreshTree = true;
        LastHostileTime = Time;
        inCombat = true;
        return true;
    }

    public void AddCombatAction(MasterSwing Action)
    {
        if (!ActGlobals.oFormActMain.InCombat)
        {
            throw new InvalidOperationException(
                "Do not add combat actions while ActGlobals.oFormActMain.InCombat is false");
        }

        if (string.IsNullOrWhiteSpace(Action.Special) || Action.Special == "hit" || Action.Special == "hits")
            Action.special = "specialAttackTerm-none";

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
        afterActionsQueue.Enqueue(Action);
    }

    private void StartLogWriterThread()
    {
        logWriterThread = new Thread(LogWriter)
        {
            IsBackground = true,
            Name = "LogWriterThread",
            Priority = ThreadPriority.BelowNormal
        };
        logWriterThread.Start();
    }

    private void LogWriter()
    {
        try
        {
            using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
            using var outputWriter = new StreamWriter(stream);
            while (true)
            {
                if (!WriteLogFile)
                {
                    Thread.Sleep(2000);
                    continue;
                }
                
                while (LogQueue.TryDequeue(out var line))
                    outputWriter.WriteLine(line);

                outputWriter.Flush();
                Thread.Sleep(500);
            }
        }
        catch (ObjectDisposedException) { }
        catch (ThreadAbortException) { }
        catch (Exception ex)
        {
            WriteExceptionLog(ex, "StartLogReaderThread failed, restarting thread");
            StartLogWriterThread();
        }
    }

    private void StartLogReaderThread()
    {
        logReaderThread = new Thread(LogReader)
        {
            IsBackground = true,
            Name = "LogReaderThread",
            Priority = ThreadPriority.Normal
        };
        logReaderThread.Start();
    }

    private void LogReader()
    {
        try
        {
            var logOutput = (LogOutput)FfxivPlugin._dataCollection._logOutput;
            while (true)
            {
                string? logLine = null;
                lock (logOutput._LogQueueLock)
                {
                    if (logOutput._LogQueue.Count > 0)
                        logLine = logOutput._LogQueue.Dequeue();
                }

                if (logLine != null)
                    ParseRawLogLine(logLine);
                else
                    Thread.Sleep(50);
            }
        }
        catch (ObjectDisposedException) { }
        catch (ThreadAbortException) { }
        catch (Exception ex)
        {
            WriteExceptionLog(ex, "StartLogReaderThread failed, restarting thread");
            StartLogReaderThread();
        }
    }

    private void StartAfterCombatActionThread()
    {
        afterActionQueueThread = new Thread(ThreadAfterCombatAction)
        {
            IsBackground = true,
            Name = "AfterActionQueueThread",
            Priority = ThreadPriority.Normal
        };
        afterActionQueueThread.Start();
    }

    private void ThreadAfterCombatAction()
    {
        try
        {
            while (true)
            {
                while (afterActionsQueue.TryDequeue(out var masterSwing))
                {
                    ActiveZone.AddCombatAction(masterSwing);
                    if (AfterCombatAction == null) continue;

                    var actionInfo = new CombatActionEventArgs(masterSwing);
                    try
                    {
                        AfterCombatAction(false, actionInfo);
                    }
                    catch (Exception ex2)
                    {
                        WriteExceptionLog(ex2, "AddCombatAction->AfterCombatAction event\n");
                    }
                }

                Thread.Sleep(2);
            }
        }
        catch (ObjectDisposedException) { }
        catch (ThreadAbortException) { }
        catch (Exception ex5)
        {
            WriteExceptionLog(ex5, "AfterCombatActionDequeue failed, restarting thread");
            StartAfterCombatActionThread();
        }
    }

    public void ValidateLists() { }

    public void ValidateTableSetup() { }

    public string CreateDamageString(long Damage, bool UseSuffix, bool UseDecimals)
    {
        const long trillion = 1000000000000L;
        const long billion = 1000000000;
        const long million = 1000000;
        const long thousand = 1000;
    
        switch (Damage)
        {
            case long.MinValue:
                return float.NaN.ToString(CultureInfo.InvariantCulture);
            case long.MaxValue:
                return float.PositiveInfinity.ToString(CultureInfo.InvariantCulture);
            default:
                if (UseSuffix)
                {
                    if (UseDecimals)
                    {
                        switch (Damage)
                        {
                            case >= trillion:
                                return $"{Damage / 1E+15:0.00}Q";
                            case >= billion:
                                return $"{Damage / billion:0.00}B";
                            case >= million:
                                return $"{Damage / million:0.00}M";
                            case >= thousand:
                                return $"{Damage / thousand:0.00}K";
                        }
                    }
                    else
                    {
                        switch (Damage)
                        {
                            case >= trillion:
                                return $"{Damage / trillion}T";
                            case >= billion:
                                return $"{Damage / billion}B";
                            case >= million:
                                return $"{Damage / million}M";
                            case >= thousand:
                                return $"{Damage / thousand}K";
                        }
                    }
                }
                return $"{Damage}";
        }
    }

    public void PlaySound(string file)
    {
        try
        {
            var snd = new SoundPlayer(file);
            snd.Play();
        }
        catch (Exception ex)
        {
            WriteExceptionLog(ex, $"sound file: {file}");
        }
    }
}
