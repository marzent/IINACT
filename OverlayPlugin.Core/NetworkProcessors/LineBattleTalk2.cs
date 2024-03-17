using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct BattleTalk2_v655
    {
        // 00|2024-02-25T15:13:29.0000000-05:00|0044|Whiskerwall Kupdi Koop|Mogglesguard, assemble! We must drive them out together, kupo!|e9f836e9767bed2e
        // Pre-processed data from packet (sorry, no raw packet for this one, instead it's my scuffed debugging packet dump's data)
        // 00000000|00000000|80034E2B|000002CE|33804|5000|0|2|0|0
        // first 4 bytes are actor ID, not always set
        // 0x80034E2B = instance content ID
        // 0x2CE = entry on `BNpcName` table
        // 33804 = entry on `InstanceContentTextData` table
        // 5000 = display time in ms?
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
        public uint param1;
        [FieldOffset(0x18)]
        public uint param2;
        [FieldOffset(0x1C)]
        public uint param3;
        [FieldOffset(0x20)]
        public uint param4;
        [FieldOffset(0x24)]
        public uint param5;

        public override string ToString()
        {
            return
                $"{actorID:X8}|" +
                $"{instanceContentID:X8}|" +
                $"{npcNameID:X8}|" +
                $"{instanceContentTextID:X8}|" +
                $"{param1:X8}|" +
                $"{param2:X8}|" +
                $"{param3:X8}|" +
                $"{param4:X8}|" +
                $"{param5:X8}";
        }
    }

    public class LineBattleTalk2
    {
        public const uint LogFileLineID = 267;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineBattleTalk2(TinyIoCContainer container)
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
                Name = "BattleTalk2",
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
                opcode = opcodeConfig["BattleTalk2"];
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
                    BattleTalk2_v655 battleTalk2Packet = *(BattleTalk2_v655*)&buffer[offsetPacketData];
                    logWriter(battleTalk2Packet.ToString(), serverTime);

                    return;
                }
            }
        }
    }
}
