using System.Collections.ObjectModel;
using System.Diagnostics;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Common.Models;
using FFXIV_ACT_Plugin.Config;
using FFXIV_ACT_Plugin.Logfile;
using FFXIV_ACT_Plugin.Memory;
using HarmonyLib;
using IINACT;
using Machina;
using ACTWrapper = FFXIV_ACT_Plugin.ACTWrapper;

namespace FFXIV_ACT_PluginPatcher {

    public class PluginPatcher {
        public static void DoPatching() {
            var harmony = new Harmony("ffxiv.act.patch");
            harmony.PatchAll();
        }
    }

    //TODO: work around the lock here and use this instead of modifying LogOutput.Run

    //[HarmonyPatch(typeof(LogOutput))]
    //[HarmonyPatch(nameof(LogOutput.WriteLine))]
    //class PatchLogWriteLine {
    //static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) {
    //    var codes = new List<CodeInstruction>(instructions);
    //    var enqueueInstructionIndex = codes.FindIndex(code => code.opcode == OpCodes.Callvirt &&
    //        code.operand.ToString()!.Equals("Void Enqueue(System.String)"));
    //    if (enqueueInstructionIndex == -1)
    //        throw new Exception("Could not patch LogOutput.WriteLine");

    //    codes[enqueueInstructionIndex] = CodeInstruction.Call(typeof(FFXIV_ACT_PluginWrapper), "logLineWritten", new[] { typeof(string) });
    //    var enqueueInstructionArgIndex = enqueueInstructionIndex - 3;
    //    codes.RemoveRange(enqueueInstructionArgIndex, 2);

    //    return codes.AsEnumerable();
    //}
    //}

