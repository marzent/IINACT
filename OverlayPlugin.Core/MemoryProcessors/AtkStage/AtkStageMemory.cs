using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage
{
    public abstract class AtkStageMemory
    {
        protected FFXIVMemory memory;
        protected ILogger logger;

        public AtkStageMemory(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        public bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            return true;
        }

        public void ScanPointers() { }

        public abstract Version GetVersion();
    }
}
