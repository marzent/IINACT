using System;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineRSV : LineBaseCustom<
            Server_MessageHeader_Global, LineRSV.RSV_v62,
            Server_MessageHeader_CN, LineRSV.RSV_v62,
            Server_MessageHeader_KR, LineRSV.RSV_v62>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct RSV_v62 : IPacketStruct
        {
            public const int structSize = 1080;
            public const int keySize = 0x30;
            public const int valueSize = 0x404;
            [FieldOffset(0x0)]
            public int valueByteCount;
            [FieldOffset(0x4)]
            public fixed byte key[keySize];
            [FieldOffset(0x34)]
            public fixed byte value[valueSize];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (byte* key = this.key) fixed (byte* value = this.value)
                {
                    int valSize = Math.Min(valueByteCount, valueSize);
                    return
                        $"{ffxiv.GetLocaleString()}|" +
                        $"{valueByteCount:X8}|" +
                        $"{FFXIVMemory.GetStringFromBytes(key, keySize).Replace("\r", "\\r").Replace("\n", "\\n")}|" +
                        $"{FFXIVMemory.GetStringFromBytes(value, valSize, valSize).Replace("\r", "\\r").Replace("\n", "\\n")}";
                }
            }
        }

        public const uint LogFileLineID = 262;
        public const string logLineName = "RSVData";
        public const string MachinaPacketName = "RSVData";

        public LineRSV(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
