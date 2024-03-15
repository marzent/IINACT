using System;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings
{
    class LineContentFinderSettings
    {
        public const uint LogFileLineID = 265;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        private IContentFinderSettingsMemory contentFinderSettingsMemory;

        public LineContentFinderSettings(TinyIoCContainer container)
        {
            ffxiv = container.Resolve<FFXIVRepository>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            contentFinderSettingsMemory = container.Resolve<IContentFinderSettingsMemory>();
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ContentFinderSettings",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });

            ffxiv.RegisterZoneChangeDelegate(OnZoneChange);

            // Theoretically we should be able to check `ffxiv.GetCurrentTerritoryID()` for a value and log it here.
            // However, this returns `0`, whether checking before or after registering the zone change delegate
            // and the zone change delegate doesn't get called if the game's already running when starting ACT

            // Instead, use a janky workaround here. Register a log line listener and then once we write our first line, unregister it.

            ActGlobals.oFormActMain.BeforeLogLineRead += LogLineHandler;
        }
        private void LogLineHandler(bool isImport, LogLineEventArgs args)
        {
            if (!contentFinderSettingsMemory.IsValid())
                return;

            var currentZoneId = ffxiv.GetCurrentTerritoryID();
            if (currentZoneId.HasValue && currentZoneId.Value > 0)
            {
                var currentZoneName = ActGlobals.oFormActMain.CurrentZone;
                WriteInContentFinderSettingsLine(args.detectedTime, $"{currentZoneId.Value:X4}", currentZoneName);
                ActGlobals.oFormActMain.BeforeLogLineRead -= LogLineHandler;
            }
        }

        private void OnZoneChange(uint zoneId, string zoneName)
        {
            if (!contentFinderSettingsMemory.IsValid())
                return;
            WriteInContentFinderSettingsLine(DateTime.Now, $"{zoneId:X}", zoneName);
        }

        private void WriteInContentFinderSettingsLine(DateTime dateTime, string zoneID, string zoneName)
        {
            var settings = contentFinderSettingsMemory.GetContentFinderSettings();

            logWriter.Invoke(
                $"{zoneID}|" +
                $"{zoneName}|" +
                $"{settings.inContentFinderContent}|" +
                $"{settings.unrestrictedParty}|" +
                $"{settings.minimalItemLevel}|" +
                $"{settings.silenceEcho}|" +
                $"{settings.explorerMode}|" +
                $"{settings.levelSync}",
                dateTime);
        }
    }
}
