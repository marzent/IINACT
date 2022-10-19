using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    public interface ITargetMemory
    {
        Combatant.Combatant GetTargetCombatant();

        Combatant.Combatant GetFocusCombatant();

        Combatant.Combatant GetHoverCombatant();

        bool IsValid();
    }

    class TargetMemoryManager : ITargetMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        private ITargetMemory memory = null;

        public TargetMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<ITargetMemory60, TargetMemory60>();
            repository = container.Resolve<FFXIVRepository>();
        }

        private void FindMemory()
        {
            List<ITargetMemory> candidates = new List<ITargetMemory>();
            candidates.Add(container.Resolve<ITargetMemory60>());

            foreach (var c in candidates)
            {
                if (c.IsValid())
                {
                    memory = c;
                    break;
                }
            }
        }

        public bool IsValid()
        {
            if (memory == null)
            {
                FindMemory();
            }
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
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
