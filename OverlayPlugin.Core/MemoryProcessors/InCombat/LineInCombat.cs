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

            // For the written log lines, consider changed=true for the first event.
            // This is different than the internal OnInCombatChanged event below.
            // Ideally we should refactor OverlayHider to not care about this to simplify this code.
            //
            // Add two boolean variables into the logline to indicate exactly which part was changed.
            // Useful if a plugin only cares about the change of in-game or ACT combat state to trigger other events.
            bool isACTChanged = lastEventArgs == null ? true : lastEventArgs.InACTCombat != inACTCombat;
            bool isGameChanged = lastEventArgs == null ? true : lastEventArgs.InGameCombat != inGameCombat;
            WriteLine(inACTCombat, inGameCombat, isACTChanged, isGameChanged);

            // TODO: backwards-compatible logic here for the internal OnInCombatChanged event args is to
            // treat the starting value of inCombat as false, and to not always set inGameCombatChanged=true for the first event.
            // Some parts of OP (e.g. OverlayHider) only care about when inCombat changes, but probably we should consider to
            // always set this to true if lastEventArgs == null so that if ACT is started while out of combat,
            // the overlays are hidden/shown appropriately.
            bool inGameCombatChanged = lastEventArgs == null ? inGameCombat : lastEventArgs.InGameCombat != inGameCombat;
            lastEventArgs = new InCombatArgs(inACTCombat, inGameCombat, inGameCombatChanged);
            OnInCombatChanged?.Invoke(this, lastEventArgs);
        }

        public void WriteLine(bool inACTCombat, bool inGameCombat, bool isACTChanged, bool isGameChanged)
        {
            var inACTCombatDecimal = inACTCombat ? 1 : 0;
            var inGameCombatDecimal = inGameCombat ? 1 : 0;
            var isACTChangedDecimal = isACTChanged ? 1 : 0;
            var isGameChangedDecimal = isGameChanged ? 1 : 0;
            var line = $"{inACTCombatDecimal}|{inGameCombatDecimal}|{isACTChangedDecimal}|{isGameChangedDecimal}";
            logWriter(line, ffxiv.GetServerTimestamp());
        }
    }
}
