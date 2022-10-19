using System;
using System.Collections.Generic;
using System.Diagnostics;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Target
{
    public abstract class TargetMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;
        private ICombatantMemory combatantMemory;

        private IntPtr targetAddress = IntPtr.Zero;

        private string targetSignature;

        // Offsets from the targetAddress to find the correct target type.
        private int targetTargetOffset;
        private int focusTargetOffset;
        private int hoverTargetOffset;

        public TargetMemory(TinyIoCContainer container, string targetSignature, int targetTargetOffset, int focusTargetOffset, int hoverTargetOffset)
        {
            this.targetSignature = targetSignature;
            this.targetTargetOffset = targetTargetOffset;
            this.focusTargetOffset = focusTargetOffset;
            this.hoverTargetOffset = hoverTargetOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
            memory.RegisterOnProcessChangeHandler(ResetPointers);
            combatantMemory = container.Resolve<ICombatantMemory>();
        }

        private void ResetPointers(object sender, Process p)
        {
            targetAddress = IntPtr.Zero;
            if (p != null)
                GetPointerAddress();
        }

        private bool HasValidPointers()
        {
            if (targetAddress == IntPtr.Zero)
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

        private bool GetPointerAddress()
        {
            if (!memory.IsValid())
                return false;

            List<string> fail = new List<string>();

            List<IntPtr> list = memory.SigScan(targetSignature, 0, true);
            if (list != null && list.Count > 0)
            {
                targetAddress = list[0];
            }
            else
            {
                targetAddress = IntPtr.Zero;
                fail.Add(nameof(targetAddress));
            }

            logger.Log(LogLevel.Debug, "targetAddress: 0x{0:X}", targetAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found target memory via {GetType().Name}.");
                return true;
            }

            logger.Log(LogLevel.Error, $"Failed to find target memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return false;
        }

        private Combatant.Combatant GetTargetRelativeCombatant(int offset)
        {
            IntPtr address = memory.ReadIntPtr(IntPtr.Add(targetAddress, offset));
            if (address == IntPtr.Zero)
                return null;

            return combatantMemory.GetCombatantFromAddress(address, 0);
        }

        public Combatant.Combatant GetTargetCombatant()
        {
            return GetTargetRelativeCombatant(targetTargetOffset);
        }

        public Combatant.Combatant GetFocusCombatant()
        {
            return GetTargetRelativeCombatant(focusTargetOffset);
        }

        public Combatant.Combatant GetHoverCombatant()
        {
            return GetTargetRelativeCombatant(hoverTargetOffset);
        }

    }
}
