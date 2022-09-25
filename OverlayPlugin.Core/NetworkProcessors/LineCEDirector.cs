using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    internal struct CEDirector_v62
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

        public override string ToString()
        {
            return 
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
        }
    }

    public class LineCEDirector
    {
        public const uint LogFileLineID = 259;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;

        private Func<string, bool> logWriter;

        public LineCEDirector(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            var ffxiv = container.Resolve<FFXIVRepository>();
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
                opcode = opcodeConfig["CEDirector"];
            }

            if (message.Length < opcode.size + offsetPacketData)
                return;

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[offsetMessageType] == opcode.opcode)
                {
                    CEDirector_v62 CEDirectorPacket = *(CEDirector_v62*)&buffer[offsetPacketData];
                    logWriter(CEDirectorPacket.ToString());

                    return;
                }
            }
        }

    }
}
