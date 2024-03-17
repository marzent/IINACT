using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct NpcYell_v655
    {
        // 00|2024-02-22T22:35:03.0000000-05:00|0044|Shanoa|Meow!♪|1d173e4a0eacfd95
        // 6.5.5 packet data (minus header):
        // 8A6B0140 00000000 0624 0000 D624 00000000 00000000 00000000 00000000 0000
        // AAAAAAAA BBBBBBBB CCCC DDDD EEEE FFFFFFFF GGGGGGGG HHHHHHHH IIIIIIII JJJJ
        // 0x0      0x4      0x8  0xA  0xC
        // Actor ID          NameID    YellID

        public const int structSize = 32;
        [FieldOffset(0x0)]
        public uint actorID;
        [FieldOffset(0x8)]
        public ushort nameID;
        [FieldOffset(0xC)]
        public ushort yellID;

        public override string ToString()
        {
            return
                $"{actorID:X8}|" +
                $"{nameID:X4}|" +
                $"{yellID:X4}";
        }
    }

    public class LineNpcYell
    {
        public const uint LogFileLineID = 266;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineNpcYell(TinyIoCContainer container)
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
                Name = "NpcYell",
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
                opcode = opcodeConfig["NpcYell"];
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
                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    NpcYell_v655 yellPacket = *(NpcYell_v655*)&buffer[offsetPacketData];
                    logWriter(yellPacket.ToString(), serverTime);

                    return;
                }
            }
        }
    }
}
