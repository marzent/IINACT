using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings
{
    public abstract class ContentFinderSettingsMemory : IContentFinderSettingsMemory
    {
        private struct ContentFinderSettingsImpl : ContentFinderSettings
        {
            public bool inContentFinderContent { get; set; }

            public byte unrestrictedParty { get; set; }

            public byte minimalItemLevel { get; set; }

            public byte silenceEcho { get; set; }

            public byte explorerMode { get; set; }

            public byte levelSync { get; set; }
        }

        protected FFXIVMemory memory;
        protected ILogger logger;

        protected IntPtr settingsAddress = IntPtr.Zero;
        protected IntPtr inContentFinderAddress = IntPtr.Zero;

        private string settingsSignature;
        private string inContentFinderSignature;
        private int inContentSettingsOffset;

        public ContentFinderSettingsMemory(TinyIoCContainer container, string settingsSignature, string inContentFinderSignature, int inContentSettingsOffset)
        {
            this.settingsSignature = settingsSignature;
            this.inContentFinderSignature = inContentFinderSignature;
            this.inContentSettingsOffset = inContentSettingsOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        protected void ResetPointers()
        {
            settingsAddress = IntPtr.Zero;
            inContentFinderAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (settingsAddress == IntPtr.Zero)
                return false;
            if (inContentFinderAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                return false;

            return true;
        }

        public virtual void ScanPointers()
        {
            ResetPointers();
            if (!memory.IsValid())
                return;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(settingsSignature, -35, true);
            if (list != null && list.Count > 0)
            {
                settingsAddress = list[0] + inContentSettingsOffset;
            }
            else
            {
                settingsAddress = IntPtr.Zero;
                fail.Add(nameof(settingsAddress));
            }

            logger.Log(LogLevel.Debug, "settingsAddress: 0x{0:X}", settingsAddress.ToInt64());

            list = memory.SigScan(inContentFinderSignature, -34, true, 1);
            if (list != null && list.Count > 0)
            {
                inContentFinderAddress = list[0];
            }
            else
            {
                inContentFinderAddress = IntPtr.Zero;
                fail.Add(nameof(inContentFinderAddress));
            }

            logger.Log(LogLevel.Debug, "inContentFinderAddress: 0x{0:X}", inContentFinderAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found content finder settings memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find content finder settings memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        private bool GetInContentFinderContent()
        {
            var bytes = memory.GetByteArray(inContentFinderAddress, 1);
            return bytes[0] != 0;
        }

        public ContentFinderSettings GetContentFinderSettings()
        {
            var settings = new ContentFinderSettingsImpl();
            settings.inContentFinderContent = GetInContentFinderContent();

            // Don't bother fetching other info if we're not in a valid ContentFinder scope
            if (!settings.inContentFinderContent)
            {
                return settings;
            }

            var bytes = memory.GetByteArray(settingsAddress, 5);
            settings.unrestrictedParty = bytes[0];
            settings.minimalItemLevel = bytes[1];
            settings.levelSync = bytes[2];
            settings.silenceEcho = bytes[3];
            settings.explorerMode = bytes[4];

            return settings;
        }
    }
}
