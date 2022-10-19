using System;
using System.Collections.Generic;
using System.Diagnostics;
using RainbowMage.OverlayPlugin.MemoryProcessors.Aggro;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    public interface ICombatantMemory : IVersionedMemory
    {
        Combatant GetSelfCombatant();
        Combatant GetCombatantFromAddress(IntPtr address, uint selfCharID);
        List<Combatant> GetCombatantList();
    }

    public class CombatantMemoryManager : ICombatantMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private ICombatantMemory memory = null;

        public CombatantMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<ICombatantMemory61, CombatantMemory61>();
            container.Register<ICombatantMemory62, CombatantMemory62>();
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
            List<ICombatantMemory> candidates = new List<ICombatantMemory>();
            candidates.Add(container.Resolve<ICombatantMemory61>());
            candidates.Add(container.Resolve<ICombatantMemory62>());
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

        public Combatant GetCombatantFromAddress(IntPtr address, uint selfCharID)
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetCombatantFromAddress(address, selfCharID);
        }

        public List<Combatant> GetCombatantList()
        {
            if (!IsValid())
            {
                return new List<Combatant>();
            }
            return memory.GetCombatantList();
        }

        public Combatant GetSelfCombatant()
        {
            if (!IsValid())
            {
                return null;
            }
            return memory.GetSelfCombatant();
        }
    }
}
