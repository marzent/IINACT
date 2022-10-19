namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    interface ITargetMemory60 : ITargetMemory { }

    class TargetMemory60 : TargetMemory, ITargetMemory60
    {
        private const string targetSignature = "83E901740832C04883C4205BC3488D0D";

        // Offsets from the targetAddress to find the correct target type.
        private const int targetTargetOffset = 176;
        private const int focusTargetOffset = 248;
        private const int hoverTargetOffset = 208;

        public TargetMemory60(TinyIoCContainer container)
            : base(container, targetSignature, targetTargetOffset, focusTargetOffset, hoverTargetOffset)
        { }
    }
}
