using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct RSV_v62
    {
        public const int structSize = 1080;
        public const int keySize = 0x30;
        public const int valueSize = 0x404;
        [FieldOffset(0x0)]
        public uint unknown1;
        [FieldOffset(0x4)]
        public fixed byte key[keySize];
        [FieldOffset(0x34)]
        public fixed byte value[valueSize];

        public override string ToString()
        {
            fixed (byte* key = this.key) fixed (byte* value = this.value)
            {
                return
                    $"|" +
                    $"{unknown1:X8}|" +
                    $"{FFXIVMemory.GetStringFromBytes(key, keySize)}|" +
                    $"{FFXIVMemory.GetStringFromBytes(value, valueSize)}";
            }
        }

        public string ToString(string locale)
        {
            fixed (byte* key = this.key) fixed (byte* value = this.value)
            {
                return
                    $"{locale}|" +
                    $"{unknown1:X8}|" +
                    $"{FFXIVMemory.GetStringFromBytes(key, keySize).Replace("\r", "\\r").Replace("\n", "\\n")}|" +
                    $"{FFXIVMemory.GetStringFromBytes(value, valueSize).Replace("\r", "\\r").Replace("\n", "\\n")}";
            }
        }
    }

    public class LineRSV
    {
        public const uint LogFileLineID = 262;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineRSV(TinyIoCContainer container)
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
                Name = "RSVData",
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
                opcode = opcodeConfig["RSVData"];
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
                    RSV_v62 RSVPacket = *(RSV_v62*)&buffer[offsetPacketData];
                    logWriter(RSVPacket.ToString(ffxiv.GetLocaleString()), serverTime);

                    return;
                }
            }
        }
    }
}
