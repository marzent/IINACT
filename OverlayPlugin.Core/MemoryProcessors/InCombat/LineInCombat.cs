using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat
{
    public class LineInCombat
    {
        public const uint LogFileLineID = 260;
        private ILogger logger;
        private readonly FFXIVRepository ffxiv;
        private IInCombatMemory inCombatMemory;
        private InCombatArgs lastEventArgs;

        private Func<string, DateTime, bool> logWriter;

        public event EventHandler<InCombatArgs> OnInCombatChanged;

        public class InCombatArgs
        {
            public bool InACTCombat { get; private set; }
            public bool InGameCombat { get; private set; }
            public bool InGameCombatChanged { get; private set; }
            public InCombatArgs(bool inACTCombat, bool inGameCombat, bool inGameCombatChanged)
            {
                this.InACTCombat = inACTCombat;
                this.InGameCombat = inGameCombat;
                this.InGameCombatChanged = inGameCombatChanged;
            }
        }

        public LineInCombat(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            inCombatMemory = container.Resolve<IInCombatMemory>();
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "InCombat",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        public void Update()
        {
            if (!inCombatMemory.IsValid())
                return;

            bool inACTCombat = Advanced_Combat_Tracker.ActGlobals.oFormActMain.InCombat;
            bool inGameCombat = inCombatMemory.GetInCombat();

            if (lastEventArgs != null && lastEventArgs.InACTCombat == inACTCombat && lastEventArgs.InGameCombat == inGameCombat)
            {
                return;
            }

            // TODO: backwards-compatible logic here is to treat the starting value of inCombat as false,
            // and to not always set inGameCombatChanged=true for the first event.  Some parts of OverlayPlugin
            // (e.g. OverlayHider) only care about when inCombat changes, but probably we should consider to
            // always set this to true if lastEventArgs == null so that if ACT is started while out of combat,
            // the overlays are hidden/shown appropriately.
            bool inGameCombatChanged = lastEventArgs == null ? inGameCombat : lastEventArgs.InGameCombat != inGameCombat;
            lastEventArgs = new InCombatArgs(inACTCombat, inGameCombat, inGameCombatChanged);
            WriteLine(inACTCombat, inGameCombat);
            OnInCombatChanged?.Invoke(this, lastEventArgs);
        }

        public void WriteLine(bool inACTCombat, bool inGameCombat)
        {
            var inACTCombatDecimal = inACTCombat ? 1 : 0;
            var inGameCombatDecimal = inGameCombat ? 1 : 0;
            var line = $"{inACTCombatDecimal}|{inGameCombatDecimal}";
            logWriter(line, ffxiv.GetServerTimestamp());
        }
    }
}
