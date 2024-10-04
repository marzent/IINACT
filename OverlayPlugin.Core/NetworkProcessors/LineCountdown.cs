using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineCountdown : LineBaseCustom<
            Server_MessageHeader_Global, LineCountdown.Countdown_v655,
            Server_MessageHeader_CN, LineCountdown.Countdown_v655,
            Server_MessageHeader_KR, LineCountdown.Countdown_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct Countdown_v655 : IPacketStruct
        {
            // 6.5.5 packet data (minus header):
            // 34120010 4F00 1500 53  00  00  0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20 00000000
            // AAAAAAAA CCCC BBBB DD  EE  FF  GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG HHHHHHHH
            // 0x0      0x4  0x6  0x8 0x9 0xA 0xB
            // Actor ID Wrld Time Res Unk Unk Name

            public const int structSize = 48;
            [FieldOffset(0x0)]
            public uint countdownStarterActorID;
            [FieldOffset(0x4)]
            public ushort countdownStarterWorldId;

            [FieldOffset(0x6)]
            public ushort countdownTimeSeconds;
            [FieldOffset(0x8)]
            public byte countdownResultCode;

            [FieldOffset(0xB)]
            public fixed byte countdownStarterName[32];

            public string ToString(long epoch, uint ActorID)
            {
                fixed (byte* name = countdownStarterName)
                {
                    return
                        $"{countdownStarterActorID:X8}|" +
                        $"{countdownStarterWorldId:X4}|" +
                        $"{countdownTimeSeconds}|" +
                        $"{countdownResultCode:X2}|" +
                        $"{FFXIVMemory.GetStringFromBytes(name, 32)}";
                }
            }
        }

        public const uint LogFileLineID = 268;
        public const string logLineName = "Countdown";
        public const string MachinaPacketName = "Countdown";

        public LineCountdown(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
