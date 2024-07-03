using System;
using System.Diagnostics;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper
{
    abstract class LineBaseSubMachina<PacketType>
        where PacketType : MachinaPacketWrapper, new()
    {
        protected static FFXIVRepository ffxiv;

        protected readonly Func<string, DateTime, bool> logWriter;
        protected MachinaRegionalizedPacketHelper<PacketType> packetHelper;
        protected GameRegion? currentRegion;

        public LineBaseSubMachina(TinyIoCContainer container, uint logFileLineID, string logLineName, string machinaPacketName)
        {
            ffxiv = ffxiv ?? container.Resolve<FFXIVRepository>();
            ffxiv.RegisterNetworkParser(MessageReceived);
            ffxiv.RegisterProcessChangedHandler(ProcessChanged);

            if (MachinaRegionalizedPacketHelper<PacketType>.Create(machinaPacketName, out packetHelper))
            {
                var customLogLines = container.Resolve<FFXIVCustomLogLines>();
                logWriter = customLogLines.RegisterCustomLogLine(new LogLineRegistryEntry()
                {
                    Name = logLineName,
                    Source = "OverlayPlugin",
                    ID = logFileLineID,
                    Version = 1,
                });
            }
            else
            {
                var logger = container.Resolve<ILogger>();
                logger.Log(LogLevel.Error, $"Failed to initialize {logFileLineID}: Failed to create {machinaPacketName} packet helper from Machina structs");
            }
        }

        protected virtual void ProcessChanged(Process process)
        {
            if (!ffxiv.IsFFXIVPluginPresent())
                return;

            currentRegion = null;
        }

        protected virtual unsafe void MessageReceived(string id, long epoch, byte[] message)
        {
            if (packetHelper == null)
                return;

            if (currentRegion == null)
                currentRegion = ffxiv.GetMachinaRegion();

            if (currentRegion == null)
                return;

            var line = packetHelper[currentRegion.Value].ToString(epoch, message);

            if (line != null)
            {
                DateTime serverTime = ffxiv.EpochToDateTime(epoch);
                logWriter(line, serverTime);
            }
        }
    }
}
