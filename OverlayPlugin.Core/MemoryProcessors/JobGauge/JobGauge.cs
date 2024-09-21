using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    public abstract class JobGaugeMemory : IJobGaugeMemory
    {
        public struct JobGaugeImpl : IJobGauge
        {
            [JsonIgnore]
            public JobGaugeJob job;
            [JsonIgnore]
            public IBaseJobGauge data;
            [JsonIgnore]
            public byte[] rawData;
            [JsonIgnore]
            public object baseObject;

            public JobGaugeJob Job => job;
            public IBaseJobGauge Data => data;
            public int[] RawData => rawData.Select((b) => (int)b).ToArray();
            public object BaseObject => baseObject;

            public bool Equals(IJobGauge obj)
            {
                if (obj == null || GetType() != obj.GetType())
                {
                    return false;
                }

                var objCast = obj;

                if (objCast.Job != Job) return false;
                var objRawData = objCast.RawData;
                var rawData = RawData;
                if (objRawData.Length != rawData.Length) return false;
                for (int i = 0; i < objRawData.Length; ++i)
                {
                    if (objRawData[i] != rawData[i]) return false;
                }
                return true;
            }
        }

        protected FFXIVMemory memory;
        private ILogger logger;

        protected IntPtr jobGaugeAddress = IntPtr.Zero;

        private string jobDataSignature;
        private int jobDataSignatureOffset;

        public JobGaugeMemory(TinyIoCContainer container, string jobDataSignature, int jobDataSignatureOffset)
        {
            this.jobDataSignature = jobDataSignature;
            this.jobDataSignatureOffset = jobDataSignatureOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        private void ResetPointers()
        {
            jobGaugeAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (jobGaugeAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                return false;

            return true;
        }

        public void ScanPointers()
        {
            ResetPointers();
            if (!memory.IsValid())
                return;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(jobDataSignature, jobDataSignatureOffset, true);
            if (list != null && list.Count > 0)
            {
                jobGaugeAddress = list[0];
            }
            else
            {
                jobGaugeAddress = IntPtr.Zero;
                fail.Add(nameof(jobGaugeAddress));
            }

            logger.Log(LogLevel.Debug, "jobGaugeAddress: 0x{0:X}", jobGaugeAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found job Gauge memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find job Gauge memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();
        public abstract IJobGauge GetJobGauge();
    }
}