    [HarmonyPatch(typeof(LogOutput))]
    [HarmonyPatch(nameof(LogOutput.Run))]
    internal class PatchLogRun {
        private static bool Prefix(LogOutput __instance, object parameter) {
            var settingsManager = __instance.GetField<ISettingsMediator>("_settingsManager");
            var logQueueLock = __instance.GetField<object>("_LogQueueLock");
            var logQueue = __instance.GetField<Queue<string>>("_LogQueue");
            var cancellationToken = (CancellationToken)parameter;
            var flag = false;
            var logFileFolder = settingsManager.DataCollectionSettings.LogFileFolder;
            while (!cancellationToken.WaitHandle.WaitOne(50)) {
                try {
                    while (settingsManager.DataCollectionSettings.LogFileFolder == logFileFolder)
                    {
                        string? logLine;
                        while (!cancellationToken.IsCancellationRequested) {
                            logLine = null;
                            lock (logQueueLock) {
                                if (logQueue.Count > 0)
                                    logLine = logQueue.Dequeue();
                            }
                            if (logLine != null) {
                                FfxivActPluginWrapper.LogLineWritten(logLine);
                            } else if (cancellationToken.WaitHandle.WaitOne(50)) {
                                return false;
                            }
                        }
                    }
                }
                catch (ThreadAbortException) {
                    break;
                }
                catch (IOException ex) {
                    settingsManager.AddExceptionMessage(DateTime.Now, "FFXIV_ACT_Plugin.LogOutput Error writing to log file. " + ex);
                }
                catch (Exception ex) {
                    if (!flag) {
                        Trace.WriteLine(ex.ToString());
                    }
                    flag = true;
                    if (cancellationToken.WaitHandle.WaitOne(100)) {
                        break;
                    }
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(ACTWrapper))]
    [HarmonyPatch("RunOnACTUIThread")]
    internal class PatchACTUI {
        private static bool Prefix() {
            return false; //not a good idea
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnNetworkReceived))]
    internal class PatchNetworkReceived {
        private static bool Prefix(DataSubscription __instance, string connection, long epoch, byte[] message) {
            var instanceEvent = __instance.GetField<NetworkReceivedDelegate>("NetworkReceived");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((NetworkReceivedDelegate)invocation).Invoke(connection, epoch, message));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnNetworkSent))]
    internal class PatchNetworkSent {
        private static bool Prefix(DataSubscription __instance, string connection, long epoch, byte[] message) {
            var instanceEvent = __instance.GetField<NetworkSentDelegate>("NetworkSent");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((NetworkSentDelegate)invocation).Invoke(connection, epoch, message));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnCombatantAdded))]
    internal class PatchCombatantAdded {
        private static bool Prefix(DataSubscription __instance, object Combatant) {
            var instanceEvent = __instance.GetField<CombatantAddedDelegate>("CombatantAdded");
            if (instanceEvent == null) return false;
            var combatant = ((Combatant)Combatant).Clone();
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((CombatantAddedDelegate)invocation).Invoke(combatant));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnCombatantRemoved))]
    internal class PatchCombatantRemoved {
        private static bool Prefix(DataSubscription __instance, object Combatant) {
            var instanceEvent = __instance.GetField<CombatantRemovedDelegate>("CombatantRemoved");
            if (instanceEvent == null) return false;
            var combatant = ((Combatant)Combatant).Clone();
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((CombatantRemovedDelegate)invocation).Invoke(combatant));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnPrimaryPlayerChanged))]
    internal class PatchPrimaryPlayerChanged {
        private static bool Prefix(DataSubscription __instance) {
            var instanceEvent = __instance.GetField<PrimaryPlayerDelegate>("PrimaryPlayerChanged");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((PrimaryPlayerDelegate)invocation).Invoke());
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnZoneChanged))]
    internal class PatchZoneChanged {
        private static bool Prefix(DataSubscription __instance, uint ZoneID, string ZoneName) {
            var instanceEvent = __instance.GetField<ZoneChangedDelegate>("ZoneChanged");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((ZoneChangedDelegate)invocation).Invoke(ZoneID, ZoneName));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnPlayerStatsChanged))]
    internal class PatchPlayerStatsChanged {
        private static bool Prefix(DataSubscription __instance, object playerStats) {
            var instanceEvent = __instance.GetField<PlayerStatsChangedDelegate>("PlayerStatsChanged");
            if (instanceEvent == null) return false;
            var playerStats2 = ((Player)playerStats).Clone();
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((PlayerStatsChangedDelegate)invocation).Invoke(playerStats2));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnPartyListChanged))]
    internal class PatchPartyListChanged {
        private static bool Prefix(DataSubscription __instance, ReadOnlyCollection<uint> partyList, int partySize) {
            var instanceEvent = __instance.GetField<PartyListChangedDelegate>("PartyListChanged");
            if (instanceEvent == null) return false;
            var partyList2 = partyList.ToList().AsReadOnly();
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((PartyListChangedDelegate)invocation).Invoke(partyList2, partySize));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnLogLine))]
    internal class PatchLogLine {
        private static bool Prefix(DataSubscription __instance, uint EventType, uint Seconds, string logline) {
            var instanceEvent = __instance.GetField<LogLineDelegate>("LogLine");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((LogLineDelegate)invocation).Invoke(EventType, Seconds, logline));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnParsedLogLine))]
    internal class PatchParsedLogLine {
        private static bool Prefix(DataSubscription __instance, uint sequence, int eventType, string message) {
            var instanceEvent = __instance.GetField<ParsedLogLineDelegate>("ParsedLogLine");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((ParsedLogLineDelegate)invocation).Invoke(sequence, eventType, message));
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(DataSubscription))]
    [HarmonyPatch(nameof(DataSubscription.OnProcessChanged))]
    internal class PatchProcessChanged {
        private static bool Prefix(DataSubscription __instance, Process process) {
            var instanceEvent = __instance.GetField<ProcessChangedDelegate>("ProcessChanged");
            if (instanceEvent == null) return false;
            var invocationList = instanceEvent.GetInvocationList();
            foreach (var invocation in invocationList) {
                Task.Run(() => ((ProcessChangedDelegate)invocation).Invoke(process));
            }
            return false;
        }
    }

    internal static class MachinaConnection {
        public static byte Age;
    }

    [HarmonyPatch(typeof(ConnectionManager))]
    [HarmonyPatch(nameof(ConnectionManager.Refresh))]
    internal class PatchSkipRefresh {
        private static bool Prefix() {
            return unchecked(MachinaConnection.Age++) == 0;
        }
    }
}