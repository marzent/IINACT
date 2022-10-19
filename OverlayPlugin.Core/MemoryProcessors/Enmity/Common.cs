using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity
{
    [Serializable]
    public class EnmityEntry
    {
        public uint ID;
        public uint OwnerID;
        public string Name;
        public uint Enmity;
        public bool isMe;
        public int HateRate;
        public byte Job;
    }
}
