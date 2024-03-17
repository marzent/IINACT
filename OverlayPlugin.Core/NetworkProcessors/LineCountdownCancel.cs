using System;
using System.Reflection;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors;

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    [StructLayout(LayoutKind.Explicit, Size = structSize, Pack = 1)]
    internal unsafe struct CountdownCancel_v655
    {
        // 6.5.5 packet data (minus header):
        // 34120010 4F00 0000 0102030405060708090A0B0C0D0E0F101112131415161718191A1B1C1D1E1F20
        // AAAAAAAA BBBB CCCC DDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDDD
        // 0x0      0x4  0x6  0x8
        // Actor ID Wrld Unk  Name

        public const int structSize = 40;
        [FieldOffset(0x0)]
        public uint countdownCancellerActorID;
        [FieldOffset(0x4)]
        public ushort countdownCancellerWorldId;

        [FieldOffset(0x8)]
        public fixed byte countdownCancellerName[32];

        public override string ToString()
        {
            fixed (byte* name = countdownCancellerName)
            {
                return
                    $"{countdownCancellerActorID:X8}|" +
                    $"{countdownCancellerWorldId:X4}|" +
                    $"{FFXIVMemory.GetStringFromBytes(name, 32)}";
            }
        }
    }

    public class LineCountdownCancel
    {
        public const uint LogFileLineID = 269;
        private ILogger logger;
        private OverlayPluginLogLineConfig opcodeConfig;
        private IOpcodeConfigEntry opcode = null;
        private readonly int offsetMessageType;
        private readonly int offsetPacketData;
        private readonly FFXIVRepository ffxiv;

        private Func<string, DateTime, bool> logWriter;

        public LineCountdownCancel(TinyIoCContainer container)
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
                Name = "CountdownCancel",
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
                opcode = opcodeConfig["CountdownCancel"];
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
                    CountdownCancel_v655 countdownCancelPacket = *(CountdownCancel_v655*)&buffer[offsetPacketData];
                    logWriter(countdownCancelPacket.ToString(), serverTime);

                    return;
                }
            }
        }
    }
}
