using System.Globalization;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineActorSetPos : LineBaseCustom<
            Server_MessageHeader_Global, LineActorSetPos.ActorSetPos_v655,
            Server_MessageHeader_CN, LineActorSetPos.ActorSetPos_v655,
            Server_MessageHeader_KR, LineActorSetPos.ActorSetPos_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct ActorSetPos_v655 : IPacketStruct
        {
            // 6.5.5 packet data (minus header):
            // 6AD3 0F  02  00000000 233E3BC1 00000000 D06AF840 00000000
            // AAAA BB  CC  DDDDDDDD EEEEEEEE FFFFFFFF GGGGGGGG HHHHHHHH
            // 0x0  0x2 0x3 0x4      0x8      0xC      0x10     0x14
            // Rot  unk unk unk      X        Y        Z        unk

            // Have never seen data in 0x4 or 0x14, probably just padding?

            public const int structSize = 24;
            [FieldOffset(0x0)]
            public ushort rotation;

            [FieldOffset(0x2)]
            public byte unknown1;

            [FieldOffset(0x3)]
            public byte unknown2;

            // Yes, these are actually floats, and not some janky ushort that needs converted through ConvertUInt16Coordinate
            [FieldOffset(0x8)]
            public float x;
            [FieldOffset(0xC)]
            public float y;
            [FieldOffset(0x10)]
            public float z;

            public string ToString(long epoch, uint ActorID)
            {
                return
                    string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:F4}|{2:X2}|{3:X2}|{4:F4}|{5:F4}|{6:F4}",
                        ActorID, FFXIVRepository.ConvertHeading(rotation), unknown1, unknown2, x, z, y);
            }
        }

        public const uint LogFileLineID = 271;
        public const string logLineName = "ActorSetPos";
        public const string MachinaPacketName = "ActorSetPos";

        public LineActorSetPos(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
