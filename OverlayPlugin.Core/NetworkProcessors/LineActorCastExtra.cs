using System;
using System.Globalization;
using RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    class LineActorCastExtra : LineBaseSubMachina<LineActorCastExtra.ActorCastExtraPacket>
    {
        public const uint LogFileLineID = 263;
        public const string LogLineName = "ActorCastExtra";
        public const string MachinaPacketName = "ActorCast";

        internal class ActorCastExtraPacket : MachinaPacketWrapper
        {
            public override string ToString(long epoch, uint ActorID)
            {
                UInt16 abilityId = Get<UInt16>("ActionID");

                // for x/y/x, subtract 7FFF then divide by (2^15 - 1) / 100
                float x = FFXIVRepository.ConvertUInt16Coordinate(Get<UInt16>("PosX"));
                // In-game uses Y as elevation and Z as north-south, but ACT convention is to use
                // Z as elevation and Y as north-south.
                float y = FFXIVRepository.ConvertUInt16Coordinate(Get<UInt16>("PosZ"));
                float z = FFXIVRepository.ConvertUInt16Coordinate(Get<UInt16>("PosY"));
                // for rotation, the packet uses '0' as north, and each increment is 1/65536 of a CCW turn, while
                // in-game uses 0=south, pi/2=west, +/-pi=north
                // Machina thinks this is a float but that appears to be incorrect, so we have to reinterpret as
                // a UInt16
                double h = FFXIVRepository.ConvertHeading(FFXIVRepository.InterpretFloatAsUInt16(Get<float>("Rotation")));

                return string.Format(CultureInfo.InvariantCulture,
                    "{0:X8}|{1:X4}|{2:F3}|{3:F3}|{4:F3}|{5:F3}",
                    ActorID, abilityId, x, y, z, h);
            }
        }
        public LineActorCastExtra(TinyIoCContainer container)
            : base(container, LogFileLineID, LogLineName, MachinaPacketName) { }
    }
}