using System.Globalization;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineActorMove : LineBaseCustom<
            Server_MessageHeader_Global, LineActorMove.ActorMove_v655,
            Server_MessageHeader_CN, LineActorMove.ActorMove_v655,
            Server_MessageHeader_KR, LineActorMove.ActorMove_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct ActorMove_v655 : IPacketStruct
        {
            // 6.5.5 packet data (minus header):
            // 1897 3004 3C00 B681 AA80 9F83 00000000
            // AAAA BBBB CCCC DDDD EEEE FFFF GGGGGGGG
            // 0x0  0x2  0x4  0x6  0x8  0xA  0xC
            // Rot  Unk  Unk  X    Y    Z    Unk

            // Have never seen data in 0xC, probably padding?

            public const int structSize = 16;

            [FieldOffset(0x0)]
            public ushort rotation;

            [FieldOffset(0x2)]
            public ushort unknown1;
            [FieldOffset(0x4)]
            public ushort unknown2;

            [FieldOffset(0x6)]
            public ushort x;
            [FieldOffset(0x8)]
            public ushort y;
            [FieldOffset(0xA)]
            public ushort z;

            public string ToString(long epoch, uint ActorID)
            {
                // Only emit for non-player actors
                if (ActorID < 0x40000000)
                {
                    return null;
                }

                return
                    string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:F4}|{2:X4}|{3:X4}|{4:F4}|{5:F4}|{6:F4}",
                        ActorID, FFXIVRepository.ConvertHeading(rotation), unknown1, unknown2,
                        FFXIVRepository.ConvertUInt16Coordinate(x), FFXIVRepository.ConvertUInt16Coordinate(z),
                        FFXIVRepository.ConvertUInt16Coordinate(y));
            }
        }

        public const uint LogFileLineID = 270;
        public const string logLineName = "ActorMove";
        public const string MachinaPacketName = "ActorMove";

        public LineActorMove(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
