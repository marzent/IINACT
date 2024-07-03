using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineBattleTalk2 : LineBaseCustom<
            Server_MessageHeader_Global, LineBattleTalk2.BattleTalk2_v655,
            Server_MessageHeader_CN, LineBattleTalk2.BattleTalk2_v655,
            Server_MessageHeader_KR, LineBattleTalk2.BattleTalk2_v655>
    {
        [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
        internal unsafe struct BattleTalk2_v655 : IPacketStruct
        {
            // 00|2024-02-25T15:13:29.0000000-05:00|0044|Whiskerwall Kupdi Koop|Mogglesguard, assemble! We must drive them out together, kupo!|e9f836e9767bed2e
            // Pre-processed data from packet (sorry, no raw packet for this one, instead it's my scuffed debugging packet dump's data)
            // 00000000|00000000|80034E2B|000002CE|33804|5000|0|2|0|0
            // first 4 bytes are actor ID, not always set
            // 0x80034E2B = instance content ID
            // 0x2CE = entry on `BNpcName` table
            // 33804 = entry on `InstanceContentTextData` table
            // 5000 = display time in ms
            // 2 = some sort of flags for display settings?

            public const int structSize = 40;
            [FieldOffset(0x0)]
            public uint actorID;
            [FieldOffset(0x8)]
            public uint instanceContentID;
            [FieldOffset(0xC)]
            public uint npcNameID;
            [FieldOffset(0x10)]
            public uint instanceContentTextID;
            [FieldOffset(0x14)]
            public uint displayMS;
            [FieldOffset(0x18)]
            public uint param1;
            [FieldOffset(0x1C)]
            public uint param2;
            [FieldOffset(0x20)]
            public uint param3;
            [FieldOffset(0x24)]
            public uint param4;

            public string ToString(long epoch, uint ActorID)
            {
                return
                    $"{actorID:X8}|" +
                    $"{instanceContentID:X8}|" +
                    $"{npcNameID:X4}|" +
                    $"{instanceContentTextID:X4}|" +
                    $"{displayMS}|" +
                    $"{param1:X}|" +
                    $"{param2:X}|" +
                    $"{param3:X}|" +
                    $"{param4:X}";
            }
        }

        public const uint LogFileLineID = 267;
        public const string logLineName = "BattleTalk2";
        public const string MachinaPacketName = "BattleTalk2";

        public LineBattleTalk2(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
