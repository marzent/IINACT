using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors {
    public class LineFateControl {
        public const uint LogFileLineID = 258;
        private ILogger logger;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;

        private const ushort FateAddCategory = 0x935;
        private const ushort FateRemoveCategory = 0x936;
        private const ushort FateUpdateCategory = 0x93E;
        private static readonly List<ushort> FateCategories = new List<ushort>() { FateAddCategory, FateRemoveCategory, FateUpdateCategory };

        private Func<string, bool> logWriter;

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct ActorControlSelf_v62 {
            public ushort category;
            public ushort padding;
            public uint param1;
            public uint param2;
            public uint param3;
            public uint param4;
            public uint param5;
            public uint param6;
            public uint padding1;

            public override string ToString() {
                return $"{category:X4}|{padding:X4}|{param1:X8}|{param2:X8}|{param3:X8}|{param4:X8}|{param5:X8}|{param6:X8}|{padding1:X8}";
            }
        }

        public LineFateControl(TinyIoCContainer container) {
            logger = container.Resolve<ILogger>();
            var ffxiv = container.Resolve<FFXIVRepository>();
            var netHelper = container.Resolve<NetworkParser>();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            this.logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry() {
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
            try {
                var mach = Assembly.Load("Machina.FFXIV");
                var msgHeaderType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                offsetMessageType = netHelper.GetOffset(msgHeaderType, "MessageType");
                offsetPacketData = Marshal.SizeOf(msgHeaderType);
                var packetType = mach.GetType("Machina.FFXIV.Headers.Server_ActorControlSelf");
                opcode = new OpcodeConfigEntry() {
                    opcode = netHelper.GetOpcode("ActorControlSelf"),
                    size = (uint)Marshal.SizeOf(typeof(ActorControlSelf_v62)),
                };
                ffxiv.RegisterNetworkParser(MessageReceived);
            }
            catch (System.IO.FileNotFoundException) {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e) {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message) {
            if (message.Length < opcode.size + offsetPacketData)
                return;

            fixed (byte* buffer = message) {
                if (*(ushort*)&buffer[offsetMessageType] == opcode.opcode) {
                    ActorControlSelf_v62 mapEffectPacket = *(ActorControlSelf_v62*)&buffer[offsetPacketData];
                    if (FateCategories.Contains(mapEffectPacket.category)) {
                        string category = "";
                        switch (mapEffectPacket.category) {
                            case FateAddCategory:
                                category = "Add";
                                break;
                            case FateRemoveCategory:
                                category = "Remove";
                                break;
                            case FateUpdateCategory:
                                category = "Update";
                                break;
                        }
                        logWriter(
                            $"{category}|" +
                            $"{mapEffectPacket.padding:X4}|" +
                            $"{mapEffectPacket.param1:X8}|" +
                            $"{mapEffectPacket.param2:X8}|" +
                            $"{mapEffectPacket.param3:X8}|" +
                            $"{mapEffectPacket.param4:X8}|" +
                            $"{mapEffectPacket.param5:X8}|" +
                            $"{mapEffectPacket.param6:X8}|" +
                            $"{mapEffectPacket.padding1:X8}"
                        );
                    }

                    return;
                }
            }
        }

    }
}
