using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.InCombat
{
    interface IInCombatMemory61 : IInCombatMemory { }

    class InCombatMemory61 : InCombatMemory, IInCombatMemory61
    {
        private const string inCombatSignature = "803D????????000F95C04883C428";
        private const int inCombatSignatureOffset = -12;
        private const int inCombatRIPOffset = 1;
        public InCombatMemory61(TinyIoCContainer container) : base(container, inCombatSignature, inCombatSignatureOffset, inCombatRIPOffset) { }

        public override Version GetVersion()
        {
            return new Version(6, 1);
        }

    }
}
