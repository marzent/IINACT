using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings
{
    interface IContentFinderSettingsMemory71 : IContentFinderSettingsMemory { }

    class ContentFinderSettingsMemory71 : ContentFinderSettingsMemory, IContentFinderSettingsMemory71
    {
        // FUN_14184cea0:14184ceaa, DAT_142780dd0
        private const string settingsSignature = "488D0D????????E8????????488BF84885C00F84????????488B4B??4889AC24";

        // FUN_1400aa840:1400aa862
        // IsLocalPlayerInParty:1400aa862 (after rename)
        private const string inContentFinderSignature = "803D??????????74??E8????????488BC8";

        public ContentFinderSettingsMemory71(TinyIoCContainer container)
            : base(container, settingsSignature, inContentFinderSignature, -1)
        { }

        // For 7.0 and onwards, handle this properly.
        // TODO: Once CN and KR are on 7.0, move this logic up to `ContentFinderSettings` for common use
        public override void ScanPointers()
        {
            ResetPointers();
            if (!memory.IsValid())
                return;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(settingsSignature, -29, true);
            if (list != null && list.Count > 0)
            {
                settingsAddress = list[0] + 0xA8;
            }
            else
            {
                settingsAddress = IntPtr.Zero;
                fail.Add(nameof(settingsAddress));
            }

            logger.Log(LogLevel.Debug, "settingsAddress: 0x{0:X}", settingsAddress.ToInt64());

            list = memory.SigScan(inContentFinderSignature, -15, true, 1);
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

        public override Version GetVersion()
        {
            return new Version(7, 1);
        }
    }
}
