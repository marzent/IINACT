using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage
{
    public abstract class AtkStageMemory
    {
        protected FFXIVMemory memory;
        protected ILogger logger;

        protected IntPtr atkStageInstanceAddress = IntPtr.Zero;

        private long atkStageSingletonAddress;

        public AtkStageMemory(TinyIoCContainer container, long atkStageSingletonAddress)
        {
            this.atkStageSingletonAddress = atkStageSingletonAddress;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        private void ResetPointers()
        {
            atkStageInstanceAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (atkStageInstanceAddress == IntPtr.Zero)
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

            long instanceAddress = memory.GetInt64(new IntPtr(memory.GetBaseAddress().ToInt64() + atkStageSingletonAddress));

            if (instanceAddress != 0)
            {
                atkStageInstanceAddress = new IntPtr(instanceAddress);
            }
            else
            {
                atkStageInstanceAddress = IntPtr.Zero;
                fail.Add(nameof(atkStageInstanceAddress));
            }

            logger.Log(LogLevel.Debug, "atkStageInstanceAddress: 0x{0:X}", atkStageInstanceAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found atkStage memory via {GetType().Name}.");
                return;
            }

            // @TODO: Change this from Debug to Error once we're actually using atkStage
            logger.Log(LogLevel.Debug, $"Failed to find atkStage memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        public IntPtr GetPointer()
        {
            if (!IsValid())
                return IntPtr.Zero;
            return atkStageInstanceAddress;
        }
    }
}
