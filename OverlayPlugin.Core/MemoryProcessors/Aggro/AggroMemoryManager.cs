using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro
{
    public interface IAggroMemory : IVersionedMemory
    {
        List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList);
    }

    public class AggroMemoryManager : IAggroMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private IAggroMemory memory = null;

        public AggroMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IAggroMemory60, AggroMemory60>();
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
            List<IAggroMemory> candidates = new List<IAggroMemory>();
            candidates.Add(container.Resolve<IAggroMemory60>());
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

        public List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList)
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetAggroList(combatantList);
        }
    }
}
