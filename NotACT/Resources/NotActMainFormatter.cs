using System.Globalization;
using static Advanced_Combat_Tracker.ActGlobals;

namespace Advanced_Combat_Tracker.Resources;

public static class NotActMainFormatter
{
    public static void SetupEnvironment()
    {
        var usCulture = new CultureInfo("en-US");
        EncounterData.ColumnDefs.Clear();
        EncounterData.ColumnDefs.Add(
            "EncId",
            new EncounterData.ColumnDef("EncId", DefaultVisible: false, "CHAR(8)", "EncId",
                                        (EncounterData Data) => string.Empty, (EncounterData Data) => Data.EncId));
        EncounterData.ColumnDefs.Add(
            "Title",
            new EncounterData.ColumnDef("Title", DefaultVisible: true, "VARCHAR(64)", "Title",
                                        (EncounterData Data) => Data.Title, (EncounterData Data) => Data.Title));
        EncounterData.ColumnDefs.Add("StartTime",
                                     new EncounterData.ColumnDef("StartTime", DefaultVisible: true, "TIMESTAMP",
                                                                 "StartTime",
                                                                 (EncounterData Data) =>
                                                                     (!(Data.StartTime == DateTime.MaxValue))
                                                                         ? $"{Data.StartTime.ToShortDateString()} {Data.StartTime.ToLongTimeString()}"
                                                                         : "--:--:--",
                                                                 (EncounterData Data) =>
                                                                     (!(Data.StartTime == DateTime.MaxValue))
                                                                         ? Data.StartTime.ToString("u").TrimEnd('Z')
                                                                         : "0000-00-00 00:00:00"));
        EncounterData.ColumnDefs.Add(
            "EndTime",
            new EncounterData.ColumnDef("EndTime", DefaultVisible: true, "TIMESTAMP", "EndTime",
                                        (EncounterData Data) =>
                                            (!(Data.EndTime == DateTime.MinValue))
                                                ? Data.EndTime.ToString("T")
                                                : "--:--:--",
                                        (EncounterData Data) =>
                                            (!(Data.EndTime == DateTime.MinValue))
                                                ? Data.EndTime.ToString("u").TrimEnd('Z')
                                                : "0000-00-00 00:00:00"));
        EncounterData.ColumnDefs.Add(
            "Duration",
            new EncounterData.ColumnDef("Duration", DefaultVisible: true, "INT", "Duration",
                                        (EncounterData Data) => Data.DurationS,
                                        (EncounterData Data) => Data.Duration.TotalSeconds.ToString("0")));
        EncounterData.ColumnDefs.Add(
            "Damage",
            new EncounterData.ColumnDef("Damage", DefaultVisible: true, "BIGINT", "Damage",
                                        (EncounterData Data) => Data.Damage.ToString((string?)GetIntCommas()),
                                        (EncounterData Data) => Data.Damage.ToString()));
        EncounterData.ColumnDefs.Add(
            "EncDPS",
            new EncounterData.ColumnDef("EncDPS", DefaultVisible: true, "DOUBLE", "EncDPS",
                                        (EncounterData Data) => Data.DPS.ToString((string?)GetFloatCommas()),
                                        (EncounterData Data) => Data.DPS.ToString(usCulture)));
        EncounterData.ColumnDefs.Add(
            "Zone",
            new EncounterData.ColumnDef("Zone", DefaultVisible: false, "VARCHAR(64)", "Zone",
                                        (EncounterData Data) => Data.ZoneName,
                                        (EncounterData Data) => Data.ZoneName));
        EncounterData.ColumnDefs.Add(
            "Kills",
            new EncounterData.ColumnDef("Kills", DefaultVisible: true, "INT", "Kills",
                                        (EncounterData Data) => Data.AlliedKills.ToString((string?)GetIntCommas()),
                                        (EncounterData Data) => Data.AlliedKills.ToString()));
        EncounterData.ColumnDefs.Add(
            "Deaths",
            new EncounterData.ColumnDef("Deaths", DefaultVisible: true, "INT", "Deaths",
                                        (EncounterData Data) => Data.AlliedDeaths.ToString(),
                                        (EncounterData Data) => Data.AlliedDeaths.ToString()));
        EncounterData.ExportVariables.Clear();
        EncounterData.ExportVariables.Add(
            "n",
            new EncounterData.TextExportFormatter("n", "New Line",
                                                  "Formatting after this element will appear on a new line.",
                                                  (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) => "\n"));
        EncounterData.ExportVariables.Add(
            "t",
            new EncounterData.TextExportFormatter("t", "Tab Character",
                                                  "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)",
                                                  (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) => "\t"));
        EncounterData.ExportVariables.Add(
            "title",
            new EncounterData.TextExportFormatter("title", "Encounter Title",
                                                  "The title of the completed encounter.  This may only be used in Allies formatting.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "title",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "duration",
            new EncounterData.TextExportFormatter("duration", "Duration",
                                                  "The duration of the combatant or the duration of the encounter, displayed as mm:ss",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "duration",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DURATION",
            new EncounterData.TextExportFormatter("DURATION", "Short Duration",
                                                  "The duration of the combatant or encounter displayed in whole seconds.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DURATION",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "damage",
            new EncounterData.TextExportFormatter("damage", "Damage",
                                                  "The amount of damage from auto-attack, spells, CAs, etc done to other combatants.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "damage",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "damage-m",
            new EncounterData.TextExportFormatter("damage-m", "Damage M",
                                                  "Damage divided by 1,000,000 (with two decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "damage-m",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "damage-*",
            new EncounterData.TextExportFormatter("damage-*", "Damage w/suffix",
                                                  "Damage divided 1/K/M/B/T/Q (with two decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "damage-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DAMAGE-k",
            new EncounterData.TextExportFormatter("DAMAGE-k", "Short Damage K",
                                                  "Damage divided by 1,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-k",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DAMAGE-m",
            new EncounterData.TextExportFormatter("DAMAGE-m", "Short Damage M",
                                                  "Damage divided by 1,000,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-m",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DAMAGE-b",
            new EncounterData.TextExportFormatter("DAMAGE-b", "Short Damage B",
                                                  "Damage divided by 1,000,000,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-b",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DAMAGE-*",
            new EncounterData.TextExportFormatter("DAMAGE-*", "Short Damage w/suffix",
                                                  "Damage divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DAMAGE-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "dps",
            new EncounterData.TextExportFormatter("dps", "DPS",
                                                  "The damage total of the combatant divided by their personal duration, formatted as 12.34",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "dps", Extra)));
        EncounterData.ExportVariables.Add(
            "dps-*",
            new EncounterData.TextExportFormatter("dps-*", "DPS w/suffix",
                                                  "The damage total of the combatant divided by their personal duration, formatted as 12.34K",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "dps-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DPS",
            new EncounterData.TextExportFormatter("DPS", "Short DPS",
                                                  "The damage total of the combatatant divided by their personal duration, formatted as 12",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DPS", Extra)));
        EncounterData.ExportVariables.Add(
            "DPS-k",
            new EncounterData.TextExportFormatter("DPS-k", "DPS K", "DPS divided by 1,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DPS-k",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DPS-m",
            new EncounterData.TextExportFormatter("DPS-m", "DPS M",
                                                  "DPS divided by 1,000,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DPS-m",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "DPS-*",
            new EncounterData.TextExportFormatter("DPS-*", "DPS w/suffix",
                                                  "DPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "DPS-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "encdps",
            new EncounterData.TextExportFormatter("encdps", "Encounter DPS",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "encdps",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "encdps-*",
            new EncounterData.TextExportFormatter("encdps-*", "Encounter DPS w/suffix",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "encdps-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCDPS",
            new EncounterData.TextExportFormatter("ENCDPS", "Short Encounter DPS",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12 -- This is more commonly used than DPS",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCDPS-k",
            new EncounterData.TextExportFormatter("ENCDPS-k", "Short Encounter DPS K",
                                                  "ENCDPS divided by 1,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-k",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCDPS-m",
            new EncounterData.TextExportFormatter("ENCDPS-m", "Short Encounter DPS M",
                                                  "ENCDPS divided by 1,000,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-m",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCDPS-*",
            new EncounterData.TextExportFormatter("ENCDPS-*", "Short Encounter DPS w/suffix",
                                                  "ENCDPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCDPS-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "hits",
            new EncounterData.TextExportFormatter("hits", "Hits",
                                                  "The number of attack attempts that produced damage.  IE a spell successfully doing damage.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "hits", Extra)));
        EncounterData.ExportVariables.Add(
            "crithits",
            new EncounterData.TextExportFormatter("crithits", "Critical Hit Count",
                                                  "The number of damaging attacks that were critical.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "crithits",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "crithit%",
            new EncounterData.TextExportFormatter("crithit%", "Critical Hit Percentage",
                                                  "The percentage of damaging attacks that were critical.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "crithit%",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "misses",
            new EncounterData.TextExportFormatter("misses", "Misses",
                                                  "The number of auto-attacks or CAs that produced a miss message.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "misses",
                                                                            Extra)));
        EncounterData.ExportVariables.Add("hitfailed",
                                          new EncounterData.TextExportFormatter(
                                              "hitfailed", "Other Avoid",
                                              "Any type of failed attack that was not a miss.  This includes resists, reflects, blocks, dodging, etc.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "hitfailed", Extra)));
        EncounterData.ExportVariables.Add(
            "swings",
            new EncounterData.TextExportFormatter("swings", "Swings (Attacks)",
                                                  "The number of attack attempts.  This includes any auto-attacks or abilities, also including resisted abilities that do no damage.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "swings",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "tohit",
            new EncounterData.TextExportFormatter("tohit", "To Hit %", "The percentage of hits to swings as 12.34",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "tohit",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "TOHIT",
            new EncounterData.TextExportFormatter("TOHIT", "Short To Hit %",
                                                  "The percentage of hits to swings as 12",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "TOHIT",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "maxhit",
            new EncounterData.TextExportFormatter("maxhit", "Highest Hit",
                                                  "The highest single damaging hit formatted as [Combatant-]SkillName-Damage#",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "maxhit",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "MAXHIT",
            new EncounterData.TextExportFormatter("MAXHIT", "Short Highest Hit",
                                                  "The highest single damaging hit formatted as [Combatant-]Damage#",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "maxhit-*",
            new EncounterData.TextExportFormatter("maxhit-*", "Highest Hit w/ suffix",
                                                  "MaxHit divided by 1/K/M/B/T/Q",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "maxhit-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "MAXHIT-*",
            new EncounterData.TextExportFormatter("MAXHIT-*", "Short Highest Hit w/ suffix",
                                                  "Short MaxHit divided by 1/K/M/B/T/Q",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "MAXHIT-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "healed",
            new EncounterData.TextExportFormatter("healed", "Healed",
                                                  "The numerical total of all heals, wards or similar sourced from this combatant.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "healed",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "enchps",
            new EncounterData.TextExportFormatter("enchps", "Encounter HPS",
                                                  "The healing total of the combatant divided by the duration of the encounter, formatted as 12.34",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "enchps",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "enchps-*",
            new EncounterData.TextExportFormatter("enchps-*", "Encounter HPS w/suffix",
                                                  "Encounter HPS divided by 1/K/M/B/T/Q",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "enchps-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCHPS",
            new EncounterData.TextExportFormatter("ENCHPS", "Short Encounter HPS",
                                                  "The healing total of the combatant divided by the duration of the encounter, formatted as 12",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCHPS-k",
            new EncounterData.TextExportFormatter("ENCHPS-k", "Short ENCHPS K",
                                                  "ENCHPS divided by 1,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-k",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCHPS-m",
            new EncounterData.TextExportFormatter("ENCHPS-m", "Short ENCHPS M",
                                                  "ENCHPS divided by 1,000,000 (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-m",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "ENCHPS-*",
            new EncounterData.TextExportFormatter("ENCHPS-*", "Short ENCHPS w/suffix",
                                                  "ENCHPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "ENCHPS-*",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "heals",
            new EncounterData.TextExportFormatter("heals", "Heal Count",
                                                  "The total number of heals from this combatant.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "heals",
                                                                            Extra)));
        EncounterData.ExportVariables.Add("critheals",
                                          new EncounterData.TextExportFormatter(
                                              "critheals", "Critical Heal Count",
                                              "The number of heals that were critical.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "critheals", Extra)));
        EncounterData.ExportVariables.Add("critheal%",
                                          new EncounterData.TextExportFormatter(
                                              "critheal%", "Critical Heal Percentage",
                                              "The percentage of heals that were critical.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "critheal%", Extra)));
        EncounterData.ExportVariables.Add(
            "cures",
            new EncounterData.TextExportFormatter("cures", "Cure or Dispel Count",
                                                  "The total number of times the combatant cured or dispelled",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "cures",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "maxheal",
            new EncounterData.TextExportFormatter("maxheal", "Highest Heal",
                                                  "The highest single healing amount formatted as [Combatant-]SkillName-Healing#",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "maxheal",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "MAXHEAL",
            new EncounterData.TextExportFormatter("MAXHEAL", "Short Highest Heal",
                                                  "The highest single healing amount formatted as [Combatant-]Healing#",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEAL",
                                                                            Extra)));
        EncounterData.ExportVariables.Add("maxhealward",
                                          new EncounterData.TextExportFormatter(
                                              "maxhealward", "Highest Heal/Ward",
                                              "The highest single healing/warding amount formatted as [Combatant-]SkillName-Healing#",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "maxhealward", Extra)));
        EncounterData.ExportVariables.Add("MAXHEALWARD",
                                          new EncounterData.TextExportFormatter(
                                              "MAXHEALWARD", "Short Highest Heal/Ward",
                                              "The highest single healing/warding amount formatted as [Combatant-]Healing#",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "MAXHEALWARD", Extra)));
        EncounterData.ExportVariables.Add("maxheal-*",
                                          new EncounterData.TextExportFormatter(
                                              "maxheal-*", "Highest Heal w/ suffix",
                                              "Highest Heal divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "maxheal-*", Extra)));
        EncounterData.ExportVariables.Add("MAXHEAL-*",
                                          new EncounterData.TextExportFormatter(
                                              "MAXHEAL-*", "Short Highest Heal w/ suffix",
                                              "Short Highest Heal divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "MAXHEAL-*", Extra)));
        EncounterData.ExportVariables.Add("maxhealward-*",
                                          new EncounterData.TextExportFormatter(
                                              "maxhealward-*", "Highest Heal/Ward w/ suffix",
                                              "Highest Heal/Ward divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(Data, SelectiveAllies, "maxhealward-*",
                                                                        Extra)));
        EncounterData.ExportVariables.Add("MAXHEALWARD-*",
                                          new EncounterData.TextExportFormatter(
                                              "MAXHEALWARD-*", "Short Highest Heal/Ward w/ suffix",
                                              "Short Highest Heal/Ward divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(Data, SelectiveAllies, "MAXHEALWARD-*",
                                                                        Extra)));
        EncounterData.ExportVariables.Add("damagetaken",
                                          new EncounterData.TextExportFormatter(
                                              "damagetaken", "Damage Received",
                                              "The total amount of damage this combatant received.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "damagetaken", Extra)));
        EncounterData.ExportVariables.Add("damagetaken-*",
                                          new EncounterData.TextExportFormatter(
                                              "damagetaken-*", "Damage Received w/suffix",
                                              "Damage Received divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(Data, SelectiveAllies, "damagetaken-*",
                                                                        Extra)));
        EncounterData.ExportVariables.Add("healstaken",
                                          new EncounterData.TextExportFormatter(
                                              "healstaken", "Healing Received",
                                              "The total amount of healing this combatant received.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "healstaken", Extra)));
        EncounterData.ExportVariables.Add("healstaken-*",
                                          new EncounterData.TextExportFormatter(
                                              "healstaken-*", "Healing Received w/suffix",
                                              "Healing Received divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "healstaken-*", Extra)));
        EncounterData.ExportVariables.Add("powerdrain",
                                          new EncounterData.TextExportFormatter(
                                              "powerdrain", "Power Drain",
                                              "The amount of power this combatant drained from others.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "powerdrain", Extra)));
        EncounterData.ExportVariables.Add("powerdrain-*",
                                          new EncounterData.TextExportFormatter(
                                              "powerdrain-*", "Power Drain w/suffix",
                                              "Power Drain divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "powerdrain-*", Extra)));
        EncounterData.ExportVariables.Add("powerheal",
                                          new EncounterData.TextExportFormatter(
                                              "powerheal", "Power Replenish",
                                              "The amount of power this combatant replenished to others.",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "powerheal", Extra)));
        EncounterData.ExportVariables.Add("powerheal-*",
                                          new EncounterData.TextExportFormatter(
                                              "powerheal-*", "Power Replenish w/suffix",
                                              "Power Replenish divided by 1/K/M/B/T/Q",
                                              (
                                                      EncounterData Data, List<CombatantData> SelectiveAllies,
                                                      string Extra) =>
                                                  EncounterFormatSwitch(
                                                      Data, SelectiveAllies, "powerheal-*", Extra)));
        EncounterData.ExportVariables.Add(
            "kills",
            new EncounterData.TextExportFormatter("kills", "Killing Blows",
                                                  "The total number of times this character landed a killing blow.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "kills",
                                                                            Extra)));
        EncounterData.ExportVariables.Add(
            "deaths",
            new EncounterData.TextExportFormatter("deaths", "Deaths",
                                                  "The total number of times this character was killed by another.",
                                                  (
                                                          EncounterData Data, List<CombatantData> SelectiveAllies,
                                                          string Extra) =>
                                                      EncounterFormatSwitch(Data, SelectiveAllies, "deaths",
                                                                            Extra)));
        CombatantData.ColumnDefs.Clear();
        CombatantData.ColumnDefs.Add(
            "EncId",
            new CombatantData.ColumnDef("EncId", DefaultVisible: false, "CHAR(8)", "EncId",
                                        (CombatantData Data) => string.Empty,
                                        (CombatantData Data) => Data.Parent.EncId,
                                        (CombatantData Left, CombatantData Right) => 0));
        CombatantData.ColumnDefs.Add(
            "Ally",
            new CombatantData.ColumnDef("Ally", DefaultVisible: false, "CHAR(1)", "Ally",
                                        (CombatantData Data) => Data.Parent.GetAllies().Contains(Data).ToString(),
                                        (CombatantData Data) =>
                                            (!Data.Parent.GetAllies().Contains(Data)) ? "F" : "T",
                                        (CombatantData Left, CombatantData Right) => Left.Parent.GetAllies()
                                            .Contains(Left).CompareTo(Right.Parent.GetAllies().Contains(Right))));
        CombatantData.ColumnDefs.Add(
            "Name",
            new CombatantData.ColumnDef("Name", DefaultVisible: true, "VARCHAR(64)", "Name",
                                        (CombatantData Data) => Data.Name, (CombatantData Data) => Data.Name,
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Name.CompareTo(Right.Name)));
        CombatantData.ColumnDefs.Add("StartTime",
                                     new CombatantData.ColumnDef("StartTime", DefaultVisible: true, "TIMESTAMP",
                                                                 "StartTime",
                                                                 (CombatantData Data) =>
                                                                     (!(Data.StartTime == DateTime.MaxValue))
                                                                         ? Data.StartTime.ToString("T")
                                                                         : "--:--:--",
                                                                 (CombatantData Data) =>
                                                                     (!(Data.StartTime == DateTime.MaxValue))
                                                                         ? Data.StartTime.ToString("u").TrimEnd('Z')
                                                                         : "0000-00-00 00:00:00",
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.StartTime.CompareTo(Right.StartTime)));
        CombatantData.ColumnDefs.Add(
            "EndTime",
            new CombatantData.ColumnDef("EndTime", DefaultVisible: false, "TIMESTAMP", "EndTime",
                                        (CombatantData Data) =>
                                            (!(Data.EndTime == DateTime.MinValue))
                                                ? Data.EndTime.ToString("T")
                                                : "--:--:--",
                                        (CombatantData Data) =>
                                            (!(Data.EndTime == DateTime.MinValue))
                                                ? Data.EndTime.ToString("u").TrimEnd('Z')
                                                : "0000-00-00 00:00:00",
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.EndTime.CompareTo(Right.EndTime)));
        CombatantData.ColumnDefs.Add(
            "Duration",
            new CombatantData.ColumnDef("Duration", DefaultVisible: true, "INT", "Duration",
                                        (CombatantData Data) => Data.DurationS,
                                        (CombatantData Data) => Data.Duration.TotalSeconds.ToString("0"),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Duration.CompareTo(Right.Duration)));
        CombatantData.ColumnDefs.Add(
            "Damage",
            new CombatantData.ColumnDef("Damage", DefaultVisible: true, "BIGINT", "Damage",
                                        (CombatantData Data) => Data.Damage.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Damage.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Damage.CompareTo(Right.Damage)));
        CombatantData.ColumnDefs.Add(
            "Damage%",
            new CombatantData.ColumnDef("Damage%", DefaultVisible: true, "VARCHAR(4)", "DamagePerc",
                                        (CombatantData Data) => Data.DamagePercent,
                                        (CombatantData Data) => Data.DamagePercent,
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Damage.CompareTo(Right.Damage)));
        CombatantData.ColumnDefs.Add(
            "Kills",
            new CombatantData.ColumnDef("Kills", DefaultVisible: false, "INT", "Kills",
                                        (CombatantData Data) => Data.Kills.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Kills.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Kills.CompareTo(Right.Kills)));
        CombatantData.ColumnDefs.Add(
            "Healed",
            new CombatantData.ColumnDef("Healed", DefaultVisible: false, "BIGINT", "Healed",
                                        (CombatantData Data) => Data.Healed.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Healed.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Healed.CompareTo(Right.Healed)));
        CombatantData.ColumnDefs.Add(
            "Healed%",
            new CombatantData.ColumnDef("Healed%", DefaultVisible: false, "VARCHAR(4)", "HealedPerc",
                                        (CombatantData Data) => Data.HealedPercent,
                                        (CombatantData Data) => Data.HealedPercent,
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Healed.CompareTo(Right.Healed)));
        CombatantData.ColumnDefs.Add("CritHeals",
                                     new CombatantData.ColumnDef("CritHeals", DefaultVisible: false, "INT",
                                                                 "CritHeals",
                                                                 (CombatantData Data) =>
                                                                     Data.CritHeals.ToString(
                                                                         (string?)GetIntCommas()),
                                                                 (CombatantData Data) => Data.CritHeals.ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.CritHeals.CompareTo(Right.CritHeals)));
        CombatantData.ColumnDefs.Add(
            "Heals",
            new CombatantData.ColumnDef("Heals", DefaultVisible: false, "INT", "Heals",
                                        (CombatantData Data) => Data.Heals.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Heals.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Heals.CompareTo(Right.Heals)));
        CombatantData.ColumnDefs.Add(
            "Cures",
            new CombatantData.ColumnDef("Cures", DefaultVisible: false, "INT", "CureDispels",
                                        (CombatantData Data) => Data.CureDispels.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.CureDispels.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.CureDispels.CompareTo(Right.CureDispels)));
        CombatantData.ColumnDefs.Add("PowerDrain",
                                     new CombatantData.ColumnDef("PowerDrain", DefaultVisible: true, "BIGINT",
                                                                 "PowerDrain",
                                                                 (CombatantData Data) =>
                                                                     Data.PowerDamage.ToString(
                                                                         (string?)GetIntCommas()),
                                                                 (CombatantData Data) =>
                                                                     Data.PowerDamage.ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.PowerDamage
                                                                         .CompareTo(Right.PowerDamage)));
        CombatantData.ColumnDefs.Add("PowerReplenish",
                                     new CombatantData.ColumnDef("PowerReplenish", DefaultVisible: false, "BIGINT",
                                                                 "PowerReplenish",
                                                                 (CombatantData Data) =>
                                                                     Data.PowerReplenish.ToString(
                                                                         (string?)GetIntCommas()),
                                                                 (CombatantData Data) =>
                                                                     Data.PowerReplenish.ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.PowerReplenish.CompareTo(
                                                                         Right.PowerReplenish)));
        CombatantData.ColumnDefs.Add(
            "DPS",
            new CombatantData.ColumnDef("DPS", DefaultVisible: false, "DOUBLE", "DPS",
                                        (CombatantData Data) => Data.DPS.ToString((string?)GetFloatCommas()),
                                        (CombatantData Data) => Data.DPS.ToString(usCulture),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.DPS.CompareTo(Right.DPS)));
        CombatantData.ColumnDefs.Add(
            "EncDPS",
            new CombatantData.ColumnDef("EncDPS", DefaultVisible: true, "DOUBLE", "EncDPS",
                                        (CombatantData Data) => Data.EncDPS.ToString((string?)GetFloatCommas()),
                                        (CombatantData Data) => Data.EncDPS.ToString(usCulture),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Damage.CompareTo(Right.Damage)));
        CombatantData.ColumnDefs.Add(
            "EncHPS",
            new CombatantData.ColumnDef("EncHPS", DefaultVisible: true, "DOUBLE", "EncHPS",
                                        (CombatantData Data) => Data.EncHPS.ToString((string?)GetFloatCommas()),
                                        (CombatantData Data) => Data.EncHPS.ToString(usCulture),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Healed.CompareTo(Right.Healed)));
        CombatantData.ColumnDefs.Add(
            "Hits",
            new CombatantData.ColumnDef("Hits", DefaultVisible: false, "INT", "Hits",
                                        (CombatantData Data) => Data.Hits.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Hits.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Hits.CompareTo(Right.Hits)));
        CombatantData.ColumnDefs.Add(
            "CritHits",
            new CombatantData.ColumnDef("CritHits", DefaultVisible: false, "INT", "CritHits",
                                        (CombatantData Data) => Data.CritHits.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.CritHits.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.CritHits.CompareTo(Right.CritHits)));
        CombatantData.ColumnDefs.Add(
            "Avoids",
            new CombatantData.ColumnDef("Avoids", DefaultVisible: false, "INT", "Blocked",
                                        (CombatantData Data) => Data.Blocked.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Blocked.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Blocked.CompareTo(Right.Blocked)));
        CombatantData.ColumnDefs.Add(
            "Misses",
            new CombatantData.ColumnDef("Misses", DefaultVisible: false, "INT", "Misses",
                                        (CombatantData Data) => Data.Misses.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Misses.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Misses.CompareTo(Right.Misses)));
        CombatantData.ColumnDefs.Add(
            "Swings",
            new CombatantData.ColumnDef("Swings", DefaultVisible: false, "INT", "Swings",
                                        (CombatantData Data) => Data.Swings.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Swings.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Swings.CompareTo(Right.Swings)));
        CombatantData.ColumnDefs.Add("HealingTaken",
                                     new CombatantData.ColumnDef("HealingTaken", DefaultVisible: false, "BIGINT",
                                                                 "HealsTaken",
                                                                 (CombatantData Data) =>
                                                                     Data.HealsTaken.ToString(
                                                                         (string?)GetIntCommas()),
                                                                 (CombatantData Data) => Data.HealsTaken.ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.HealsTaken.CompareTo(Right.HealsTaken)));
        CombatantData.ColumnDefs.Add("DamageTaken",
                                     new CombatantData.ColumnDef("DamageTaken", DefaultVisible: true, "BIGINT",
                                                                 "DamageTaken",
                                                                 (CombatantData Data) =>
                                                                     Data.DamageTaken.ToString(
                                                                         (string?)GetIntCommas()),
                                                                 (CombatantData Data) =>
                                                                     Data.DamageTaken.ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.DamageTaken
                                                                         .CompareTo(Right.DamageTaken)));
        CombatantData.ColumnDefs.Add(
            "Deaths",
            new CombatantData.ColumnDef("Deaths", DefaultVisible: true, "INT", "Deaths",
                                        (CombatantData Data) => Data.Deaths.ToString((string?)GetIntCommas()),
                                        (CombatantData Data) => Data.Deaths.ToString(),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.Deaths.CompareTo(Right.Deaths)));
        CombatantData.ColumnDefs.Add(
            "ToHit%",
            new CombatantData.ColumnDef("ToHit%", DefaultVisible: false, "FLOAT", "ToHit",
                                        (CombatantData Data) => Data.ToHit.ToString((string?)GetFloatCommas()),
                                        (CombatantData Data) => Data.ToHit.ToString(usCulture),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.ToHit.CompareTo(Right.ToHit)));
        CombatantData.ColumnDefs.Add(
            "CritDam%",
            new CombatantData.ColumnDef("CritDam%", DefaultVisible: false, "VARCHAR(8)", "CritDamPerc",
                                        (CombatantData Data) => Data.CritDamPerc.ToString("0'%"),
                                        (CombatantData Data) => Data.CritDamPerc.ToString("0'%"),
                                        (CombatantData Left, CombatantData Right) =>
                                            Left.CritDamPerc.CompareTo(Right.CritDamPerc)));
        CombatantData.ColumnDefs.Add("CritHeal%",
                                     new CombatantData.ColumnDef("CritHeal%", DefaultVisible: false, "VARCHAR(8)",
                                                                 "CritHealPerc",
                                                                 (CombatantData Data) =>
                                                                     Data.CritHealPerc.ToString("0'%"),
                                                                 (CombatantData Data) =>
                                                                     Data.CritHealPerc.ToString("0'%"),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.CritHealPerc
                                                                         .CompareTo(Right.CritHealPerc)));
        CombatantData.ColumnDefs.Add("CritTypes",
                                     new CombatantData.ColumnDef("CritTypes", DefaultVisible: true, "VARCHAR(32)",
                                                                 "CritTypes", CombatantDataGetCriticalTypes,
                                                                 CombatantDataGetCriticalTypes,
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     CombatantDataGetCriticalTypes(Left)
                                                                         .CompareTo(
                                                                             CombatantDataGetCriticalTypes(
                                                                                 Right))));
        CombatantData.ColumnDefs.Add("Threat +/-",
                                     new CombatantData.ColumnDef("Threat +/-", DefaultVisible: false, "VARCHAR(32)",
                                                                 "ThreatStr",
                                                                 (CombatantData Data) =>
                                                                     Data.GetThreatStr("Threat (Out)"),
                                                                 (CombatantData Data) =>
                                                                     Data.GetThreatStr("Threat (Out)"),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.GetThreatDelta("Threat (Out)")
                                                                         .CompareTo(
                                                                             Right.GetThreatDelta(
                                                                                 "Threat (Out)"))));
        CombatantData.ColumnDefs.Add("ThreatDelta",
                                     new CombatantData.ColumnDef("ThreatDelta", DefaultVisible: false, "BIGINT",
                                                                 "ThreatDelta",
                                                                 (CombatantData Data) =>
                                                                     Data.GetThreatDelta("Threat (Out)")
                                                                         .ToString((string?)GetIntCommas()),
                                                                 (CombatantData Data) =>
                                                                     Data.GetThreatDelta("Threat (Out)").ToString(),
                                                                 (CombatantData Left, CombatantData Right) =>
                                                                     Left.GetThreatDelta("Threat (Out)")
                                                                         .CompareTo(
                                                                             Right.GetThreatDelta(
                                                                                 "Threat (Out)"))));
        CombatantData.ColumnDefs["Damage"].GetCellForeColor = (CombatantData Data) => Color.DarkRed;
        CombatantData.ColumnDefs["Damage%"].GetCellForeColor = (CombatantData Data) => Color.DarkRed;
        CombatantData.ColumnDefs["Healed"].GetCellForeColor = (CombatantData Data) => Color.DarkBlue;
        CombatantData.ColumnDefs["Healed%"].GetCellForeColor = (CombatantData Data) => Color.DarkBlue;
        CombatantData.ColumnDefs["PowerDrain"].GetCellForeColor = (CombatantData Data) => Color.DarkMagenta;
        CombatantData.ColumnDefs["DPS"].GetCellForeColor = (CombatantData Data) => Color.DarkRed;
        CombatantData.ColumnDefs["EncDPS"].GetCellForeColor = (CombatantData Data) => Color.DarkRed;
        CombatantData.ColumnDefs["EncHPS"].GetCellForeColor = (CombatantData Data) => Color.DarkBlue;
        CombatantData.ColumnDefs["DamageTaken"].GetCellForeColor = (CombatantData Data) => Color.DarkOrange;
        CombatantData.OutgoingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {
                "Auto-Attack (Out)",
                new CombatantData.DamageTypeDef("Auto-Attack (Out)", -1, Color.DarkGoldenrod)
            },
            {
                "Skill/Ability (Out)",
                new CombatantData.DamageTypeDef("Skill/Ability (Out)", -1, Color.DarkOrange)
            },
            {
                "Outgoing Damage",
                new CombatantData.DamageTypeDef("Outgoing Damage", 0, Color.Orange)
            },
            {
                "Healed (Out)",
                new CombatantData.DamageTypeDef("Healed (Out)", 1, Color.Blue)
            },
            {
                "Power Drain (Out)",
                new CombatantData.DamageTypeDef("Power Drain (Out)", -1, Color.Purple)
            },
            {
                "Power Replenish (Out)",
                new CombatantData.DamageTypeDef("Power Replenish (Out)", 1, Color.Violet)
            },
            {
                "Cure/Dispel (Out)",
                new CombatantData.DamageTypeDef("Cure/Dispel (Out)", 0, Color.Wheat)
            },
            {
                "Threat (Out)",
                new CombatantData.DamageTypeDef("Threat (Out)", -1, Color.Yellow)
            },
            {
                "All Outgoing (Ref)",
                new CombatantData.DamageTypeDef("All Outgoing (Ref)", 0, Color.Black)
            }
        };
        CombatantData.IncomingDamageTypeDataObjects = new Dictionary<string, CombatantData.DamageTypeDef>
        {
            {
                "Incoming Damage",
                new CombatantData.DamageTypeDef("Incoming Damage", -1, Color.Red)
            },
            {
                "Healed (Inc)",
                new CombatantData.DamageTypeDef("Healed (Inc)", 1, Color.LimeGreen)
            },
            {
                "Power Drain (Inc)",
                new CombatantData.DamageTypeDef("Power Drain (Inc)", -1, Color.Magenta)
            },
            {
                "Power Replenish (Inc)",
                new CombatantData.DamageTypeDef("Power Replenish (Inc)", 1, Color.MediumPurple)
            },
            {
                "Cure/Dispel (Inc)",
                new CombatantData.DamageTypeDef("Cure/Dispel (Inc)", 0, Color.Wheat)
            },
            {
                "Threat (Inc)",
                new CombatantData.DamageTypeDef("Threat (Inc)", -1, Color.Yellow)
            },
            {
                "All Incoming (Ref)",
                new CombatantData.DamageTypeDef("All Incoming (Ref)", 0, Color.Black)
            }
        };
        CombatantData.SwingTypeToDamageTypeDataLinksOutgoing = new SortedDictionary<int, List<string>>
        {
            {
                1,
                new List<string> { "Auto-Attack (Out)", "Outgoing Damage" }
            },
            {
                2,
                new List<string> { "Skill/Ability (Out)", "Outgoing Damage" }
            },
            {
                3,
                new List<string> { "Healed (Out)" }
            },
            {
                10,
                new List<string> { "Power Drain (Out)" }
            },
            {
                13,
                new List<string> { "Power Replenish (Out)" }
            },
            {
                20,
                new List<string> { "Cure/Dispel (Out)" }
            },
            {
                16,
                new List<string> { "Threat (Out)" }
            }
        };
        CombatantData.SwingTypeToDamageTypeDataLinksIncoming = new SortedDictionary<int, List<string>>
        {
            {
                1,
                new List<string> { "Incoming Damage" }
            },
            {
                2,
                new List<string> { "Incoming Damage" }
            },
            {
                3,
                new List<string> { "Healed (Inc)" }
            },
            {
                10,
                new List<string> { "Power Drain (Inc)" }
            },
            {
                13,
                new List<string> { "Power Replenish (Inc)" }
            },
            {
                20,
                new List<string> { "Cure/Dispel (Inc)" }
            },
            {
                16,
                new List<string> { "Threat (Inc)" }
            }
        };
        CombatantData.DamageSwingTypes = new List<int> { 1, 2 };
        CombatantData.HealingSwingTypes = new List<int> { 3 };
        CombatantData.DamageTypeDataNonSkillDamage = "Auto-Attack (Out)";
        CombatantData.DamageTypeDataOutgoingDamage = "Outgoing Damage";
        CombatantData.DamageTypeDataOutgoingHealing = "Healed (Out)";
        CombatantData.DamageTypeDataIncomingDamage = "Incoming Damage";
        CombatantData.DamageTypeDataIncomingHealing = "Healed (Inc)";
        CombatantData.DamageTypeDataOutgoingPowerReplenish = "Power Replenish (Out)";
        CombatantData.DamageTypeDataOutgoingPowerDamage = "Power Drain (Out)";
        CombatantData.DamageTypeDataOutgoingCures = "Cure/Dispel (Out)";
        CombatantData.ExportVariables.Clear();
        CombatantData.ExportVariables.Add(
            "n",
            new CombatantData.TextExportFormatter("n", "New Line",
                                                  "Formatting after this element will appear on a new line.",
                                                  (CombatantData Data, string Extra) => "\n"));
        CombatantData.ExportVariables.Add(
            "t",
            new CombatantData.TextExportFormatter("t", "Tab Character",
                                                  "Formatting after this element will appear in a relative column arrangement.  (The formatting example cannot display this properly)",
                                                  (CombatantData Data, string Extra) => "\t"));
        CombatantData.ExportVariables.Add(
            "name",
            new CombatantData.TextExportFormatter("name", "Name", "The combatant's name.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "name", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME",
            new CombatantData.TextExportFormatter("NAME", "Short Name",
                                                  "The combatant's name shortened to a number of characters after a colon, like: \"NAME:5\"",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME", Extra)));
        CombatantData.ExportVariables.Add(
            "duration",
            new CombatantData.TextExportFormatter("duration", "Duration",
                                                  "The duration of the combatant or the duration of the encounter, displayed as mm:ss",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "duration", Extra)));
        CombatantData.ExportVariables.Add(
            "DURATION",
            new CombatantData.TextExportFormatter("DURATION", "Short Duration",
                                                  "The duration of the combatant or encounter displayed in whole seconds.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DURATION", Extra)));
        CombatantData.ExportVariables.Add(
            "damage",
            new CombatantData.TextExportFormatter("damage", "Damage",
                                                  "The amount of damage from auto-attack, spells, CAs, etc done to other combatants.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "damage", Extra)));
        CombatantData.ExportVariables.Add(
            "damage-m",
            new CombatantData.TextExportFormatter("damage-m", "Damage M",
                                                  "Damage divided by 1,000,000 (with two decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "damage-m", Extra)));
        CombatantData.ExportVariables.Add(
            "damage-b",
            new CombatantData.TextExportFormatter("damage-b", "Damage B",
                                                  "Damage divided by 1,000,000,000 (with two decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "damage-b", Extra)));
        CombatantData.ExportVariables.Add(
            "damage-*",
            new CombatantData.TextExportFormatter("damage-*", "Damage w/suffix",
                                                  "Damage divided by 1/K/M/B/T/Q (with one decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "damage-*", Extra)));
        CombatantData.ExportVariables.Add(
            "DAMAGE-k",
            new CombatantData.TextExportFormatter("DAMAGE-k", "Short Damage K",
                                                  "Damage divided by 1,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DAMAGE-k", Extra)));
        CombatantData.ExportVariables.Add(
            "DAMAGE-m",
            new CombatantData.TextExportFormatter("DAMAGE-m", "Short Damage M",
                                                  "Damage divided by 1,000,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DAMAGE-m", Extra)));
        CombatantData.ExportVariables.Add(
            "DAMAGE-b",
            new CombatantData.TextExportFormatter("DAMAGE-b", "Short Damage B",
                                                  "Damage divided by 1,000,000,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DAMAGE-b", Extra)));
        CombatantData.ExportVariables.Add(
            "DAMAGE-*",
            new CombatantData.TextExportFormatter("DAMAGE-*", "Short Damage w/suffix",
                                                  "Damage divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DAMAGE-*", Extra)));
        CombatantData.ExportVariables.Add(
            "damage%",
            new CombatantData.TextExportFormatter("damage%", "Damage %",
                                                  "This value represents the percent share of all damage done by allies in this encounter.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "damage%", Extra)));
        CombatantData.ExportVariables.Add(
            "dps",
            new CombatantData.TextExportFormatter("dps", "DPS",
                                                  "The damage total of the combatant divided by their personal duration, formatted as 12.34",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "dps", Extra)));
        CombatantData.ExportVariables.Add(
            "dps-*",
            new CombatantData.TextExportFormatter("dps-*", "DPS w/suffix",
                                                  "The damage total of the combatant divided by their personal duration, formatted as 12.34K",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "dps-*", Extra)));
        CombatantData.ExportVariables.Add(
            "DPS",
            new CombatantData.TextExportFormatter("DPS", "Short DPS",
                                                  "The damage total of the combatatant divided by their personal duration, formatted as 12K",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DPS", Extra)));
        CombatantData.ExportVariables.Add(
            "DPS-k",
            new CombatantData.TextExportFormatter("DPS-k", "Short DPS K",
                                                  "Short DPS divided by 1,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DPS-k", Extra)));
        CombatantData.ExportVariables.Add(
            "DPS-m",
            new CombatantData.TextExportFormatter("DPS-m", "Short DPS M",
                                                  "Short DPS divided by 1,000,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DPS-m", Extra)));
        CombatantData.ExportVariables.Add(
            "DPS-*",
            new CombatantData.TextExportFormatter("DPS-*", "Short DPS w/suffix",
                                                  "Short DPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "DPS-*", Extra)));
        CombatantData.ExportVariables.Add(
            "encdps",
            new CombatantData.TextExportFormatter("encdps", "Encounter DPS",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "encdps", Extra)));
        CombatantData.ExportVariables.Add(
            "encdps-*",
            new CombatantData.TextExportFormatter("encdps-*", "Encounter DPS w/suffix",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12.34 -- This is more commonly used than DPS",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "encdps-*", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCDPS",
            new CombatantData.TextExportFormatter("ENCDPS", "Short Encounter DPS",
                                                  "The damage total of the combatant divided by the duration of the encounter, formatted as 12 -- This is more commonly used than DPS",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCDPS", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCDPS-k",
            new CombatantData.TextExportFormatter("ENCDPS-k", "Short Encounter DPS K",
                                                  "Short Encounter DPS divided by 1,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCDPS-k", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCDPS-m",
            new CombatantData.TextExportFormatter("ENCDPS-m", "Short Encounter DPS M",
                                                  "Short Encounter DPS divided by 1,000,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCDPS-m", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCDPS-*",
            new CombatantData.TextExportFormatter("ENCDPS-*", "Short Encounter DPS w/suffix",
                                                  "Short Encounter DPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCDPS-*", Extra)));
        CombatantData.ExportVariables.Add(
            "hits",
            new CombatantData.TextExportFormatter("hits", "Hits",
                                                  "The number of attack attempts that produced damage.  IE a spell successfully doing damage.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "hits", Extra)));
        CombatantData.ExportVariables.Add(
            "crithits",
            new CombatantData.TextExportFormatter("crithits", "Critical Hit Count",
                                                  "The number of damaging attacks that were critical.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "crithits", Extra)));
        CombatantData.ExportVariables.Add(
            "crithit%",
            new CombatantData.TextExportFormatter("crithit%", "Critical Hit Percentage",
                                                  "The percentage of damaging attacks that were critical.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "crithit%", Extra)));
        CombatantData.ExportVariables.Add("crittypes",
                                          new CombatantData.TextExportFormatter(
                                              "crittypes", "Critical Types",
                                              "Distribution of Critical Types  (Normal|Legendary|Fabled|Mythical)",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "crittypes", Extra)));
        CombatantData.ExportVariables.Add(
            "misses",
            new CombatantData.TextExportFormatter("misses", "Misses",
                                                  "The number of auto-attacks or CAs that produced a miss message.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "misses", Extra)));
        CombatantData.ExportVariables.Add("hitfailed",
                                          new CombatantData.TextExportFormatter(
                                              "hitfailed", "Other Avoid",
                                              "Any type of failed attack that was not a miss.  This includes resists, reflects, blocks, dodging, etc.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "hitfailed", Extra)));
        CombatantData.ExportVariables.Add(
            "swings",
            new CombatantData.TextExportFormatter("swings", "Swings (Attacks)",
                                                  "The number of attack attempts.  This includes any auto-attacks or abilities, also including resisted abilities that do no damage.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "swings", Extra)));
        CombatantData.ExportVariables.Add(
            "tohit",
            new CombatantData.TextExportFormatter("tohit", "To Hit %", "The percentage of hits to swings as 12.34",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "tohit", Extra)));
        CombatantData.ExportVariables.Add(
            "TOHIT",
            new CombatantData.TextExportFormatter("TOHIT", "Short To Hit %",
                                                  "The percentage of hits to swings as 12",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "TOHIT", Extra)));
        CombatantData.ExportVariables.Add(
            "maxhit",
            new CombatantData.TextExportFormatter("maxhit", "Highest Hit",
                                                  "The highest single damaging hit formatted as [Combatant-]SkillName-Damage#",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "maxhit", Extra)));
        CombatantData.ExportVariables.Add(
            "MAXHIT",
            new CombatantData.TextExportFormatter("MAXHIT", "Short Highest Hit",
                                                  "The highest single damaging hit formatted as [Combatant-]Damage#",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "MAXHIT", Extra)));
        CombatantData.ExportVariables.Add(
            "maxhit-*",
            new CombatantData.TextExportFormatter("maxhit-*", "Highest Hit w/ suffix",
                                                  "MaxHit divided by 1/K/M/B/T/Q",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "maxhit-*", Extra)));
        CombatantData.ExportVariables.Add(
            "MAXHIT-*",
            new CombatantData.TextExportFormatter("MAXHIT-*", "Short Highest Hit w/ suffix",
                                                  "Short MaxHit divided by 1/K/M/B/T/Q",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "MAXHIT-*", Extra)));
        CombatantData.ExportVariables.Add(
            "healed",
            new CombatantData.TextExportFormatter("healed", "Healed",
                                                  "The numerical total of all heals, wards or similar sourced from this combatant.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "healed", Extra)));
        CombatantData.ExportVariables.Add(
            "healed%",
            new CombatantData.TextExportFormatter("healed%", "Healed %",
                                                  "This value represents the percent share of all healing done by allies in this encounter.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "healed%", Extra)));
        CombatantData.ExportVariables.Add(
            "enchps",
            new CombatantData.TextExportFormatter("enchps", "Encounter HPS",
                                                  "The healing total of the combatant divided by the duration of the encounter, formatted as 12.34",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "enchps", Extra)));
        CombatantData.ExportVariables.Add(
            "enchps-*",
            new CombatantData.TextExportFormatter("enchps-*", "Encounter HPS w/suffix",
                                                  "Encounter HPS divided by 1/K/M/B/T/Q",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "enchps-*", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCHPS",
            new CombatantData.TextExportFormatter("ENCHPS", "Short Encounter HPS",
                                                  "The healing total of the combatant divided by the duration of the encounter, formatted as 12",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCHPS", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCHPS-k",
            new CombatantData.TextExportFormatter("ENCHPS-k", "Short Encounter HPS K",
                                                  "Short Encounter HPS divided by 1,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCHPS-k", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCHPS-m",
            new CombatantData.TextExportFormatter("ENCHPS-m", "Short Encounter HPS M",
                                                  "Short Encounter HPS divided by 1,000,000 (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCHPS-m", Extra)));
        CombatantData.ExportVariables.Add(
            "ENCHPS-*",
            new CombatantData.TextExportFormatter("ENCHPS-*", "Short Encounter HPS w/suffix",
                                                  "Short Encounter HPS divided by 1/K/M/B/T/Q (with no decimal places)",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "ENCHPS-*", Extra)));
        CombatantData.ExportVariables.Add("critheals",
                                          new CombatantData.TextExportFormatter(
                                              "critheals", "Critical Heal Count",
                                              "The number of heals that were critical.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "critheals", Extra)));
        CombatantData.ExportVariables.Add("critheal%",
                                          new CombatantData.TextExportFormatter(
                                              "critheal%", "Critical Heal Percentage",
                                              "The percentage of heals that were critical.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "critheal%", Extra)));
        CombatantData.ExportVariables.Add(
            "heals",
            new CombatantData.TextExportFormatter("heals", "Heal Count",
                                                  "The total number of heals from this combatant.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "heals", Extra)));
        CombatantData.ExportVariables.Add(
            "cures",
            new CombatantData.TextExportFormatter("cures", "Cure or Dispel Count",
                                                  "The total number of times the combatant cured or dispelled",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "cures", Extra)));
        CombatantData.ExportVariables.Add(
            "maxheal",
            new CombatantData.TextExportFormatter("maxheal", "Highest Heal",
                                                  "The highest single healing amount formatted as [Combatant-]SkillName-Healing#",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "maxheal", Extra)));
        CombatantData.ExportVariables.Add(
            "MAXHEAL",
            new CombatantData.TextExportFormatter("MAXHEAL", "Short Highest Heal",
                                                  "The highest single healing amount formatted as [Combatant-]Healing#",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "MAXHEAL", Extra)));
        CombatantData.ExportVariables.Add("maxhealward",
                                          new CombatantData.TextExportFormatter(
                                              "maxhealward", "Highest Heal/Ward",
                                              "The highest single healing/warding amount formatted as [Combatant-]SkillName-Healing#",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "maxhealward", Extra)));
        CombatantData.ExportVariables.Add("MAXHEALWARD",
                                          new CombatantData.TextExportFormatter(
                                              "MAXHEALWARD", "Short Highest Heal/Ward",
                                              "The highest single healing/warding amount formatted as [Combatant-]Healing#",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "MAXHEALWARD", Extra)));
        CombatantData.ExportVariables.Add("maxheal-*",
                                          new CombatantData.TextExportFormatter(
                                              "maxheal-*", "Highest Heal w/ suffix",
                                              "Highest Heal divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "maxheal-*", Extra)));
        CombatantData.ExportVariables.Add("MAXHEAL-*",
                                          new CombatantData.TextExportFormatter(
                                              "MAXHEAL-*", "Short Highest Heal w/ suffix",
                                              "Short Highest Heal divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "MAXHEAL-*", Extra)));
        CombatantData.ExportVariables.Add("maxhealward-*",
                                          new CombatantData.TextExportFormatter(
                                              "maxhealward-*", "Highest Heal/Ward w/ suffix",
                                              "Highest Heal/Ward divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "maxhealward-*", Extra)));
        CombatantData.ExportVariables.Add("MAXHEALWARD-*",
                                          new CombatantData.TextExportFormatter(
                                              "MAXHEALWARD-*", "Short Highest Heal/Ward w/ suffix",
                                              "Short Highest Heal/Ward divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "MAXHEALWARD-*", Extra)));
        CombatantData.ExportVariables.Add("damagetaken",
                                          new CombatantData.TextExportFormatter(
                                              "damagetaken", "Damage Received",
                                              "The total amount of damage this combatant received.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "damagetaken", Extra)));
        CombatantData.ExportVariables.Add("damagetaken-*",
                                          new CombatantData.TextExportFormatter(
                                              "damagetaken-*", "Damage Received w/suffix",
                                              "Damage Received divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "damagetaken-*", Extra)));
        CombatantData.ExportVariables.Add("healstaken",
                                          new CombatantData.TextExportFormatter(
                                              "healstaken", "Healing Received",
                                              "The total amount of healing this combatant received.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "healstaken", Extra)));
        CombatantData.ExportVariables.Add("healstaken-*",
                                          new CombatantData.TextExportFormatter(
                                              "healstaken-*", "Healing Received w/suffix",
                                              "Healing Received divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "healstaken-*", Extra)));
        CombatantData.ExportVariables.Add("powerdrain",
                                          new CombatantData.TextExportFormatter(
                                              "powerdrain", "Power Drain",
                                              "The amount of power this combatant drained from others.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "powerdrain", Extra)));
        CombatantData.ExportVariables.Add("powerdrain-*",
                                          new CombatantData.TextExportFormatter(
                                              "powerdrain-*", "Power Drain w/suffix",
                                              "Power Drain divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "powerdrain-*", Extra)));
        CombatantData.ExportVariables.Add("powerheal",
                                          new CombatantData.TextExportFormatter(
                                              "powerheal", "Power Replenish",
                                              "The amount of power this combatant replenished to others.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "powerheal", Extra)));
        CombatantData.ExportVariables.Add("powerheal-*",
                                          new CombatantData.TextExportFormatter(
                                              "powerheal-*", "Power Replenish w/suffix",
                                              "Power Replenish divided by 1/K/M/B/T/Q",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "powerheal-*", Extra)));
        CombatantData.ExportVariables.Add(
            "kills",
            new CombatantData.TextExportFormatter("kills", "Killing Blows",
                                                  "The total number of times this character landed a killing blow.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "kills", Extra)));
        CombatantData.ExportVariables.Add(
            "deaths",
            new CombatantData.TextExportFormatter("deaths", "Deaths",
                                                  "The total number of times this character was killed by another.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "deaths", Extra)));
        CombatantData.ExportVariables.Add("threatstr",
                                          new CombatantData.TextExportFormatter(
                                              "threatstr", "Threat Increase/Decrease",
                                              "The amount of direct threat output that was increased/decreased.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "threatstr", Extra)));
        CombatantData.ExportVariables.Add("threatdelta",
                                          new CombatantData.TextExportFormatter(
                                              "threatdelta", "Threat Delta",
                                              "The amount of direct threat output relative to zero.",
                                              (CombatantData Data, string Extra) =>
                                                  CombatantFormatSwitch(Data, "threatdelta", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME3",
            new CombatantData.TextExportFormatter("NAME3", "Name (3 chars)",
                                                  "The combatant's name, up to 3 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME3", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME4",
            new CombatantData.TextExportFormatter("NAME4", "Name (4 chars)",
                                                  "The combatant's name, up to 4 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME4", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME5",
            new CombatantData.TextExportFormatter("NAME5", "Name (5 chars)",
                                                  "The combatant's name, up to 5 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME5", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME6",
            new CombatantData.TextExportFormatter("NAME6", "Name (6 chars)",
                                                  "The combatant's name, up to 6 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME6", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME7",
            new CombatantData.TextExportFormatter("NAME7", "Name (7 chars)",
                                                  "The combatant's name, up to 7 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME7", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME8",
            new CombatantData.TextExportFormatter("NAME8", "Name (8 chars)",
                                                  "The combatant's name, up to 8 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME8", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME9",
            new CombatantData.TextExportFormatter("NAME9", "Name (9 chars)",
                                                  "The combatant's name, up to 9 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME9", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME10",
            new CombatantData.TextExportFormatter("NAME10", "Name (10 chars)",
                                                  "The combatant's name, up to 10 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME10", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME11",
            new CombatantData.TextExportFormatter("NAME11", "Name (11 chars)",
                                                  "The combatant's name, up to 11 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME11", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME12",
            new CombatantData.TextExportFormatter("NAME12", "Name (12 chars)",
                                                  "The combatant's name, up to 12 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME12", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME13",
            new CombatantData.TextExportFormatter("NAME13", "Name (13 chars)",
                                                  "The combatant's name, up to 13 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME13", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME14",
            new CombatantData.TextExportFormatter("NAME14", "Name (14 chars)",
                                                  "The combatant's name, up to 14 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME14", Extra)));
        CombatantData.ExportVariables.Add(
            "NAME15",
            new CombatantData.TextExportFormatter("NAME15", "Name (15 chars)",
                                                  "The combatant's name, up to 15 characters will be displayed.",
                                                  (CombatantData Data, string Extra) =>
                                                      CombatantFormatSwitch(Data, "NAME15", Extra)));
        DamageTypeData.ColumnDefs.Clear();
        DamageTypeData.ColumnDefs.Add(
            "EncId",
            new DamageTypeData.ColumnDef("EncId", DefaultVisible: false, "CHAR(8)", "EncId",
                                         (DamageTypeData Data) => string.Empty,
                                         (DamageTypeData Data) => Data.Parent.Parent.EncId));
        DamageTypeData.ColumnDefs.Add("Combatant",
                                      new DamageTypeData.ColumnDef("Combatant", DefaultVisible: false,
                                                                   "VARCHAR(64)", "Combatant",
                                                                   (DamageTypeData Data) => Data.Parent.Name,
                                                                   (DamageTypeData Data) => Data.Parent.Name));
        DamageTypeData.ColumnDefs.Add(
            "Grouping",
            new DamageTypeData.ColumnDef("Grouping", DefaultVisible: false, "VARCHAR(92)", "Grouping",
                                         (DamageTypeData Data) => string.Empty, GetDamageTypeGrouping));
        DamageTypeData.ColumnDefs.Add(
            "Type",
            new DamageTypeData.ColumnDef("Type", DefaultVisible: true, "VARCHAR(64)", "Type",
                                         (DamageTypeData Data) => Data.Type, (DamageTypeData Data) => Data.Type));
        DamageTypeData.ColumnDefs.Add("StartTime",
                                      new DamageTypeData.ColumnDef("StartTime", DefaultVisible: false, "TIMESTAMP",
                                                                   "StartTime",
                                                                   (DamageTypeData Data) =>
                                                                       (!(Data.StartTime == DateTime.MaxValue))
                                                                           ? Data.StartTime.ToString("T")
                                                                           : "--:--:--",
                                                                   (DamageTypeData Data) =>
                                                                       (!(Data.StartTime == DateTime.MaxValue))
                                                                           ? Data.StartTime.ToString("u")
                                                                               .TrimEnd('Z')
                                                                           : "0000-00-00 00:00:00"));
        DamageTypeData.ColumnDefs.Add(
            "EndTime",
            new DamageTypeData.ColumnDef("EndTime", DefaultVisible: false, "TIMESTAMP", "EndTime",
                                         (DamageTypeData Data) =>
                                             (!(Data.EndTime == DateTime.MinValue))
                                                 ? Data.EndTime.ToString("T")
                                                 : "--:--:--",
                                         (DamageTypeData Data) =>
                                             (!(Data.EndTime == DateTime.MinValue))
                                                 ? Data.EndTime.ToString("u").TrimEnd('Z')
                                                 : "0000-00-00 00:00:00"));
        DamageTypeData.ColumnDefs.Add(
            "Duration",
            new DamageTypeData.ColumnDef("Duration", DefaultVisible: false, "INT", "Duration",
                                         (DamageTypeData Data) => Data.DurationS,
                                         (DamageTypeData Data) => Data.Duration.TotalSeconds.ToString("0")));
        DamageTypeData.ColumnDefs.Add(
            "Damage",
            new DamageTypeData.ColumnDef("Damage", DefaultVisible: true, "BIGINT", "Damage",
                                         (DamageTypeData Data) => Data.Damage.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Damage.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "EncDPS",
            new DamageTypeData.ColumnDef("EncDPS", DefaultVisible: true, "DOUBLE", "EncDPS",
                                         (DamageTypeData Data) => Data.EncDPS.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.EncDPS.ToString(usCulture)));
        DamageTypeData.ColumnDefs.Add(
            "CharDPS",
            new DamageTypeData.ColumnDef("CharDPS", DefaultVisible: false, "DOUBLE", "CharDPS",
                                         (DamageTypeData Data) => Data.CharDPS.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.CharDPS.ToString(usCulture)));
        DamageTypeData.ColumnDefs.Add(
            "DPS",
            new DamageTypeData.ColumnDef("DPS", DefaultVisible: false, "DOUBLE", "DPS",
                                         (DamageTypeData Data) => Data.DPS.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.DPS.ToString(usCulture)));
        DamageTypeData.ColumnDefs.Add(
            "Average",
            new DamageTypeData.ColumnDef("Average", DefaultVisible: true, "DOUBLE", "Average",
                                         (DamageTypeData Data) => Data.Average.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.Average.ToString(usCulture)));
        DamageTypeData.ColumnDefs.Add(
            "Median",
            new DamageTypeData.ColumnDef("Median", DefaultVisible: false, "BIGINT", "Median",
                                         (DamageTypeData Data) => Data.Median.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Median.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "MinHit",
            new DamageTypeData.ColumnDef("MinHit", DefaultVisible: true, "BIGINT", "MinHit",
                                         (DamageTypeData Data) => Data.MinHit.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.MinHit.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "MaxHit",
            new DamageTypeData.ColumnDef("MaxHit", DefaultVisible: true, "BIGINT", "MaxHit",
                                         (DamageTypeData Data) => Data.MaxHit.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.MaxHit.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "Hits",
            new DamageTypeData.ColumnDef("Hits", DefaultVisible: true, "INT", "Hits",
                                         (DamageTypeData Data) => Data.Hits.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Hits.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "CritHits",
            new DamageTypeData.ColumnDef("CritHits", DefaultVisible: false, "INT", "CritHits",
                                         (DamageTypeData Data) => Data.CritHits.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.CritHits.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "Avoids",
            new DamageTypeData.ColumnDef("Avoids", DefaultVisible: false, "INT", "Blocked",
                                         (DamageTypeData Data) => Data.Blocked.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Blocked.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "Misses",
            new DamageTypeData.ColumnDef("Misses", DefaultVisible: false, "INT", "Misses",
                                         (DamageTypeData Data) => Data.Misses.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Misses.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "Swings",
            new DamageTypeData.ColumnDef("Swings", DefaultVisible: true, "INT", "Swings",
                                         (DamageTypeData Data) => Data.Swings.ToString((string?)GetIntCommas()),
                                         (DamageTypeData Data) => Data.Swings.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "ToHit",
            new DamageTypeData.ColumnDef("ToHit", DefaultVisible: false, "FLOAT", "ToHit",
                                         (DamageTypeData Data) => Data.ToHit.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.ToHit.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "AvgDelay",
            new DamageTypeData.ColumnDef("AvgDelay", DefaultVisible: false, "FLOAT", "AverageDelay",
                                         (DamageTypeData Data) =>
                                             Data.AverageDelay.ToString((string?)GetFloatCommas()),
                                         (DamageTypeData Data) => Data.AverageDelay.ToString()));
        DamageTypeData.ColumnDefs.Add(
            "Crit%",
            new DamageTypeData.ColumnDef("Crit%", DefaultVisible: false, "VARCHAR(8)", "CritPerc",
                                         (DamageTypeData Data) => Data.CritPerc.ToString("0'%"),
                                         (DamageTypeData Data) => Data.CritPerc.ToString("0'%")));
        DamageTypeData.ColumnDefs.Add("CritTypes",
                                      new DamageTypeData.ColumnDef("CritTypes", DefaultVisible: true, "VARCHAR(32)",
                                                                   "CritTypes", DamageTypeDataGetCriticalTypes,
                                                                   DamageTypeDataGetCriticalTypes));
        AttackType.ColumnDefs.Clear();
        AttackType.ColumnDefs.Add(
            "EncId",
            new AttackType.ColumnDef("EncId", DefaultVisible: false, "CHAR(8)", "EncId",
                                     (AttackType Data) => string.Empty,
                                     (AttackType Data) => Data.Parent.Parent.Parent.EncId,
                                     (AttackType Left, AttackType Right) => 0));
        AttackType.ColumnDefs.Add(
            "Attacker",
            new AttackType.ColumnDef("Attacker", DefaultVisible: false, "VARCHAR(64)", "Attacker",
                                     (AttackType Data) =>
                                         (!Data.Parent.Outgoing) ? string.Empty : Data.Parent.Parent.Name,
                                     (AttackType Data) =>
                                         (!Data.Parent.Outgoing) ? string.Empty : Data.Parent.Parent.Name,
                                     (AttackType Left, AttackType Right) => 0));
        AttackType.ColumnDefs.Add(
            "Victim",
            new AttackType.ColumnDef("Victim", DefaultVisible: false, "VARCHAR(64)", "Victim",
                                     (AttackType Data) =>
                                         (!Data.Parent.Outgoing) ? Data.Parent.Parent.Name : string.Empty,
                                     (AttackType Data) =>
                                         (!Data.Parent.Outgoing) ? Data.Parent.Parent.Name : string.Empty,
                                     (AttackType Left, AttackType Right) => 0));
        AttackType.ColumnDefs.Add("SwingType",
                                  new AttackType.ColumnDef("SwingType", DefaultVisible: false, "TINYINT",
                                                           "SwingType", GetAttackTypeSwingType,
                                                           GetAttackTypeSwingType,
                                                           (AttackType Left, AttackType Right) => 0));
        AttackType.ColumnDefs.Add(
            "Type",
            new AttackType.ColumnDef("Type", DefaultVisible: true, "VARCHAR(64)", "Type",
                                     (AttackType Data) => Data.Type, (AttackType Data) => Data.Type,
                                     (AttackType Left, AttackType Right) => Left.Type.CompareTo(Right.Type)));
        AttackType.ColumnDefs.Add("StartTime",
                                  new AttackType.ColumnDef("StartTime", DefaultVisible: false, "TIMESTAMP",
                                                           "StartTime",
                                                           (AttackType Data) =>
                                                               (!(Data.StartTime == DateTime.MaxValue))
                                                                   ? Data.StartTime.ToString("T")
                                                                   : "--:--:--",
                                                           (AttackType Data) =>
                                                               (!(Data.StartTime == DateTime.MaxValue))
                                                                   ? Data.StartTime.ToString("u").TrimEnd('Z')
                                                                   : "0000-00-00 00:00:00",
                                                           (AttackType Left, AttackType Right) =>
                                                               Left.StartTime.CompareTo(Right.StartTime)));
        AttackType.ColumnDefs.Add(
            "EndTime",
            new AttackType.ColumnDef("EndTime", DefaultVisible: false, "TIMESTAMP", "EndTime",
                                     (AttackType Data) =>
                                         (!(Data.EndTime == DateTime.MinValue))
                                             ? Data.EndTime.ToString("T")
                                             : "--:--:--",
                                     (AttackType Data) =>
                                         (!(Data.EndTime == DateTime.MinValue))
                                             ? Data.EndTime.ToString("u").TrimEnd('Z')
                                             : "0000-00-00 00:00:00",
                                     (AttackType Left, AttackType Right) => Left.EndTime.CompareTo(Right.EndTime)));
        AttackType.ColumnDefs.Add(
            "Duration",
            new AttackType.ColumnDef("Duration", DefaultVisible: false, "INT", "Duration",
                                     (AttackType Data) => Data.DurationS,
                                     (AttackType Data) => Data.Duration.TotalSeconds.ToString("0"),
                                     (AttackType Left, AttackType Right) =>
                                         Left.Duration.CompareTo(Right.Duration)));
        AttackType.ColumnDefs.Add(
            "Damage",
            new AttackType.ColumnDef("Damage", DefaultVisible: true, "BIGINT", "Damage",
                                     (AttackType Data) => Data.Damage.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Damage.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Damage.CompareTo(Right.Damage)));
        AttackType.ColumnDefs.Add(
            "EncDPS",
            new AttackType.ColumnDef("EncDPS", DefaultVisible: true, "DOUBLE", "EncDPS",
                                     (AttackType Data) => Data.EncDPS.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.EncDPS.ToString(usCulture),
                                     (AttackType Left, AttackType Right) => Left.EncDPS.CompareTo(Right.EncDPS)));
        AttackType.ColumnDefs.Add(
            "CharDPS",
            new AttackType.ColumnDef("CharDPS", DefaultVisible: false, "DOUBLE", "CharDPS",
                                     (AttackType Data) => Data.CharDPS.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.CharDPS.ToString(usCulture),
                                     (AttackType Left, AttackType Right) => Left.CharDPS.CompareTo(Right.CharDPS)));
        AttackType.ColumnDefs.Add(
            "DPS",
            new AttackType.ColumnDef("DPS", DefaultVisible: false, "DOUBLE", "DPS",
                                     (AttackType Data) => Data.DPS.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.DPS.ToString(usCulture),
                                     (AttackType Left, AttackType Right) => Left.DPS.CompareTo(Right.DPS)));
        AttackType.ColumnDefs.Add(
            "Average",
            new AttackType.ColumnDef("Average", DefaultVisible: true, "DOUBLE", "Average",
                                     (AttackType Data) => Data.Average.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.Average.ToString(usCulture),
                                     (AttackType Left, AttackType Right) => Left.Average.CompareTo(Right.Average)));
        AttackType.ColumnDefs.Add(
            "Median",
            new AttackType.ColumnDef("Median", DefaultVisible: true, "BIGINT", "Median",
                                     (AttackType Data) => Data.Median.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Median.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Median.CompareTo(Right.Median)));
        AttackType.ColumnDefs.Add(
            "MinHit",
            new AttackType.ColumnDef("MinHit", DefaultVisible: true, "BIGINT", "MinHit",
                                     (AttackType Data) => Data.MinHit.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.MinHit.ToString(),
                                     (AttackType Left, AttackType Right) => Left.MinHit.CompareTo(Right.MinHit)));
        AttackType.ColumnDefs.Add(
            "MaxHit",
            new AttackType.ColumnDef("MaxHit", DefaultVisible: true, "BIGINT", "MaxHit",
                                     (AttackType Data) => Data.MaxHit.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.MaxHit.ToString(),
                                     (AttackType Left, AttackType Right) => Left.MaxHit.CompareTo(Right.MaxHit)));
        AttackType.ColumnDefs.Add(
            "Resist",
            new AttackType.ColumnDef("Resist", DefaultVisible: true, "VARCHAR(64)", "Resist",
                                     (AttackType Data) => Data.Resist, (AttackType Data) => Data.Resist,
                                     (AttackType Left, AttackType Right) => Left.Resist.CompareTo(Right.Resist)));
        AttackType.ColumnDefs.Add(
            "Hits",
            new AttackType.ColumnDef("Hits", DefaultVisible: true, "INT", "Hits",
                                     (AttackType Data) => Data.Hits.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Hits.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Hits.CompareTo(Right.Hits)));
        AttackType.ColumnDefs.Add(
            "CritHits",
            new AttackType.ColumnDef("CritHits", DefaultVisible: false, "INT", "CritHits",
                                     (AttackType Data) => Data.CritHits.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.CritHits.ToString(),
                                     (AttackType Left, AttackType Right) =>
                                         Left.CritHits.CompareTo(Right.CritHits)));
        AttackType.ColumnDefs.Add(
            "Avoids",
            new AttackType.ColumnDef("Avoids", DefaultVisible: false, "INT", "Blocked",
                                     (AttackType Data) => Data.Blocked.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Blocked.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Blocked.CompareTo(Right.Blocked)));
        AttackType.ColumnDefs.Add(
            "Misses",
            new AttackType.ColumnDef("Misses", DefaultVisible: false, "INT", "Misses",
                                     (AttackType Data) => Data.Misses.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Misses.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Misses.CompareTo(Right.Misses)));
        AttackType.ColumnDefs.Add(
            "Swings",
            new AttackType.ColumnDef("Swings", DefaultVisible: true, "INT", "Swings",
                                     (AttackType Data) => Data.Swings.ToString((string?)GetIntCommas()),
                                     (AttackType Data) => Data.Swings.ToString(),
                                     (AttackType Left, AttackType Right) => Left.Swings.CompareTo(Right.Swings)));
        AttackType.ColumnDefs.Add(
            "ToHit",
            new AttackType.ColumnDef("ToHit", DefaultVisible: true, "FLOAT", "ToHit",
                                     (AttackType Data) => Data.ToHit.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.ToHit.ToString(usCulture),
                                     (AttackType Left, AttackType Right) => Left.ToHit.CompareTo(Right.ToHit)));
        AttackType.ColumnDefs.Add(
            "AvgDelay",
            new AttackType.ColumnDef("AvgDelay", DefaultVisible: false, "FLOAT", "AverageDelay",
                                     (AttackType Data) => Data.AverageDelay.ToString((string?)GetFloatCommas()),
                                     (AttackType Data) => Data.AverageDelay.ToString(usCulture),
                                     (AttackType Left, AttackType Right) =>
                                         Left.AverageDelay.CompareTo(Right.AverageDelay)));
        AttackType.ColumnDefs.Add(
            "Crit%",
            new AttackType.ColumnDef("Crit%", DefaultVisible: true, "VARCHAR(8)", "CritPerc",
                                     (AttackType Data) => Data.CritPerc.ToString("0'%"),
                                     (AttackType Data) => Data.CritPerc.ToString("0'%"),
                                     (AttackType Left, AttackType Right) =>
                                         Left.CritPerc.CompareTo(Right.CritPerc)));
        AttackType.ColumnDefs.Add("CritTypes",
                                  new AttackType.ColumnDef("CritTypes", DefaultVisible: true, "VARCHAR(32)",
                                                           "CritTypes", AttackTypeGetCriticalTypes,
                                                           AttackTypeGetCriticalTypes,
                                                           (AttackType Left, AttackType Right) =>
                                                               AttackTypeGetCriticalTypes(Left)
                                                                   .CompareTo(AttackTypeGetCriticalTypes(Right))));
        MasterSwing.ColumnDefs.Clear();
        MasterSwing.ColumnDefs.Add(
            "EncId",
            new MasterSwing.ColumnDef("EncId", DefaultVisible: false, "CHAR(8)", "EncId",
                                      (MasterSwing Data) => string.Empty,
                                      (MasterSwing Data) => Data.ParentEncounter.EncId,
                                      (MasterSwing Left, MasterSwing Right) => 0));
        MasterSwing.ColumnDefs.Add(
            "Time",
            new MasterSwing.ColumnDef("Time", DefaultVisible: true, "TIMESTAMP", "STime",
                                      (MasterSwing Data) => Data.Time.ToString("T"),
                                      (MasterSwing Data) => Data.Time.ToString("u").TrimEnd('Z'),
                                      (MasterSwing Left, MasterSwing Right) => Left.Time.CompareTo(Right.Time)));
        MasterSwing.ColumnDefs.Add("RelativeTime",
                                   new MasterSwing.ColumnDef("RelativeTime", DefaultVisible: true, "FLOAT",
                                                             "RelativeTime",
                                                             (MasterSwing Data) =>
                                                                 (Data.Time - Data.ParentEncounter.StartTime)
                                                                 .ToString("T"),
                                                             (MasterSwing Data) =>
                                                                 (Data.Time - Data.ParentEncounter.StartTime)
                                                                 .TotalSeconds.ToString(),
                                                             (MasterSwing Left, MasterSwing Right) =>
                                                                 Left.Time.CompareTo(Right.Time)));
        MasterSwing.ColumnDefs.Add(
            "Attacker",
            new MasterSwing.ColumnDef("Attacker", DefaultVisible: true, "VARCHAR(64)", "Attacker",
                                      (MasterSwing Data) => Data.Attacker, (MasterSwing Data) => Data.Attacker,
                                      (MasterSwing Left, MasterSwing Right) =>
                                          Left.Attacker.CompareTo(Right.Attacker)));
        MasterSwing.ColumnDefs.Add("SwingType",
                                   new MasterSwing.ColumnDef("SwingType", DefaultVisible: false, "TINYINT",
                                                             "SwingType",
                                                             (MasterSwing Data) => Data.SwingType.ToString(),
                                                             (MasterSwing Data) => Data.SwingType.ToString(),
                                                             (MasterSwing Left, MasterSwing Right) =>
                                                                 Left.SwingType.CompareTo(Right.SwingType)));
        MasterSwing.ColumnDefs.Add("AttackType",
                                   new MasterSwing.ColumnDef("AttackType", DefaultVisible: true, "VARCHAR(64)",
                                                             "AttackType", (MasterSwing Data) => Data.AttackType,
                                                             (MasterSwing Data) => Data.AttackType,
                                                             (MasterSwing Left, MasterSwing Right) =>
                                                                 Left.AttackType.CompareTo(Right.AttackType)));
        MasterSwing.ColumnDefs.Add("DamageType",
                                   new MasterSwing.ColumnDef("DamageType", DefaultVisible: true, "VARCHAR(64)",
                                                             "DamageType", (MasterSwing Data) => Data.DamageType,
                                                             (MasterSwing Data) => Data.DamageType,
                                                             (MasterSwing Left, MasterSwing Right) =>
                                                                 Left.DamageType.CompareTo(Right.DamageType)));
        MasterSwing.ColumnDefs.Add(
            "Victim",
            new MasterSwing.ColumnDef("Victim", DefaultVisible: true, "VARCHAR(64)", "Victim",
                                      (MasterSwing Data) => Data.Victim, (MasterSwing Data) => Data.Victim,
                                      (MasterSwing Left, MasterSwing Right) =>
                                          Left.Victim.CompareTo(Right.Victim)));
        MasterSwing.ColumnDefs.Add("DamageNum",
                                   new MasterSwing.ColumnDef("DamageNum", DefaultVisible: false, "BIGINT", "Damage",
                                                             (MasterSwing Data) => ((long)Data.Damage).ToString(),
                                                             (MasterSwing Data) => ((long)Data.Damage).ToString(),
                                                             (MasterSwing Left, MasterSwing Right) =>
                                                                 Left.Damage.CompareTo(Right.Damage)));
        MasterSwing.ColumnDefs.Add(
            "Damage",
            new MasterSwing.ColumnDef("Damage", DefaultVisible: true, "VARCHAR(128)", "DamageString",
                                      (MasterSwing Data) => Data.Damage.ToString(),
                                      (MasterSwing Data) => Data.Damage.ToString(),
                                      (MasterSwing Left, MasterSwing Right) =>
                                          Left.Damage.CompareTo(Right.Damage)));
        MasterSwing.ColumnDefs.Add(
            "Critical",
            new MasterSwing.ColumnDef("Critical", DefaultVisible: false, "CHAR(1)", "Critical",
                                      (MasterSwing Data) => Data.Critical.ToString(),
                                      (MasterSwing Data) => Data.Critical.ToString(usCulture)[0].ToString(),
                                      (MasterSwing Left, MasterSwing Right) =>
                                          Left.Critical.CompareTo(Right.Critical)));
        MasterSwing.ColumnDefs.Add("CriticalStr", new MasterSwing.ColumnDef(
                                       "CriticalStr", DefaultVisible: true, "VARCHAR(32)", "CriticalStr",
                                       (MasterSwing Data) =>
                                           Data.Tags.ContainsKey("CriticalStr")
                                               ? ((string)Data.Tags["CriticalStr"])
                                               : "None",
                                       (MasterSwing Data) =>
                                           Data.Tags.ContainsKey("CriticalStr")
                                               ? ((string)Data.Tags["CriticalStr"])
                                               : "None", delegate(MasterSwing Left, MasterSwing Right)
                                       {
                                           var obj = (Left.Tags.ContainsKey("CriticalStr")
                                                          ? ((string)Left.Tags["CriticalStr"])
                                                          : "None");
                                           var strB = (Right.Tags.ContainsKey("CriticalStr")
                                                           ? ((string)Right.Tags["CriticalStr"])
                                                           : "None");
                                           return obj.CompareTo(strB);
                                       }));
        MasterSwing.ColumnDefs.Add(
            "Special",
            new MasterSwing.ColumnDef("Special", DefaultVisible: true, "VARCHAR(64)", "Special",
                                      (MasterSwing Data) => Data.Special, (MasterSwing Data) => Data.Special,
                                      (MasterSwing Left, MasterSwing Right) =>
                                          Left.Special.CompareTo(Right.Special)));
        foreach (var columnDef in MasterSwing.ColumnDefs)
        {
            columnDef.Value.GetCellForeColor = (MasterSwing Data) => GetSwingTypeColor(Data.SwingType);
        }
    }

    private static string GetIntCommas() => !mainTableShowCommas ? "0" : "#,0";

    private static string GetFloatCommas() => !mainTableShowCommas ? "0.00" : "#,0.00";

    private static string CombatantDataGetCriticalTypes(CombatantData data) =>
        data.AllOut.TryGetValue(Trans["attackTypeTerm-all"], out var value)
            ? AttackTypeGetCriticalTypes(value)
            : "-";

    private static string AttackTypeGetCriticalTypes(AttackType attackType)
    {
        var totalCriticals = 0;
        var legendaryCriticals = 0;
        var fabledCriticals = 0;
        var mythicalCriticals = 0;

        foreach (var swing in attackType.Items)
        {
            if (!swing.Critical)
            {
                continue;
            }

            totalCriticals++;

            if (!swing.Tags.ContainsKey("CriticalStr"))
            {
                continue;
            }

            var criticalType = (string)swing.Tags["CriticalStr"];

            if (criticalType.Contains("Legendary"))
            {
                legendaryCriticals++;
            }
            else if (criticalType.Contains("Fabled"))
            {
                fabledCriticals++;
            }
            else if (criticalType.Contains("Mythical"))
            {
                mythicalCriticals++;
            }
        }

        var legendaryPercentage = totalCriticals == 0 ? 0 : (float)legendaryCriticals / totalCriticals * 100f;
        var fabledPercentage = totalCriticals == 0 ? 0 : (float)fabledCriticals / totalCriticals * 100f;
        var mythicalPercentage = totalCriticals == 0 ? 0 : (float)mythicalCriticals / totalCriticals * 100f;

        return totalCriticals == 0 ? "-" : $"{legendaryPercentage:0.0}% Legendary - {fabledPercentage:0.0}% Fabled - {mythicalPercentage:0.0}% Mythical";
    }


    private static string DamageTypeDataGetCriticalTypes(DamageTypeData data) =>
        data.Items.TryGetValue(Trans["attackTypeTerm-all"], out var value)
            ? AttackTypeGetCriticalTypes(value)
            : "-";

    private static string GetDamageTypeGrouping(DamageTypeData damageData)
    {
        string @params;
        var swingTypeCount = 0;
    
        if (damageData.Outgoing)
        {
            @params = $"attacker={damageData.Parent.Name}";

            foreach (var swingTypeLink in CombatantData.SwingTypeToDamageTypeDataLinksOutgoing)
            {
                foreach (var damageType in swingTypeLink.Value)
                {
                    if (damageData.Type == damageType)
                    {
                        @params += $"&swingtype{(swingTypeCount == 0 ? "" : swingTypeCount.ToString())}={swingTypeLink.Key}";
                        swingTypeCount++;
                    }
                }
            }
        }
        else
        {
            @params = $"victim={damageData.Parent.Name}";

            foreach (var swingTypeLink in CombatantData.SwingTypeToDamageTypeDataLinksIncoming)
            {
                foreach (var damageType in swingTypeLink.Value)
                {
                    if (damageData.Type == damageType)
                    {
                        @params += $"&swingtype{(swingTypeCount == 0 ? "" : swingTypeCount.ToString())}={swingTypeLink.Key}";
                        swingTypeCount++;
                    }
                }
            }
        }

        return @params;
    }


    private static string GetAttackTypeSwingType(AttackType attack)
    {
        var swingTypes = attack.Items.Select(swing => swing.SwingType).Distinct().ToList();
        return swingTypes.Count == 1 ? swingTypes[0].ToString() : "100";
    }


    private static Color GetSwingTypeColor(int swingType)
    {
        switch (swingType)
        {
            case 1:
            case 2:
                return Color.Crimson;
            case 3:
                return Color.Blue;
            case 4:
                return Color.DarkRed;
            case 5:
                return Color.DarkOrange;
            case 8:
                return Color.DarkOrchid;
            case 9:
                return Color.DodgerBlue;
            default:
                return Color.Black;
        }
    }

    private static string EncounterFormatSwitch(
        EncounterData data, List<CombatantData> selectiveAllies, string varName, string extra, int tries = 0)
    {
        try
        {
            var num = 0L;
            var num2 = 0L;
            var num3 = 0;
            var num4 = 0;
            var num5 = 0;
            var num6 = 0;
            var num7 = 0;
            var num8 = 0;
            var num9 = 0;
            var num10 = 0;
            var num11 = 0f;
            var num12 = 0.0;
            var num13 = 0.0;
            var num14 = 0L;
            var num15 = 0L;
            var num16 = 0L;
            var num17 = 0L;
            var num18 = 0;
            var num19 = 0;
            switch (varName)
            {
                case "maxheal":
                    return data.GetMaxHeal(ShowType: true, CountWards: false, UseSuffix: false);
                case "MAXHEAL":
                    return data.GetMaxHeal(ShowType: false, CountWards: false, UseSuffix: false);
                case "maxheal-*":
                    return data.GetMaxHeal(ShowType: true, CountWards: false);
                case "MAXHEAL-*":
                    return data.GetMaxHeal(ShowType: false, CountWards: false);
                case "maxhealward":
                    return data.GetMaxHeal(ShowType: true, CountWards: true, UseSuffix: false);
                case "MAXHEALWARD":
                    return data.GetMaxHeal(ShowType: false, CountWards: true, UseSuffix: false);
                case "maxhealward-*":
                    return data.GetMaxHeal();
                case "MAXHEALWARD-*":
                    return data.GetMaxHeal(ShowType: false);
                case "maxhit":
                    return data.GetMaxHit(ShowType: true, UseSuffix: false);
                case "MAXHIT":
                    return data.GetMaxHit(ShowType: false, UseSuffix: false);
                case "maxhit-*":
                    return data.GetMaxHit();
                case "MAXHIT-*":
                    return data.GetMaxHit(ShowType: false);
                case "duration":
                    if (data.Active)
                    {
                        if (oFormActMain.LastEstimatedTime > data.StartTime)
                        {
                            var timeSpan = oFormActMain.LastEstimatedTime - data.StartTime;
                            return timeSpan.Hours == 0
                                       ? $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}"
                                       : $"{timeSpan.Hours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
                        }
                        return "00:00";
                    }
                    return data.DurationS;
                case "DURATION":
                    if (data.Active)
                    {
                        if (oFormActMain.LastEstimatedTime > data.StartTime)
                        {
                            return ((int)(oFormActMain.LastEstimatedTime - data.StartTime).TotalSeconds)
                                .ToString("0");
                        }

                        return "0";
                    }
                    return data.Duration.TotalSeconds.ToString("0");
                case "damage":
                    foreach (var SelectiveAlly in selectiveAllies)
                    {
                        num += SelectiveAlly.Damage;
                    }

                    return num.ToString();
                case "damage-m":
                    foreach (var SelectiveAlly2 in selectiveAllies)
                    {
                        num += SelectiveAlly2.Damage;
                    }

                    return ((double)num / 1000000.0).ToString("0.00");
                case "damage-b":
                    foreach (var SelectiveAlly3 in selectiveAllies)
                    {
                        num += SelectiveAlly3.Damage;
                    }

                    return ((double)num / 1000000000.0).ToString("0.00");
                case "damage-*":
                    foreach (var SelectiveAlly4 in selectiveAllies)
                    {
                        num += SelectiveAlly4.Damage;
                    }

                    return oFormActMain.CreateDamageString(num, UseSuffix: true, UseDecimals: true);
                case "DAMAGE-k":
                    foreach (var SelectiveAlly5 in selectiveAllies)
                    {
                        num += SelectiveAlly5.Damage;
                    }

                    return ((double)num / 1000.0).ToString("0");
                case "DAMAGE-m":
                    foreach (var SelectiveAlly6 in selectiveAllies)
                    {
                        num += SelectiveAlly6.Damage;
                    }

                    return ((double)num / 1000000.0).ToString("0");
                case "DAMAGE-b":
                    foreach (var SelectiveAlly7 in selectiveAllies)
                    {
                        num += SelectiveAlly7.Damage;
                    }

                    return ((double)num / 1000000000.0).ToString("0");
                case "DAMAGE-*":
                    foreach (var SelectiveAlly8 in selectiveAllies)
                    {
                        num += SelectiveAlly8.Damage;
                    }

                    return oFormActMain.CreateDamageString(num, UseSuffix: true, UseDecimals: false);
                case "healed":
                    foreach (var SelectiveAlly9 in selectiveAllies)
                    {
                        num2 += SelectiveAlly9.Healed;
                    }

                    return num2.ToString();
                case "healed-*":
                    foreach (var SelectiveAlly10 in selectiveAllies)
                    {
                        num2 += SelectiveAlly10.Healed;
                    }

                    return oFormActMain.CreateDamageString(num2, UseSuffix: true, UseDecimals: true);
                case "swings":
                    foreach (var SelectiveAlly11 in selectiveAllies)
                    {
                        num3 += SelectiveAlly11.Swings;
                    }

                    return num3.ToString();
                case "hits":
                    foreach (var SelectiveAlly12 in selectiveAllies)
                    {
                        num4 += SelectiveAlly12.Hits;
                    }

                    return num4.ToString();
                case "crithits":
                    foreach (var SelectiveAlly13 in selectiveAllies)
                    {
                        num5 += SelectiveAlly13.CritHits;
                    }

                    return num5.ToString();
                case "crithit%":
                    foreach (var SelectiveAlly14 in selectiveAllies)
                    {
                        num5 += SelectiveAlly14.CritHits;
                    }

                    foreach (var SelectiveAlly15 in selectiveAllies)
                    {
                        num4 += SelectiveAlly15.Hits;
                    }

                    return ((float)num5 / (float)num4).ToString("0'%");
                case "heals":
                    foreach (var SelectiveAlly16 in selectiveAllies)
                    {
                        num6 += SelectiveAlly16.Heals;
                    }

                    return num6.ToString();
                case "critheals":
                    foreach (var SelectiveAlly17 in selectiveAllies)
                    {
                        num7 += SelectiveAlly17.CritHits;
                    }

                    return num7.ToString();
                case "critheal%":
                    foreach (var SelectiveAlly18 in selectiveAllies)
                    {
                        num7 += SelectiveAlly18.CritHeals;
                    }

                    foreach (var SelectiveAlly19 in selectiveAllies)
                    {
                        num6 += SelectiveAlly19.Heals;
                    }

                    return ((float)num7 / (float)num6).ToString("0'%");
                case "cures":
                    foreach (var SelectiveAlly20 in selectiveAllies)
                    {
                        num8 += SelectiveAlly20.CureDispels;
                    }

                    return num8.ToString();
                case "misses":
                    foreach (var SelectiveAlly21 in selectiveAllies)
                    {
                        num9 += SelectiveAlly21.Misses;
                    }

                    return num9.ToString();
                case "hitfailed":
                    foreach (var SelectiveAlly22 in selectiveAllies)
                    {
                        num10 += SelectiveAlly22.Blocked;
                    }

                    return num10.ToString();
                case "TOHIT":
                    foreach (var SelectiveAlly23 in selectiveAllies)
                    {
                        num11 += SelectiveAlly23.ToHit;
                    }

                    return (num11 / (float)selectiveAllies.Count).ToString("0");
                case "DPS":
                case "ENCDPS":
                    foreach (var SelectiveAlly24 in selectiveAllies)
                    {
                        num += SelectiveAlly24.Damage;
                    }

                    return ((double)num / data.Duration.TotalSeconds).ToString("0");
                case "DPS-*":
                case "ENCDPS-*":
                    foreach (var SelectiveAlly25 in selectiveAllies)
                    {
                        num += SelectiveAlly25.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return oFormActMain.CreateDamageString((long)num12, UseSuffix: true, UseDecimals: false);
                case "DPS-k":
                case "ENCDPS-k":
                    foreach (var SelectiveAlly26 in selectiveAllies)
                    {
                        num += SelectiveAlly26.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return (num12 / 1000.0).ToString("0");
                case "ENCDPS-m":
                    foreach (var SelectiveAlly27 in selectiveAllies)
                    {
                        num += SelectiveAlly27.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return (num12 / 1000000.0).ToString("0");
                case "ENCHPS":
                    foreach (var SelectiveAlly28 in selectiveAllies)
                    {
                        num2 += SelectiveAlly28.Healed;
                    }

                    return ((double)num2 / data.Duration.TotalSeconds).ToString("0");
                case "ENCHPS-k":
                    foreach (var SelectiveAlly29 in selectiveAllies)
                    {
                        num2 += SelectiveAlly29.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return (num13 / 1000.0).ToString("0");
                case "ENCHPS-m":
                    foreach (var SelectiveAlly30 in selectiveAllies)
                    {
                        num2 += SelectiveAlly30.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return (num13 / 1000000.0).ToString("0");
                case "ENCHPS-*":
                    foreach (var SelectiveAlly31 in selectiveAllies)
                    {
                        num2 += SelectiveAlly31.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return oFormActMain.CreateDamageString((long)num13, UseSuffix: true, UseDecimals: false);
                case "tohit":
                    foreach (var SelectiveAlly32 in selectiveAllies)
                    {
                        num11 += SelectiveAlly32.ToHit;
                    }

                    return (num11 / (float)selectiveAllies.Count).ToString("F");
                case "dps":
                case "encdps":
                    foreach (var SelectiveAlly33 in selectiveAllies)
                    {
                        num += SelectiveAlly33.Damage;
                    }

                    return ((double)num / data.Duration.TotalSeconds).ToString("F");
                case "dps-k":
                case "encdps-k":
                    foreach (var SelectiveAlly34 in selectiveAllies)
                    {
                        num += SelectiveAlly34.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return (num12 / 1000.0).ToString("F");
                case "encdps-m":
                    foreach (var SelectiveAlly35 in selectiveAllies)
                    {
                        num += SelectiveAlly35.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return (num12 / 1000000.0).ToString("F");
                case "encdps-*":
                    foreach (var SelectiveAlly36 in selectiveAllies)
                    {
                        num += SelectiveAlly36.Damage;
                    }

                    num12 = (double)num / data.Duration.TotalSeconds;
                    return oFormActMain.CreateDamageString((long)num12, UseSuffix: true, UseDecimals: true);
                case "enchps":
                    foreach (var SelectiveAlly37 in selectiveAllies)
                    {
                        num2 += SelectiveAlly37.Healed;
                    }

                    return ((double)num2 / data.Duration.TotalSeconds).ToString("F");
                case "enchps-k":
                    foreach (var SelectiveAlly38 in selectiveAllies)
                    {
                        num2 += SelectiveAlly38.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return (num13 / 1000.0).ToString("F");
                case "enchps-m":
                    foreach (var SelectiveAlly39 in selectiveAllies)
                    {
                        num2 += SelectiveAlly39.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return (num13 / 1000000.0).ToString("F");
                case "enchps-*":
                    foreach (var SelectiveAlly40 in selectiveAllies)
                    {
                        num2 += SelectiveAlly40.Healed;
                    }

                    num13 = (double)num2 / data.Duration.TotalSeconds;
                    return oFormActMain.CreateDamageString((long)num13, UseSuffix: true, UseDecimals: true);
                case "healstaken":
                    foreach (var SelectiveAlly41 in selectiveAllies)
                    {
                        num14 += SelectiveAlly41.HealsTaken;
                    }

                    return num14.ToString();
                case "healstaken-*":
                    foreach (var SelectiveAlly42 in selectiveAllies)
                    {
                        num14 += SelectiveAlly42.HealsTaken;
                    }

                    return oFormActMain.CreateDamageString(num14, UseSuffix: true, UseDecimals: true);
                case "damagetaken":
                    foreach (var SelectiveAlly43 in selectiveAllies)
                    {
                        num15 += SelectiveAlly43.DamageTaken;
                    }

                    return num15.ToString();
                case "damagetaken-*":
                    foreach (var SelectiveAlly44 in selectiveAllies)
                    {
                        num15 += SelectiveAlly44.DamageTaken;
                    }

                    return oFormActMain.CreateDamageString(num15, UseSuffix: true, UseDecimals: true);
                case "powerdrain":
                    foreach (var SelectiveAlly45 in selectiveAllies)
                    {
                        num16 += SelectiveAlly45.PowerDamage;
                    }

                    return num16.ToString();
                case "powerdrain-*":
                    foreach (var SelectiveAlly46 in selectiveAllies)
                    {
                        num16 += SelectiveAlly46.PowerDamage;
                    }

                    return oFormActMain.CreateDamageString(num16, UseSuffix: true, UseDecimals: true);
                case "powerheal":
                    foreach (var SelectiveAlly47 in selectiveAllies)
                    {
                        num17 += SelectiveAlly47.PowerReplenish;
                    }

                    return num17.ToString();
                case "powerheal-*":
                    foreach (var SelectiveAlly48 in selectiveAllies)
                    {
                        num17 += SelectiveAlly48.PowerReplenish;
                    }

                    return oFormActMain.CreateDamageString(num17, UseSuffix: true, UseDecimals: true);
                case "kills":
                    foreach (var SelectiveAlly49 in selectiveAllies)
                    {
                        num18 += SelectiveAlly49.Kills;
                    }

                    return num18.ToString();
                case "deaths":
                    foreach (var SelectiveAlly50 in selectiveAllies)
                    {
                        num19 += SelectiveAlly50.Deaths;
                    }

                    return num19.ToString();
                case "title":
                    return data.Title;
                default:
                    return varName;
            }
        }
        catch (Exception ex)
        {
            if (tries < 3)
                return EncounterFormatSwitch(data, selectiveAllies, varName, extra, tries + 1);
            
            if (ex is not InvalidOperationException)
                oFormActMain.WriteExceptionLog(ex, $"{data} -> {varName}({extra})");
            else 
                oFormActMain.PluginLog.Verbose(ex, $"[NotAct] FFIXV_ACT_Plugin modified collection needed for {data} -> {varName}({extra})");
            
            return "ERROR";
        }
    }

    private static string CombatantFormatSwitch(CombatantData data, string varName, string extra, int tries = 0)
    {
        try
        {
            var num = 0;
            switch (varName)
            {
                case "name":
                    return data.Name;
                case "NAME":
                    num = int.Parse(extra);
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME3":
                    num = 3;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME4":
                    num = 4;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME5":
                    num = 5;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME6":
                    num = 6;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME7":
                    num = 7;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME8":
                    num = 8;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME9":
                    num = 9;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME10":
                    num = 10;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME11":
                    num = 11;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME12":
                    num = 12;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME13":
                    num = 13;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME14":
                    num = 14;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "NAME15":
                    num = 15;
                    return (data.Name.Length - num > 0)
                               ? data.Name.Remove(num, data.Name.Length - num).Trim()
                               : data.Name;
                case "DURATION":
                    return data.Duration.TotalSeconds.ToString("0");
                case "duration":
                    return data.DurationS;
                case "maxhit":
                    return data.GetMaxHit(ShowType: true, UseSuffix: false);
                case "MAXHIT":
                    return data.GetMaxHit(ShowType: false, UseSuffix: false);
                case "maxhit-*":
                    return data.GetMaxHit();
                case "MAXHIT-*":
                    return data.GetMaxHit(ShowType: false);
                case "maxheal":
                    return data.GetMaxHeal(ShowType: true, CountWards: false, UseSuffix: false);
                case "MAXHEAL":
                    return data.GetMaxHeal(ShowType: false, CountWards: false, UseSuffix: false);
                case "maxheal-*":
                    return data.GetMaxHeal();
                case "MAXHEAL-*":
                    return data.GetMaxHeal(ShowType: false);
                case "maxhealward":
                    return data.GetMaxHeal(ShowType: true, CountWards: true, UseSuffix: false);
                case "MAXHEALWARD":
                    return data.GetMaxHeal(ShowType: false, CountWards: true, UseSuffix: false);
                case "maxhealward-*":
                    return data.GetMaxHeal(ShowType: true, CountWards: true);
                case "MAXHEALWARD-*":
                    return data.GetMaxHeal(ShowType: false, CountWards: true);
                case "damage":
                    return data.Damage.ToString();
                case "damage-k":
                    return ((double)data.Damage / 1000.0).ToString("0.00");
                case "damage-m":
                    return ((double)data.Damage / 1000000.0).ToString("0.00");
                case "damage-b":
                    return ((double)data.Damage / 1000000000.0).ToString("0.00");
                case "damage-*":
                    return oFormActMain.CreateDamageString(data.Damage, UseSuffix: true, UseDecimals: true);
                case "DAMAGE-k":
                    return ((double)data.Damage / 1000.0).ToString("0");
                case "DAMAGE-m":
                    return ((double)data.Damage / 1000000.0).ToString("0");
                case "DAMAGE-b":
                    return ((double)data.Damage / 1000000000.0).ToString("0");
                case "DAMAGE-*":
                    return oFormActMain.CreateDamageString(data.Damage, UseSuffix: true, UseDecimals: false);
                case "healed":
                    return data.Healed.ToString();
                case "healed-*":
                    return oFormActMain.CreateDamageString(data.Healed, UseSuffix: true, UseDecimals: true);
                case "swings":
                    return data.Swings.ToString();
                case "hits":
                    return data.Hits.ToString();
                case "crithits":
                    return data.CritHits.ToString();
                case "critheals":
                    return data.CritHeals.ToString();
                case "crittypes":
                    return CombatantDataGetCriticalTypes(data);
                case "crithit%":
                    return data.CritDamPerc.ToString("0'%");
                case "critheal%":
                    return data.CritHealPerc.ToString("0'%");
                case "heals":
                    return data.Heals.ToString();
                case "cures":
                    return data.CureDispels.ToString();
                case "misses":
                    return data.Misses.ToString();
                case "hitfailed":
                    return data.Blocked.ToString();
                case "TOHIT":
                    return data.ToHit.ToString("0");
                case "DPS":
                    return data.DPS.ToString("0");
                case "DPS-k":
                    return (data.DPS / 1000.0).ToString("0");
                case "DPS-m":
                    return (data.DPS / 1000000.0).ToString("0");
                case "DPS-*":
                    return oFormActMain.CreateDamageString((long)data.DPS, UseSuffix: true, UseDecimals: false);
                case "ENCDPS":
                    return data.EncDPS.ToString("0");
                case "ENCDPS-k":
                    return (data.EncDPS / 1000.0).ToString("0");
                case "ENCDPS-m":
                    return (data.EncDPS / 1000000.0).ToString("0");
                case "ENCDPS-*":
                    return oFormActMain.CreateDamageString((long)data.EncDPS, UseSuffix: true, UseDecimals: false);
                case "ENCHPS":
                    return data.EncHPS.ToString("0");
                case "ENCHPS-k":
                    return (data.EncHPS / 1000.0).ToString("0");
                case "ENCHPS-m":
                    return (data.EncHPS / 1000000.0).ToString("0");
                case "ENCHPS-*":
                    return oFormActMain.CreateDamageString((long)data.EncHPS, UseSuffix: true, UseDecimals: false);
                case "tohit":
                    return data.ToHit.ToString("F");
                case "dps":
                    return data.DPS.ToString("F");
                case "dps-k":
                    return (data.DPS / 1000.0).ToString("F");
                case "dps-*":
                    return oFormActMain.CreateDamageString((long)data.DPS, UseSuffix: true, UseDecimals: true);
                case "encdps":
                    return data.EncDPS.ToString("F");
                case "encdps-k":
                    return (data.EncDPS / 1000.0).ToString("F");
                case "encdps-m":
                    return (data.EncDPS / 1000000.0).ToString("F");
                case "encdps-*":
                    return oFormActMain.CreateDamageString((long)data.EncDPS, UseSuffix: true, UseDecimals: true);
                case "enchps":
                    return data.EncHPS.ToString("F");
                case "enchps-k":
                    return (data.EncHPS / 1000.0).ToString("F");
                case "enchps-m":
                    return (data.EncHPS / 1000000.0).ToString("F");
                case "enchps-*":
                    return oFormActMain.CreateDamageString((long)data.EncHPS, UseSuffix: true, UseDecimals: true);
                case "healstaken":
                    return data.HealsTaken.ToString();
                case "healstaken-*":
                    return oFormActMain.CreateDamageString(data.HealsTaken, UseSuffix: true, UseDecimals: true);
                case "damagetaken":
                    return data.DamageTaken.ToString();
                case "damagetaken-*":
                    return oFormActMain.CreateDamageString(data.DamageTaken, UseSuffix: true, UseDecimals: true);
                case "powerdrain":
                    return data.PowerDamage.ToString();
                case "powerdrain-*":
                    return oFormActMain.CreateDamageString(data.PowerDamage, UseSuffix: true, UseDecimals: true);
                case "powerheal":
                    return data.PowerReplenish.ToString();
                case "powerheal-*":
                    return oFormActMain.CreateDamageString(data.PowerReplenish, UseSuffix: true, UseDecimals: true);
                case "kills":
                    return data.Kills.ToString();
                case "deaths":
                    return data.Deaths.ToString();
                case "damage%":
                    return data.DamagePercent;
                case "healed%":
                    return data.HealedPercent;
                case "threatstr":
                    return data.GetThreatStr("Threat (Out)");
                case "threatdelta":
                    return data.GetThreatDelta("Threat (Out)").ToString();
                case "n":
                    return "\n";
                case "t":
                    return "\t";
                default:
                    return varName;
            }
        }
        catch (Exception ex)
        {
            if (tries < 3)
                return CombatantFormatSwitch(data, varName, extra, tries + 1);
            
            if (ex is not InvalidOperationException)
                oFormActMain.WriteExceptionLog(ex, $"{data} -> {varName}({extra})");
            else 
                oFormActMain.PluginLog.Verbose(ex, $"[NotAct] FFIXV_ACT_Plugin modified collection needed for {data} -> {varName}({extra})");
            
            return "ERROR";
        }
    }
}
