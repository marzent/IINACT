using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineNpcYell : LineBaseCustom<
            Server_MessageHeader_Global, LineNpcYell.NpcYell_v655,
            Server_MessageHeader_CN, LineNpcYell.NpcYell_v655,
            Server_MessageHeader_KR, LineNpcYell.NpcYell_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct NpcYell_v655 : IPacketStruct
        {
            // 00|2024-02-22T22:35:03.0000000-05:00|0044|Shanoa|Meow!♪|1d173e4a0eacfd95
            // 6.5.5 packet data (minus header):
            // 8A6B0140 00000000 0624 0000 D624 00000000 00000000 00000000 00000000 0000
            // AAAAAAAA BBBBBBBB CCCC DDDD EEEE FFFFFFFF GGGGGGGG HHHHHHHH IIIIIIII JJJJ
            // 0x0      0x4      0x8  0xA  0xC
            // Actor ID          NameID    YellID

            public const int structSize = 32;
            [FieldOffset(0x0)]
            public uint actorID;
            [FieldOffset(0x8)]
            public ushort nameID;
            [FieldOffset(0xC)]
            public ushort yellID;

            public string ToString(long epoch, uint ActorID)
            {
                return
                    $"{actorID:X8}|" +
                    $"{nameID:X4}|" +
                    $"{yellID:X4}";
            }
        }

        public const uint LogFileLineID = 266;
        public const string logLineName = "NpcYell";
        public const string MachinaPacketName = "NpcYell";

        public LineNpcYell(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
