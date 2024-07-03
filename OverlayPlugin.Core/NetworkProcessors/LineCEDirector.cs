using System.Collections.Generic;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineCEDirector : LineBaseCustom<
            Server_MessageHeader_Global, LineCEDirector.CEDirector_v62,
            Server_MessageHeader_CN, LineCEDirector.CEDirector_v62,
            Server_MessageHeader_KR, LineCEDirector.CEDirector_v62>
    {
        [StructLayout(LayoutKind.Explicit)]
        internal struct CEDirector_v62 : IPacketStruct
        {
            [FieldOffset(0x0)]
            public uint popTime;
            [FieldOffset(0x4)]
            public ushort timeRemaining;
            [FieldOffset(0x6)]
            public ushort unk9;
            [FieldOffset(0x8)]
            public byte ceKey;
            [FieldOffset(0x9)]
            public byte numPlayers;
            [FieldOffset(0xA)]
            public byte status;
            [FieldOffset(0xB)]
            public byte unk10;
            [FieldOffset(0xC)]
            public byte progress;
            [FieldOffset(0xD)]
            public byte unk11;
            [FieldOffset(0xE)]
            public byte unk12;
            [FieldOffset(0xF)]
            public byte unk13;

            public string ToString(long epoch, uint ActorID)
            {
                string line =
                    $"{popTime:X8}|" +
                    $"{timeRemaining:X4}|" +
                    $"{unk9:X4}|" +
                    $"{ceKey:X2}|" +
                    $"{numPlayers:X2}|" +
                    $"{status:X2}|" +
                    $"{unk10:X2}|" +
                    $"{progress:X2}|" +
                    $"{unk11:X2}|" +
                    $"{unk12:X2}|" +
                    $"{unk13:X2}";

                var isBeingRemoved = status == 0;
                if (isBeingRemoved)
                {
                    if (!ces.Remove(ceKey))
                    {
                        return null;
                    }
                }
                else
                {
                    string oldData;
                    if (ces.TryGetValue(ceKey, out oldData))
                    {
                        if (oldData == line)
                        {
                            return null;
                        }
                    }
                    ces[ceKey] = line;
                }

                return line;
            }
        }
        public const uint LogFileLineID = 259;
        public const string logLineName = "CEDirector";
        public const string MachinaPacketName = "CEDirector";

        // Used to reduce spam of these packets to log file
        // Only emit a line if it doesn't match the last line for this CE ID
        private static Dictionary<byte, string> ces = new Dictionary<byte, string>();

        public LineCEDirector(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName)
        {
            ffxiv.RegisterZoneChangeDelegate((zoneID, zoneName) => ces.Clear());
        }
    }
}
