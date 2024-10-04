using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineMapEffect : LineBaseCustom<
            Server_MessageHeader_Global, LineMapEffect.MapEffect_v62,
            Server_MessageHeader_CN, LineMapEffect.MapEffect_v62,
            Server_MessageHeader_KR, LineMapEffect.MapEffect_v62>
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MapEffect_v62 : IPacketStruct
        {
            public uint instanceContentID;
            public uint flags;
            public byte index;
            public byte unknown1;
            public ushort unknown2;
            public uint padding;

            public string ToString(long epoch, uint ActorID)
            {
                return $"{instanceContentID:X8}|{flags:X8}|{index:X2}|{unknown1:X2}|{unknown2:X4}";
            }
        }

        public const uint LogFileLineID = 257;
        public const string logLineName = "MapEffect";
        public const string MachinaPacketName = "MapEffect";

        public LineMapEffect(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName) { }
    }
}
