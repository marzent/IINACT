using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors {
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    internal struct MapEffect_v62 {
        [FieldOffset(0x0)]
        public uint instanceContentID;
        [FieldOffset(0x4)]
        public uint flags;
        [FieldOffset(0x8)]
        public byte index;
        [FieldOffset(0x9)]
        public byte unknown1;
        [FieldOffset(0x10)]
        public ushort unknown2;

        public override string ToString() {
            return $"{instanceContentID:X8}|{flags:X8}|{index:X2}|{unknown1:X2}|{unknown2:X4}";
        }
    }

    public class LineMapEffect {
        public const uint LogFileLineID = 257;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;

        private Func<string, bool> logWriter;

        public LineMapEffect(TinyIoCContainer container) {
            logger = container.Resolve<ILogger>();
            var ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            opcodeConfig = container.Resolve<OverlayPluginLogLineConfig>();
            try {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
                offsetPacketData = Marshal.SizeOf(msgHeaderType);
                ffxiv.RegisterNetworkParser(MessageReceived);
            }
            catch (System.IO.FileNotFoundException) {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e) {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry() {
                Name = "MapEffect",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message) {
            // Wait for network data to actually fetch opcode info from file and register log line
            // This is because the FFXIV_ACT_Plugin's `GetGameVersion` method only returns valid data
            // if the player is currently logged in/a network connection is active
            if (opcode == null) {
                opcode = opcodeConfig["MapEffect"];
            }

            if (message.Length < opcode.size + offsetPacketData)
                return;

            fixed (byte* buffer = message) {
                if (*(ushort*)&buffer[offsetMessageType] == opcode.opcode) {
                    MapEffect_v62 mapEffectPacket = *(MapEffect_v62*)&buffer[offsetPacketData];
                    logWriter(mapEffectPacket.ToString());

                    return;
                }
            }
        }

    }
}
