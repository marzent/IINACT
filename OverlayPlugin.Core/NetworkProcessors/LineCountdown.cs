using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct Countdown_v655
    {
        // 6.5.5 packet data (minus header):
        // 34120010 4F00 1500 53  00  00  0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20 00000000
        // AAAAAAAA CCCC BBBB DD  EE  FF  GGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGGG HHHHHHHH
        // 0x0      0x4  0x6  0x8 0x9 0xA 0xB
        // Actor ID Wrld Time Res Unk Unk Name

        public const int structSize = 48;
        [FieldOffset(0x0)]
        public uint countdownStarterActorID;
        [FieldOffset(0x4)]
        public ushort countdownStarterWorldId;

        [FieldOffset(0x6)]
        public ushort countdownTimeMS;
        [FieldOffset(0x8)]
        public byte countdownResultCode;

        [FieldOffset(0xB)]
        public fixed byte countdownStarterName[32];

        public override string ToString()
        {
            fixed (byte* name = countdownStarterName)
            {
                return
                    $"{countdownStarterActorID:X8}|" +
                    $"{countdownStarterWorldId:X4}|" +
                    $"{countdownTimeMS}|" +
                    $"{countdownResultCode:X2}|" +
                    $"{FFXIVMemory.GetStringFromBytes(name, 32)}";
            }
        }
    }

    public class LineCountdown
    {
        public const uint LogFileLineID = 268;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineCountdown(TinyIoCContainer container)
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
                Name = "Countdown",
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
                opcode = opcodeConfig["Countdown"];
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
                    Countdown_v655 countdownPacket = *(Countdown_v655*)&buffer[offsetPacketData];
                    logWriter(countdownPacket.ToString(), serverTime);

                    return;
                }
            }
        }
    }
}
