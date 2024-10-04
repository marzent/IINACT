using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineCountdownCancel : LineBaseCustom<
            Server_MessageHeader_Global, LineCountdownCancel.CountdownCancel_v655,
            Server_MessageHeader_CN, LineCountdownCancel.CountdownCancel_v655,
            Server_MessageHeader_KR, LineCountdownCancel.CountdownCancel_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct CountdownCancel_v655 : IPacketStruct
        {
            // 6.5.5 packet data (minus header):
            // 34120010 4F00 0000 0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20
            // AAAAAAAA BBBB CCCC DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD
            // 0x0      0x4  0x6  0x8
            // Actor ID Wrld Unk  Name

            public const int structSize = 40;
            [FieldOffset(0x0)]
            public uint countdownCancellerActorID;
            [FieldOffset(0x4)]
            public ushort countdownCancellerWorldId;

            [FieldOffset(0x8)]
            public fixed byte countdownCancellerName[32];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (byte* name = countdownCancellerName)
                {
                    return
                        $"{countdownCancellerActorID:X8}|" +
                        $"{countdownCancellerWorldId:X4}|" +
                        $"{FFXIVMemory.GetStringFromBytes(name, 32)}";
                }
            }
        }
        public const uint LogFileLineID = 269;
        public const string logLineName = "CountdownCancel";
        public const string MachinaPacketName = "CountdownCancel";

        public LineCountdownCancel(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
