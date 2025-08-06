using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Machina.FFXIV;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineMapEffect : LineBaseCustom<
            Server_MessageHeader_Global, LineMapEffect.MapEffect_v62,
            Server_MessageHeader_CN, LineMapEffect.MapEffect_v62,
            Server_MessageHeader_KR, LineMapEffect.MapEffect_v62>
    {
        // `MapEffect` and some of the `MapEffect#` packets can be verified in Zelenia Normal, when blooms appear and despawn
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MapEffect_v62 : IPacketStruct, IMapEffectPacket
        {
            public uint instanceContentID;
            public ushort flags1;
            public ushort flags2;
            public byte index;
            public byte unknown1;
            public ushort unknown2;
            public uint padding;

            uint IMapEffectPacket.instanceContentID => instanceContentID;

            ushort IMapEffectPacket.count => 1;

            List<ushort> IMapEffectPacket.flags1
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    flags.Add(flags1);

                    return flags;
                }
            }

            List<ushort> IMapEffectPacket.flags2
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    flags.Add(flags2);

                    return flags;
                }
            }

            List<byte> IMapEffectPacket.indexes
            {
                get
                {
                    List<byte> indexes = new List<byte>();

                    indexes.Add(index);

                    return indexes;
                }
            }

            public string ToString(long epoch, uint ActorID)
            {
                return $"";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct MapEffect4_v72 : IPacketStruct, IMapEffectPacket
        {
            private const int MaxCount = 4;
            public ushort count;
            public fixed ushort flags1[MaxCount];
            public fixed ushort flags2[MaxCount];
            public fixed byte indexes[MaxCount];
            public ushort padding;

            uint IMapEffectPacket.instanceContentID => 0;

            ushort IMapEffectPacket.count => count;

            List<ushort> IMapEffectPacket.flags1
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags1)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<ushort> IMapEffectPacket.flags2
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags2)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<byte> IMapEffectPacket.indexes
            {
                get
                {
                    List<byte> indexes = new List<byte>();

                    unsafe
                    {
                        fixed (byte* ptr = this.indexes)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                indexes.Add(ptr[i]);
                            }
                        }
                    }

                    return indexes;
                }
            }

            public string ToString(long epoch, uint ActorID)
            {
                return $"";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct MapEffect8_v72 : IPacketStruct, IMapEffectPacket
        {
            private const int MaxCount = 8;
            public ushort count;
            public fixed ushort flags1[MaxCount];
            public fixed ushort flags2[MaxCount];
            public fixed byte indexes[MaxCount];
            public ushort padding;

            uint IMapEffectPacket.instanceContentID => 0;

            ushort IMapEffectPacket.count => count;

            List<ushort> IMapEffectPacket.flags1
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags1)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<ushort> IMapEffectPacket.flags2
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags2)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<byte> IMapEffectPacket.indexes
            {
                get
                {
                    List<byte> indexes = new List<byte>();

                    unsafe
                    {
                        fixed (byte* ptr = this.indexes)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                indexes.Add(ptr[i]);
                            }
                        }
                    }

                    return indexes;
                }
            }

            public string ToString(long epoch, uint ActorID)
            {
                return $"";
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe struct MapEffect12_v72 : IPacketStruct, IMapEffectPacket
        {
            private const int MaxCount = 12;
            public ushort count;
            public fixed ushort flags1[MaxCount];
            public fixed ushort flags2[MaxCount];
            public fixed byte indexes[MaxCount];
            public ushort padding;

            uint IMapEffectPacket.instanceContentID => 0;

            ushort IMapEffectPacket.count => count;

            List<ushort> IMapEffectPacket.flags1
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags1)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<ushort> IMapEffectPacket.flags2
            {
                get
                {
                    List<ushort> flags = new List<ushort>();

                    unsafe
                    {
                        fixed (ushort* ptr = flags2)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                flags.Add(ptr[i]);
                            }
                        }
                    }

                    return flags;
                }
            }

            List<byte> IMapEffectPacket.indexes
            {
                get
                {
                    List<byte> indexes = new List<byte>();

                    unsafe
                    {
                        fixed (byte* ptr = this.indexes)
                        {
                            for (var i = 0; i < count && i < MaxCount; ++i)
                            {
                                indexes.Add(ptr[i]);
                            }
                        }
                    }

                    return indexes;
                }
            }

            public string ToString(long epoch, uint ActorID)
            {
                return $"";
            }
        }

        internal interface IMapEffectPacket
        {
            uint instanceContentID { get; }
            ushort count { get; }
            List<ushort> flags1 { get; }
            List<ushort> flags2 { get; }
            List<byte> indexes { get; }
        }

        public const uint LogFileLineID = 257;
        public const string logLineName = "MapEffect";
        public const string MachinaPacketName = "MapEffect";
        private readonly ILogger logger;
        private RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect4_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect4_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect4_v72> packetHelper_4;

        private RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect8_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect8_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect8_v72> packetHelper_8;

        private RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect12_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect12_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect12_v72> packetHelper_12;

        public LineMapEffect(TinyIoCContainer container)
            : base(container, LogFileLineID, logLineName, MachinaPacketName)
        {
            logger = container.Resolve<ILogger>();

            var opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();

            packetHelper_4 = RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect4_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect4_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect4_v72>.CreateFromOpcodeConfig(opcodeConfig, $"{MachinaPacketName}4");

            packetHelper_8 = RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect8_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect8_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect8_v72>.CreateFromOpcodeConfig(opcodeConfig, $"{MachinaPacketName}8");

            packetHelper_12 = RegionalizedPacketHelper<
            Server_MessageHeader_Global, LineMapEffect.MapEffect12_v72,
            Server_MessageHeader_CN, LineMapEffect.MapEffect12_v72,
            Server_MessageHeader_KR, LineMapEffect.MapEffect12_v72>.CreateFromOpcodeConfig(opcodeConfig, $"{MachinaPacketName}12");
        }

        protected override void MessageReceived(string id, long epoch, byte[] message)
        {
            if (MessageReceivedSubHandler(packetHelper, epoch, message))
                return;
            if (MessageReceivedSubHandler(packetHelper_4, epoch, message))
                return;
            if (MessageReceivedSubHandler(packetHelper_8, epoch, message))
                return;
            if (MessageReceivedSubHandler(packetHelper_12, epoch, message))
                return;
        }

        protected bool MessageReceivedSubHandler<T>(RegionalizedPacketHelper<
            Server_MessageHeader_Global, T,
            Server_MessageHeader_CN, T,
            Server_MessageHeader_KR, T> helper, long epoch, byte[] message) where T : unmanaged, IMapEffectPacket, IPacketStruct
        {
            if (packetHelper == null)
                return true;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return true;

            switch (currentRegion)
            {
                case GameRegion.Global:
                    {
                        if (!helper.global.ToStructs(message, out var header, out var packet))
                            return false;
                        WriteLinesFor(epoch, packet.instanceContentID, packet.count, packet.flags1, packet.flags2, packet.indexes);
                        return true;
                    }
                case GameRegion.Chinese:
                    {
                        if (!helper.cn.ToStructs(message, out var header, out var packet))
                            return false;
                        WriteLinesFor(epoch, packet.instanceContentID, packet.count, packet.flags1, packet.flags2, packet.indexes);
                        return true;
                    }
                case GameRegion.Korean:
                    {
                        if (!helper.kr.ToStructs(message, out var header, out var packet))
                            return false;
                        WriteLinesFor(epoch, packet.instanceContentID, packet.count, packet.flags1, packet.flags2, packet.indexes);
                        return true;
                    }

                default:
                    return false;
            }
        }

        private void WriteLinesFor(long epoch, uint instanceContentID, ushort count, List<ushort> flags1, List<ushort> flags2, List<byte> indexes)
        {
            if (count == 0)
            {
                logger.Log(LogLevel.Error, $"Got MapEffect packet with 0 entries");
                return;
            }

            if (flags1.Count != count)
            {
                logger.Log(LogLevel.Error, $"Mismatch in flags1 count for MapEffect packet, expected {count}, got {flags1.Count}");
                return;
            }
            if (flags2.Count != count)
            {
                logger.Log(LogLevel.Error, $"Mismatch in flags2 count for MapEffect packet, expected {count}, got {flags2.Count}");
                return;
            }
            if (indexes.Count != count)
            {
                logger.Log(LogLevel.Error, $"Mismatch in indexes count for MapEffect packet, expected {count}, got {indexes.Count}");
                return;
            }

            DateTime serverTime = ffxiv.EpochToDateTime(epoch);

            var strInstanceContentID = instanceContentID == 0 ? "" : $"{instanceContentID:X8}";

            for (var i = 0; i < count; ++i)
            {
                var flag1 = flags1[i];
                var flag2 = flags2[i];
                var index = indexes[i];

                // Two blank fields at the end used to be $"{unknown1:X2}|{unknown2:X4}", turned out to be padding
                // @TODO: Remove these fields at some point, this is a breaking change.
                // @TODO: Since we'll be breaking things anyways, maybe separate flags out into two fields?
                logWriter($"{strInstanceContentID}|{flag2:X4}{flag1:X4}|{index:X2}||", serverTime);
            }
        }
    }
}
