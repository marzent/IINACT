using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Machina.FFXIV;

// To test `DisplayLogMessage`, you can:
// Open a treasure coffer that gives you item(s):
// 274|2024-01-10T19:28:37.5000000-05:00|10001234|020F|04D0|0|93E0|0|0|0|d274429622d0c27e
// 274|2024-01-10T19:28:37.5000000-05:00|10001234|020F|04D0|0|93F3|0|0|0|d274429622d0c27e
// 00|2024-01-10T19:28:36.0000000-05:00|0A3E||You obtain a windswept shamshir.|92337ce2a33e52f8
// 00|2024-01-10T19:28:36.0000000-05:00|0A3E||You obtain a windswept shield.|a48cbf20d0255d4e
// Sell TT cards for MGP:
// 274|2024-01-10T20:08:35.3520000-05:00|10001234|020F|129D|0|320|0|0|0|d274429622d0c27e
// 00|2024-01-10T20:08:35.0000000-05:00|0A3E||You obtain 800 MGP.|f768dc4f098c15a6
// Die in Eureka with a `Spirit of the Remembered` active:
// 274|2024-02-15T19:35:41.9950000-05:00|10001234|020F|236D|0|669|0|0|0|d274429622d0c27e
// 00|2024-02-15T19:35:41.0000000-05:00|0939||The memories of heroes past live on again!|bb3bfbfc487ad4e9

// To test `DisplayLogMessageParams`, you can play a Gold Saucer minigame:
// 274|2024-03-21T20:45:41.3680000-04:00|10001234|0210|129D|10001234|F|0|0|0|d274429622d0c27e
// 00|2024-03-21T20:45:40.0000000-04:00|08BE||You obtain 15 MGP.|97702e809544a633

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineActorControlSelfExtra
    {
        public const uint LogFileLineID = 274;
        private ILogger logger;
        private readonly FFXIVRepository ffxiv;

        // Any category defined in this array will be allowed as an emitted line
        public static readonly Server_ActorControlCategory[] AllowedActorControlCategories = {
            // Some `LogMessage` messages can be triggered by both 0x020F and 0x0210 categories, not sure what the difference is
            // except that 0x0210 messages usually have another actor ID in the parameters
            Server_ActorControlCategory.DisplayLogMessage,
            Server_ActorControlCategory.DisplayLogMessageParams,
        };

        private class RegionalizedInfo
        {
            public readonly int packetSize;
            public readonly int packetOpcode;
            public readonly int offsetMessageType;
            public readonly Type headerType;
            public readonly Type actorControlType;
            public readonly FieldInfo fieldCastSourceId;
            public readonly FieldInfo fieldCategory;
            public readonly FieldInfo fieldParam1;
            public readonly FieldInfo fieldParam2;
            public readonly FieldInfo fieldParam3;
            public readonly FieldInfo fieldParam4;
            public readonly FieldInfo fieldParam5;
            public readonly FieldInfo fieldParam6;

            public RegionalizedInfo(Type headerType, Type actorControlType, NetworkParser netHelper)
            {
                this.headerType = headerType;
                this.actorControlType = actorControlType;
                fieldCastSourceId = headerType.GetField("ActorID");
                fieldCategory = actorControlType.GetField("category");
                fieldParam1 = actorControlType.GetField("param1");
                fieldParam2 = actorControlType.GetField("param2");
                fieldParam3 = actorControlType.GetField("param3");
                fieldParam4 = actorControlType.GetField("param4");
                fieldParam5 = actorControlType.GetField("param5");
                fieldParam6 = actorControlType.GetField("param6");
                packetOpcode = netHelper.GetOpcode("ActorControlSelf");
                packetSize = Marshal.SizeOf(actorControlType);
                offsetMessageType = netHelper.GetOffset(headerType, "MessageType");
            }
        }

        private RegionalizedInfo regionalized;

        private readonly Func<string, DateTime, bool> logWriter;
        private readonly NetworkParser netHelper;

        public LineActorControlSelfExtra(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            netHelper = container.Resolve<NetworkParser>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "ActorControlSelfExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private void ProcessChanged(Process process)
        {
            GameRegion region = ffxiv.GetMachinaRegion();
            if (!ffxiv.IsFFXIVPluginPresent())
                return;
            try
            {
                Assembly mach = Assembly.Load("Machina.FFXIV");
                Type headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                string actorControlTypeStr;
                switch (region)
                {
                    case GameRegion.Global:
                        {
                            actorControlTypeStr = "Machina.FFXIV.Headers.Server_ActorControlSelf";
                            break;
                        }
                    case GameRegion.Korean:
                        {
                            actorControlTypeStr = "Machina.FFXIV.Headers.Korean.Server_ActorControlSelf";
                            break;
                        }
                    case GameRegion.Chinese:
                        {
                            actorControlTypeStr = "Machina.FFXIV.Headers.Chinese.Server_ActorControlSelf";
                            break;
                        }
                    default:
                        {
                            return;
                        }
                }

                Type actorControlType = mach.GetType(actorControlTypeStr);
                RegionalizedInfo info = new RegionalizedInfo(headerType, actorControlType, netHelper);
                regionalized = info;
            }
            catch (System.IO.FileNotFoundException)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserNoFfxiv);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, Resources.NetworkParserInitException, e);
            }
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            RegionalizedInfo info = regionalized;
            if (info == null)
                return;

            if (message.Length < info.packetSize)
                return;

            fixed (byte* buffer = message)
            {
                if (*(ushort*)&buffer[info.offsetMessageType] == info.packetOpcode)
                {
                    object header = Marshal.PtrToStructure(new IntPtr(buffer), info.headerType);
                    UInt32 sourceId = (UInt32)info.fieldCastSourceId.GetValue(header);

                    object packet = Marshal.PtrToStructure(new IntPtr(buffer), info.actorControlType);
                    Server_ActorControlCategory category = (Server_ActorControlCategory)info.fieldCategory.GetValue(packet);

                    if (AllowedActorControlCategories.Contains(category))
                    {
                        UInt32 param1 = (UInt32)info.fieldParam1.GetValue(packet);
                        UInt32 param2 = (UInt32)info.fieldParam2.GetValue(packet);
                        UInt32 param3 = (UInt32)info.fieldParam3.GetValue(packet);
                        UInt32 param4 = (UInt32)info.fieldParam4.GetValue(packet);
                        UInt32 param5 = (UInt32)info.fieldParam5.GetValue(packet);
                        UInt32 param6 = (UInt32)info.fieldParam6.GetValue(packet);

                        string line = string.Format(CultureInfo.InvariantCulture,
                            "{0:X8}|{1:X4}|{2:X}|{3:X}|{4:X}|{5:X}|{6:X}|{7:X}",
                            sourceId, (ushort)category, param1, param2, param3, param4, param5, param6);

                        DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                        logWriter(line, serverTime);
                    }
                }
            }
        }
    }
}
