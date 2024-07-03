using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    interface ITargetMemory70 : ITargetMemory { }

    class TargetMemory70 : TargetMemory, ITargetMemory70
    {
        private const string targetSignature = "E8????????483BC374??488D0D";

        // Offsets from the targetAddress to find the correct target type.
        private const int targetTargetOffset = 176;
        private const int focusTargetOffset = 248;
        private const int hoverTargetOffset = 208;

        public TargetMemory70(TinyIoCContainer container)
            : base(container, targetSignature, targetTargetOffset, focusTargetOffset, hoverTargetOffset)
        { }

        public override Version GetVersion()
        {
            return new Version(7, 0);
        }
    }
}
