using System;
using System.Collections.Generic;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro
{
    [Serializable]
    public class AggroEntry
    {
        public uint ID;
        public string Name;
        public int HateRate;
        public int Order;
        public bool isCurrentTarget;
        public bool IsTargetable;

        public int CurrentHP;
        public int MaxHP;

        // Target of Enemy
        public EnmityEntry Target;

        // Effects
        public List<EffectEntry> Effects;
    }
}
