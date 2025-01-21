using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using Machina.FFXIV;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    /**
     * Class for position/angle info from ActionEffect (i.e. line 21/22).
     */
    class LineAbilityExtra : LineBaseSubMachina<LineAbilityExtra.AbilityExtraPacket<LineAbilityExtra.Server_ActionEffect1_Extra>>
    {
        public const uint LogFileLineID = 264;
        public const string LogLineName = "AbilityExtra";
        public const string MachinaPacketName = "ActionEffect1";

        internal class AbilityExtraPacket<T> : MachinaPacketWrapper
            where T : unmanaged, IActionEffectExtra
        {
            public unsafe override string ToString(long epoch, uint ActorID)
            {
                IntPtr packetPtr = IntPtr.Zero;
                try
                {
                    MachinaPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>> packetHelper = (MachinaPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>>)aeHelper[staticRegion.Value];

                    packetPtr = Marshal.AllocHGlobal(Marshal.SizeOf(packetValue));
                    Marshal.StructureToPtr(packetValue, packetPtr, true);

                    T rawPacket = *(T*)packetPtr.ToPointer();

                    packetHelper.ToStructs(packetPtr, out var _, out var aeHeader, true);

                    // Ability ID is really 16-bit, so it is formatted as such, but we will get an
                    // exception if we try to prematurely cast it to UInt16
                    var abilityId = aeHeader.Get<uint>("actionId");
                    var globalEffectCounter = aeHeader.Get<uint>("globalEffectCounter");

                    var h = FFXIVRepository.ConvertHeading(aeHeader.Get<ushort>("rotation"));
                    var atId = (aeHeader.Get<uint>("animationTargetId"));

                    if (rawPacket.actionEffectCount == 1)
                    {
                        // AE1 only contains rotation.
                        return string.Format(CultureInfo.InvariantCulture,
                            "{0:X8}|{1:X4}|{2:X8}|{3}||||{4:F3}|{5:X8}",
                            ActorID, abilityId, globalEffectCounter, (int)LineSubType.NO_DATA, h, atId);
                    }

                    float x = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.x);
                    float y = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.y);
                    float z = FFXIVRepository.ConvertUInt16Coordinate(rawPacket.z);

                    return string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X4}|{2:X8}|{3}|{4:F3}|{5:F3}|{6:F3}|{7:F3}|{8:X8}",
                        ActorID, abilityId, globalEffectCounter, (int)LineSubType.DATA_PRESENT, x, y, z, h, atId);
                }
                finally
                {
                    if (packetPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(packetPtr);
                    }
                }
            }
        }

        // Just use the smallest expected packet for the default `packetHelper` implementation
        // We don't care if actual packet data is mangled in the struct, it is just to access the header data
        private static MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>> aeHelper;

        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect8_Extra>> packetHelper_8;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect16_Extra>> packetHelper_16;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect24_Extra>> packetHelper_24;
        private MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect32_Extra>> packetHelper_32;

        protected static GameRegion? staticRegion;

        internal interface IActionEffectExtra
        {
            uint actionEffectCount { get; }
            ushort x { get; }
            ushort y { get; }
            ushort z { get; }
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct Server_ActionEffect1_Extra : IActionEffectExtra
        {
            public uint actionEffectCount => 1;

            public ushort x => 0;
            public ushort y => 0;
            public ushort z => 0;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct Server_ActionEffect8_Extra : IActionEffectExtra
        {
            [FieldOffset(0x290)]
            private ushort _x;

            [FieldOffset(0x292)]
            private ushort _z;

            [FieldOffset(0x294)]
            private ushort _y;

            public uint actionEffectCount => 8;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct Server_ActionEffect16_Extra : IActionEffectExtra
        {
            [FieldOffset(0x4D0)]
            public ushort _x;

            [FieldOffset(0x4D2)]
            public ushort _z;

            [FieldOffset(0x4D4)]
            public ushort _y;

            public uint actionEffectCount => 16;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct Server_ActionEffect24_Extra : IActionEffectExtra
        {
            [FieldOffset(0x710)]
            public ushort _x;

            [FieldOffset(0x712)]
            public ushort _z;

            [FieldOffset(0x714)]
            public ushort _y;

            public uint actionEffectCount => 24;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        [StructLayout(LayoutKind.Explicit, Pack = 1)]
        internal struct Server_ActionEffect32_Extra : IActionEffectExtra
        {
            [FieldOffset(0x950)]
            public ushort _x;

            [FieldOffset(0x952)]
            public ushort _z;

            [FieldOffset(0x954)]
            public ushort _y;

            public uint actionEffectCount => 32;

            public ushort x => _x;
            public ushort y => _y;
            public ushort z => _z;
        }

        // For possible future expansion.
        private enum LineSubType
        {
            NO_DATA = 0,
            DATA_PRESENT = 1,
            ERROR = 256
        }

        public LineAbilityExtra(TinyIoCContainer container) : base(container, LogFileLineID, LogLineName, MachinaPacketName)
        {
            var logger = container.Resolve<ILogger>();

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>>.Create(MachinaPacketName, out packetHelper, "Ability1"))
            {
                logger.Log(LogLevel.Error, $"Failed to initialize LineAbilityExtra: Creating {MachinaPacketName} failed");
                return;
            }

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect1_Extra>>.Create("ActionEffectHeader", out aeHelper, "Ability1"))
            {
                logger.Log(LogLevel.Error, "Failed to initialize LineAbilityExtra: Creating ActionEffectHeader failed");
                return;
            }

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect8_Extra>>.Create("ActionEffect8", out packetHelper_8, "Ability8"))
            {
                logger.Log(LogLevel.Error, "Failed to initialize LineAbilityExtra: Creating ActionEffect8 failed");
                return;
            }

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect16_Extra>>.Create("ActionEffect16", out packetHelper_16, "Ability16"))
            {
                logger.Log(LogLevel.Error, "Failed to initialize LineAbilityExtra: Creating ActionEffect16 failed");
                return;
            }

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect24_Extra>>.Create("ActionEffect24", out packetHelper_24, "Ability24"))
            {
                logger.Log(LogLevel.Error, "Failed to initialize LineAbilityExtra: Creating ActionEffect24 failed");
                return;
            }

            if (!MachinaRegionalizedPacketHelper<AbilityExtraPacket<Server_ActionEffect32_Extra>>.Create("ActionEffect32", out packetHelper_32, "Ability32"))
            {
                logger.Log(LogLevel.Error, "Failed to initialize LineAbilityExtra: Creating ActionEffect32 failed");
                return;
            }
        }

        protected override void ProcessChanged(Process process)
        {
            base.ProcessChanged(process);

            staticRegion = null;
        }

        protected override unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (packetHelper_32 == null)
                return;

            if (staticRegion == null)
                staticRegion = ffxiv.GetMachinaRegion();

            if (staticRegion == null)
                return;

            var line = packetHelper[staticRegion.Value].ToString(epoch, message);

            if (line == null)
            {
                line = packetHelper_8[staticRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_16[staticRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_24[staticRegion.Value].ToString(epoch, message);
            }

            if (line == null)
            {
                line = packetHelper_32[staticRegion.Value].ToString(epoch, message);
            }

            if (line != null)
            {
                DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                logWriter(line, serverTime);

                return;
            }
        }
    }
}
