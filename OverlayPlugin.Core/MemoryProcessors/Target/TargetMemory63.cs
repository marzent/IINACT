namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    interface ITargetMemory63 : ITargetMemory { }

    class TargetMemory63 : TargetMemory, ITargetMemory63
    {
        private const string targetSignature = "483BC3750832C04883C4205BC3488D0D";

        // Offsets from the targetAddress to find the correct target type.
        private const int targetTargetOffset = 176;
        private const int focusTargetOffset = 248;
        private const int hoverTargetOffset = 208;

        public TargetMemory63(TinyIoCContainer container)
            : base(container, targetSignature, targetTargetOffset, focusTargetOffset, hoverTargetOffset)
        { }
    }
}
