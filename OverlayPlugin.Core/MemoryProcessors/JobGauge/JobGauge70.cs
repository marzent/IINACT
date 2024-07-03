using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    interface IJobGaugeMemory70 : IJobGaugeMemory { }

    partial class JobGaugeMemory70 : JobGaugeMemory, IJobGaugeMemory70
    {
        private static string jobDataSignature = "488B3D????????33ED";
        private static int jobDataSignatureOffset = -6;

        public JobGaugeMemory70(TinyIoCContainer container)
                : base(container, jobDataSignature, jobDataSignatureOffset)
        { }

        public override Version GetVersion()
        {
            return new Version(7, 0);
        }

        public override IJobGauge GetJobGauge()
        {
            if (!IsValid()) return null;

            var jobGaugeManager = GetJobGaugeManager();

            var ret = new JobGaugeImpl();

            ret.baseObject = jobGaugeManager;
            ret.job = (JobGaugeJob)jobGaugeManager.ClassJobID;
            ret.rawData = jobGaugeManager.GetRawGaugeData;

            switch (ret.job)
            {
                case JobGaugeJob.WHM: ret.data = jobGaugeManager.WhiteMage; break;
                case JobGaugeJob.SCH: ret.data = jobGaugeManager.Scholar; break;
                case JobGaugeJob.AST: ret.data = jobGaugeManager.Astrologian; break;
                case JobGaugeJob.SGE: ret.data = jobGaugeManager.Sage; break;

                case JobGaugeJob.BRD: ret.data = jobGaugeManager.Bard; break;
                case JobGaugeJob.MCH: ret.data = jobGaugeManager.Machinist; break;
                case JobGaugeJob.DNC: ret.data = jobGaugeManager.Dancer; break;

                case JobGaugeJob.BLM: ret.data = jobGaugeManager.BlackMage; break;
                case JobGaugeJob.SMN: ret.data = jobGaugeManager.Summoner; break;
                case JobGaugeJob.RDM: ret.data = jobGaugeManager.RedMage; break;

                case JobGaugeJob.MNK: ret.data = jobGaugeManager.Monk; break;
                case JobGaugeJob.DRG: ret.data = jobGaugeManager.Dragoon; break;
                case JobGaugeJob.NIN: ret.data = jobGaugeManager.Ninja; break;
                case JobGaugeJob.SAM: ret.data = jobGaugeManager.Samurai; break;
                case JobGaugeJob.RPR: ret.data = jobGaugeManager.Reaper; break;

                case JobGaugeJob.DRK: ret.data = jobGaugeManager.DarkKnight; break;
                case JobGaugeJob.PLD: ret.data = jobGaugeManager.Paladin; break;
                case JobGaugeJob.WAR: ret.data = jobGaugeManager.Warrior; break;
                case JobGaugeJob.GNB: ret.data = jobGaugeManager.Gunbreaker; break;
            }

            return ret;
        }

        private unsafe JobGaugeManager GetJobGaugeManager()
        {
            var rawData = memory.GetByteArray(jobGaugeAddress, sizeof(JobGaugeManager));
            fixed (byte* buffer = rawData)
            {
                return (JobGaugeManager)Marshal.PtrToStructure(new IntPtr(buffer), typeof(JobGaugeManager));
            }
        }
    }
}
