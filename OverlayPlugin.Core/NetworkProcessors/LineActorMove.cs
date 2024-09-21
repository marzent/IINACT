using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct ActorMove_v655
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

        public string ToString(uint actorID, FFXIVRepository ffxiv)
        {
            return $"{actorID:X8}|" +
                $"{ffxiv.ConvertHeading(rotation):F4}|" +
                $"{unknown1:X4}|" +
                $"{unknown2:X4}|" +
                $"{ffxiv.ConvertUInt16Coordinate(x):F4}|" +
                // y and z are intentionally flipped to match other log lines
                $"{ffxiv.ConvertUInt16Coordinate(z):F4}|" +
                $"{ffxiv.ConvertUInt16Coordinate(y):F4}";
        }
    }

    public class LineActorMove
    {
        public const uint LogFileLineID = 270;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;

        private readonly int offsetHeaderActorID;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;

        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineActorMove(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();
            try
            {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                offsetHeaderActorID = netHelper.GetOffset(msgHeaderType, "ActorID");
                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
                offsetPacketData = Marshal.SizeOf(msgHeaderType);
                ffxiv.RegisterNetworkParser(MessageReceived);
            }
            catch (System.IO.FileNotFoundException)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorMove",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            // Wait for network data to actually fetch opcode info from file and register log line
            // This is because the FFXIV_ACT_Plugin's `GetGameVersion` method only returns valid data
            // if the player is currently logged in/a network connection is active
            if (opcode == null)
            {
                opcode = opcodeConfig["ActorMove"];
                if (opcode == null)
                {
                    return;
                }
            }

            if (message.Length < opcode.size + offsetPacketData)
                return;

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[offsetMessageType] == opcode.opcode)
                {
                    uint actorID = *(uint*)&buffer[offsetHeaderActorID];

                    // Only emit for non-player actors
                    if (actorID < 0x40000000)
                    {
                        return;
                    }

                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    ActorMove_v655 actorMovePacket = *(ActorMove_v655*)&buffer[offsetPacketData];
                    logWriter(actorMovePacket.ToString(actorID, ffxiv), serverTime);

                    return;
                }
            }
        }
    }
}
