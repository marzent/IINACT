using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;

/*
At initial zone-in of Barbariccia normal, there are 9 actors that spawn with `animationState` of `01`:
272|2024-03-02T15:45:44.2260000-05:00|4000226B|E0000000|0000|01|89d2d9b95839548f
272|2024-03-02T15:45:44.2260000-05:00|4000226D|E0000000|0000|01|b5e6a59cc0b2c1f3
272|2024-03-02T15:45:44.2260000-05:00|4000226E|E0000000|0000|01|0864edbcb74f9721
272|2024-03-02T15:45:44.2260000-05:00|4000226F|E0000000|0000|01|56b1f6fddd97dede
272|2024-03-02T15:45:44.2260000-05:00|40002270|E0000000|0000|01|036aae0ca3f7ebda
272|2024-03-02T15:45:44.2260000-05:00|40002271|E0000000|0000|01|1396bfae913f5832
272|2024-03-02T15:45:44.2260000-05:00|4000226C|E0000000|0000|01|a11c9d39fdbeeb7a
272|2024-03-02T15:45:44.2260000-05:00|4000226A|E0000000|0000|01|0fef26e4fb626dda
272|2024-03-02T15:45:44.2700000-05:00|40002272|E0000000|0000|01|55ca54ac2707553a
03|2024-03-02T15:45:44.4890000-05:00|4000226C|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|898c4e2990d59eec
03|2024-03-02T15:45:44.4890000-05:00|40002271|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|b0a23f029b8f2da5
03|2024-03-02T15:45:44.4890000-05:00|40002270|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|c46da1ddfd7e3ed2
03|2024-03-02T15:45:44.4890000-05:00|4000226A|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|723a0d74b51582c2
03|2024-03-02T15:45:44.4890000-05:00|40002272|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|890e57d3f5d9809d
03|2024-03-02T15:45:44.4890000-05:00|4000226B|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|16e2d766f065a0fd
03|2024-03-02T15:45:44.4890000-05:00|4000226F|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|0391ea7f37b7098f
03|2024-03-02T15:45:44.4890000-05:00|4000226E|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|df9cee50012d0b5d
03|2024-03-02T15:45:44.4890000-05:00|4000226D|Barbariccia|00|5A|0000|00||11398|14802|69200|69200|10000|10000|||100.00|100.00|0.00|0.00|70bf7f0124bc77c7
 */

/*
At initial zone-in of P10N, the primary `Pandæmonium` actor spawns with an animation state set to `01`:
272|2024-03-02T18:29:17.2380000-05:00|40013FCB|E0000000|0000|01|57876e5fcb020437
261|2024-03-02T18:29:16.9350000-05:00|Add|40013FCB|BNpcID|3F19|BNpcNameID|3042|CastTargetID|E0000000|CurrentMP|10000|CurrentWorldID|65535|Heading|0.0000|Level|90|MaxHP|24437980|MaxMP|10000|ModelStatus|2304|Name|Pandæmonium|NPCTargetID|E0000000|PosX|100.0000|PosY|65.0000|Radius|35.0000|Type|2|WorldID|65535|8f2b51b8e66ccb68
This is later cleared to a state of `00` via `ActorControl` `SetAnimationState`:
273|2024-03-02T18:36:55.8380000-05:00|40013FCB|003E|00000000|00000000|00000000|00000000|1be7b98bf67c8479
 */

namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public class LineSpawnNpcExtra
    {
        public const uint LogFileLineID = 272;
        private ILogger logger;
        private readonly FFXIVRepository ffxiv;

        private readonly Func<string, DateTime, bool> logWriter;
        private readonly NetworkParser netHelper;

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct Server_NpcSpawn
        {
            [FieldOffset(0x78)]
            public uint parentActorId;

            [FieldOffset(0x9A)]
            public ushort tetherId;

            [FieldOffset(0xB3)]
            public byte animationState;
        }

        private class RegionalizedInfo
        {
            public readonly int packetSize;
            public readonly int packetOpcode;
            public readonly int offsetMessageType;
            public readonly Type headerType;
            public readonly FieldInfo fieldCastSourceId;

            public RegionalizedInfo(Type headerType, NetworkParser netHelper)
            {
                this.headerType = headerType;
                fieldCastSourceId = headerType.GetField("ActorID");
                packetOpcode = netHelper.GetOpcode("NpcSpawn");
                packetSize = Marshal.SizeOf(typeof(Server_NpcSpawn));
                offsetMessageType = netHelper.GetOffset(headerType, "MessageType");
            }
        }

        private RegionalizedInfo regionalized;

        public LineSpawnNpcExtra(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            ffxiv = container.Resolve<FFXIVRepository>();
            netHelper = container.Resolve<NetworkParser>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            var customLogLines = container.Resolve<FFXIVCustomLogLines>();
            logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
            {
                Name = "NpcSpawnExtra",
                Source = "OverlayPlugin",
                ID = LogFileLineID,
                Version = 1,
            });
        }

        private void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            try
            {
                Assembly mach = Assembly.Load("Machina.FFXIV");
                Type headerType = mach.GetType("Machina.FFXIV.Headers.Server_MessageHeader");
                regionalized = new RegionalizedInfo(headerType, netHelper);
            }
            catch
            {
            }
        }

        private unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            var info = regionalized;
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

                    var packet = Marshal.PtrToStructure<Server_NpcSpawn>(new IntPtr(buffer));
                    var parentActorId = packet.parentActorId;
                    var tetherId = packet.tetherId;
                    var animationState = packet.animationState;

                    string line = string.Format(CultureInfo.InvariantCulture,
                        "{0:X8}|{1:X8}|{2:X4}|{3:X2}",
                        sourceId, parentActorId, tetherId, animationState);

                    DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                    logWriter(line, serverTime);
                }
            }
        }
    }
}