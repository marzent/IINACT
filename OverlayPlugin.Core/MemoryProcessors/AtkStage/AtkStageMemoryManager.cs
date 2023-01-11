using System;
using System.Collections.Generic;
using System.Diagnostics;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkGui.FFXIVClientStructs;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage
{
    public interface IAtkStageMemory : IVersionedMemory
    {
        IntPtr GetAddonAddress(string name);
        dynamic GetAddon(string name);
    }

    class AtkStageMemoryManager : IAtkStageMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IAtkStageMemory memory = null;

        public AtkStageMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IAtkStageMemory62, AtkStageMemory62>();
            repository = container.Resolve<FFXIVRepository>();

            var memory = container.Resolve<FFXIVMemory>();
            memory.RegisterOnProcessChangeHandler(FindMemory);
        }

        private void FindMemory(object sender, Process p)
        {
            memory = null;
            if (p == null)
            {
                return;
            }
            ScanPointers();
        }

        public void ScanPointers()
        {
            List<IAtkStageMemory> candidates = new List<IAtkStageMemory>();
            candidates.Add(container.Resolve<IAtkStageMemory62>());
            memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
        }

        public bool IsValid()
        {
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        Version IVersionedMemory.GetVersion()
        {
            if (!IsValid())
                return null;
            return memory.GetVersion();
        }

        public IntPtr GetAddonAddress(string name)
        {
            if (!IsValid())
            {
                return IntPtr.Zero;
            }
            return memory.GetAddonAddress(name);
        }

        public dynamic GetAddon(string name)
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetAddon(name);
        }
    }
}
