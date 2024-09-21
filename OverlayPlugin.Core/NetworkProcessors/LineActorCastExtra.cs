using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineActorCastExtra
    {
        public const uint LogFileLineID = 263;
        private ILogger logger;
        private readonly FFXIVRepository ffxiv;

        private class RegionalizedInfo
        {
            public readonly int packetSize;
            public readonly int packetOpcode;
            public readonly int offsetMessageType;
            public readonly Type headerType;
            public readonly Type actorCastType;
            public readonly FieldInfo fieldCastSourceId;
            public readonly FieldInfo fieldAbilityId;
            public readonly FieldInfo fieldX;
            public readonly FieldInfo fieldY;
            public readonly FieldInfo fieldZ;
            public readonly FieldInfo fieldR;

            public RegionalizedInfo(Type headerType, Type actorCastType, NetworkParser netHelper)
            {
                this.headerType = headerType;
                this.actorCastType = actorCastType;
                fieldCastSourceId = headerType.GetField("ActorID");
                fieldAbilityId = actorCastType.GetField("ActionID");
                fieldX = actorCastType.GetField("PosX");
                fieldY = actorCastType.GetField("PosY");
                fieldZ = actorCastType.GetField("PosZ");
                fieldR = actorCastType.GetField("Rotation");
                packetOpcode = netHelper.GetOpcode("ActorCast");
                packetSize = Marshal.SizeOf(actorCastType);
                offsetMessageType = netHelper.GetOffset(headerType, "MessageType");
            }
        }

        private RegionalizedInfo regionalized;

        private readonly Func<string, DateTime, bool> logWriter;
        private readonly NetworkParser netHelper;

        public LineActorCastExtra(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            netHelper = container.Resolve<NetworkParser>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorCastExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private void ProcessChanged(Process process)
        {
            GameRegion region = ffxiv.GetMachinaRegion();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            try
            {
                Assembly mach = Assembly.Load("Machina.FFXIV");
                Type headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                string actorCastTypeStr;
                switch (region)
                {
                    case GameRegion.Global:
                        {
                            actorCastTypeStr = "Machina.FFXIV.Headers.Server_ActorCast";
                            break;
                        }
                    case GameRegion.Korean:
                        {
                            actorCastTypeStr = "Machina.FFXIV.Headers.Korean.Server_ActorCast";
                            break;
                        }
                    case GameRegion.Chinese:
                        {
                            actorCastTypeStr = "Machina.FFXIV.Headers.Chinese.Server_ActorCast";
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }

                Type actorCastType = mach.GetType(actorCastTypeStr);
                RegionalizedInfo info = new RegionalizedInfo(headerType, actorCastType, netHelper);
                regionalized = info;
            }
            catch (System.IO.FileNotFoundException)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            RegionalizedInfo info = regionalized;
            if (info == null)
                return;

            if (message.Length < info.packetSize)
                return;

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[info.offsetMessageType] == info.packetOpcode)
                {
                    object header = Marshal.PtrToStructure(new IntPtr(buffer), info.headerType);
                    UInt32 sourceId = (UInt32)info.fieldCastSourceId.GetValue(header);

                    object packet = Marshal.PtrToStructure(new IntPtr(buffer), info.actorCastType);
                    UInt16 abilityId = (UInt16)info.fieldAbilityId.GetValue(packet);
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    // for x/y/x, subtract 7FFF then divide by (2^15 - 1) / 100
                    float x = ffxiv.ConvertUInt16Coordinate((UInt16)info.fieldX.GetValue(packet));
                    // In-game uses Y as elevation and Z as north-south, but ACT convention is to use
                    // Z as elevation and Y as north-south.
                    float y = ffxiv.ConvertUInt16Coordinate((UInt16)info.fieldZ.GetValue(packet));
                    float z = ffxiv.ConvertUInt16Coordinate((UInt16)info.fieldY.GetValue(packet));
                    // for rotation, the packet uses '0' as north, and each increment is 1/65536 of a CCW turn, while
                    // in-game uses 0=south, pi/2=west, +/-pi=north
                    // Machina thinks this is a float but that appears to be incorrect, so we have to reinterpret as
                    // a UInt16
                    double h = ffxiv.ConvertHeading(ffxiv.InterpretFloatAsUInt16((float)info.fieldR.GetValue(packet)));


                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X4}|{2:F3}|{3:F3}|{4:F3}|{5:F3}",
                        sourceId, abilityId, x, y, z, h);

                    logWriter(line, serverTime);
                }
            }
        }
    }
}
