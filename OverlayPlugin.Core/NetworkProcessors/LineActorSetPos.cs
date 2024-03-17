using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct ActorSetPos_v655
    {
        // 6.5.5 packet data (minus header):
        // 6AD3 0F  02  00000000 233E3BC1 00000000 D06AF840 00000000
        // AAAA BB  CC  DDDDDDDD EEEEEEEE FFFFFFFF GGGGGGGG HHHHHHHH
        // 0x0  0x2 0x3 0x4      0x8      0xC      0x10     0x14
        // Rot  unk unk unk      X        Y        Z        unk

        // Have never seen data in 0x4 or 0x14, probably just padding?

        public const int structSize = 24;
        [FieldOffset(0x0)]
        public ushort rotation;

        [FieldOffset(0x2)]
        public byte unknown1;

        [FieldOffset(0x3)]
        public byte unknown2;

        // Yes, these are actually floats, and not some janky ushort that needs converted through ConvertUInt16Coordinate
        [FieldOffset(0x8)]
        public float x;
        [FieldOffset(0xC)]
        public float y;
        [FieldOffset(0x10)]
        public float z;

        public string ToString(uint actorID, FFXIVRepository ffxiv)
        {
            return $"{actorID:X8}|" +
                $"{ffxiv.ConvertHeading(rotation):F4}|" +
                $"{unknown1:X2}|" +
                $"{unknown2:X2}|" +
                $"{x:F4}|" +
                // y and z are intentionally flipped to match other log lines
                $"{z:F4}|" +
                $"{y:F4}";
        }
    }

    public class LineActorSetPos
    {
        public const uint LogFileLineID = 271;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;

        private readonly int offsetHeaderActorID;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;

        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineActorSetPos(TinyIoCContainer container)
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
                Name = "ActorSetPos",
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
                opcode = opcodeConfig["ActorSetPos"];
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
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    ActorSetPos_v655 actorSetPosPacket = *(ActorSetPos_v655*)&buffer[offsetPacketData];
                    logWriter(actorSetPosPacket.ToString(actorID, ffxiv), serverTime);

                    return;
                }
            }
        }
    }
}
