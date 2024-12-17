using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    public interface ITargetMemory : IVersionedMemory
    {
        Combatant.Combatant GetTargetCombatant();

        Combatant.Combatant GetFocusCombatant();

        Combatant.Combatant GetHoverCombatant();
    }

    class TargetMemoryManager : ITargetMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private ITargetMemory memory = null;

        public TargetMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<ITargetMemory70, TargetMemory70>();
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
            List<ITargetMemory> candidates = new List<ITargetMemory>();
            candidates.Add(container.Resolve<ITargetMemory70>());
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

        public Combatant.Combatant GetTargetCombatant()
        {
            if (!IsValid())
            {
                return null;
            }

            return memory.GetTargetCombatant();
        }

        public Combatant.Combatant GetFocusCombatant()
        {
            if (!IsValid())
            {
                return null;
            }

            return memory.GetFocusCombatant();
        }

        public Combatant.Combatant GetHoverCombatant()
        {
            if (!IsValid())
            {
                return null;
            }

            return memory.GetHoverCombatant();
        }
    }
}
