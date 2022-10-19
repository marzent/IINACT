using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.EnmityHud
{
    [Serializable]
    public class EnmityHudEntry
    {
        public int Order;
        public uint ID;
        public uint HPPercent;
        public uint EnmityPercent;
        public uint CastPercent;
    }

}
