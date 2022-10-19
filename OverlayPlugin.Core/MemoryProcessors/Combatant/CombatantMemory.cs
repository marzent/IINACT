using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    public abstract class CombatantMemory : ICombatantMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;

        private IntPtr charmapAddress = IntPtr.Zero;

        private string charmapSignature;

        private int numMemoryCombatants;
        private int combatantSize;
        private int effectSize;

        // Constants.
        protected const uint emptyID = 0xE0000000;

        public CombatantMemory(TinyIoCContainer container, string charmapSignature, int combatantSize, int effectSize, int numMemoryCombatants = 421)
        {
            this.charmapSignature = charmapSignature;
            this.combatantSize = combatantSize;
            this.effectSize = effectSize;
            this.numMemoryCombatants = numMemoryCombatants;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
        }

        private void ResetPointers()
        {
            charmapAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (charmapAddress == IntPtr.Zero)
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

            List<IntPtr> list = memory.SigScan(charmapSignature, 0, true);
            if (list != null && list.Count > 0)
            {
                charmapAddress = list[0];
            }
            else
            {
                charmapAddress = IntPtr.Zero;
                fail.Add(nameof(charmapAddress));
            }

            logger.Log(LogLevel.Debug, "charmapAddress: 0x{0:X}", charmapAddress.ToInt64());

            Combatant c = GetSelfCombatant();
            if (c != null)
            {
                logger.Log(LogLevel.Debug, "MyCharacter: '{0}' (0x{1:X})", c.Name, c.ID);
            }

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found combatant memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find combatant memory via {GetType().Name}: {string.Join(",", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        public Combatant GetSelfCombatant()
        {
            IntPtr address = memory.ReadIntPtr(charmapAddress);
            if (address == IntPtr.Zero)
                return null;
            byte[] source = memory.GetByteArray(address, combatantSize);
            return GetCombatantFromByteArray(source, 0, true, true);
        }

        public Combatant GetCombatantFromAddress(IntPtr address, uint selfCharID = 0)
        {
            byte[] c = memory.GetByteArray(address, combatantSize);
            return GetCombatantFromByteArray(c, selfCharID, false);
        }

        public unsafe List<Combatant> GetCombatantList()
        {
            var result = new List<Combatant>();
            var seen = new HashSet<uint>();
            var mychar = GetSelfCombatant();

            // Int64 pointer size
            const int sz = 8;
            byte[] source = memory.GetByteArray(charmapAddress, sz * numMemoryCombatants);
            if (source == null || source.Length == 0)
                return result;

            for (int i = 0; i < numMemoryCombatants; i++)
            {
                IntPtr p;
                fixed (byte* bp = source) p = new IntPtr(*(Int64*)&bp[i * sz]);

                if (p == IntPtr.Zero)
                    continue;

                byte[] c = memory.GetByteArray(p, combatantSize);
                Combatant combatant = GetMobFromByteArray(c, mychar == null ? 0 : mychar.ID);
                if (combatant == null)
                    continue;
                if (seen.Contains(combatant.ID))
                    continue;

                // TODO: should this just be a dictionary? there are a lot of id lookups.
                result.Add(combatant);
                seen.Add(combatant.ID);
            }

            return result;
        }

        // Returns a combatant if the combatant is a mob or a PC.
        protected abstract unsafe Combatant GetMobFromByteArray(byte[] source, uint mycharID);

        // Will return any kind of combatant, even if not a mob.
        // This function always returns a combatant object, even if empty.
        protected abstract unsafe Combatant GetCombatantFromByteArray(byte[] source, uint mycharID, bool isPlayer, bool exceptEffects = false);

        protected unsafe List<EffectEntry> GetEffectEntries(byte* source, ObjectType type, uint mycharID)
        {
            var result = new List<EffectEntry>();
            int maxEffects = (type == ObjectType.PC) ? 30 : 60;
            var size = EffectMemory.Size * maxEffects;

            var bytes = new byte[size];
            Marshal.Copy((IntPtr)source, bytes, 0, size);

            for (int i = 0; i < maxEffects; i++)
            {
                var effect = GetEffectEntryFromByteArray(bytes, i);

                if (effect.BuffID > 0 &&
                    effect.Stack >= 0 &&
                    effect.Timer >= 0.0f &&
                    effect.ActorID > 0)
                {
                    effect.isOwner = effect.ActorID == mycharID;

                    result.Add(effect);
                }
            }

            return result;
        }

        protected unsafe EffectEntry GetEffectEntryFromByteArray(byte[] source, int num = 0)
        {
            fixed (byte* p = source)
            {
                EffectMemory mem = *(EffectMemory*)&p[num * EffectMemory.Size];

                EffectEntry effectEntry = new EffectEntry()
                {
                    BuffID = mem.BuffID,
                    Stack = mem.Stack,
                    Timer = mem.Timer,
                    ActorID = mem.ActorID,
                    isOwner = false,
                };

                return effectEntry;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        public struct EffectMemory
        {
            public const int Size = 12;

            [FieldOffset(0)]
            public ushort BuffID;

            [FieldOffset(2)]
            public ushort Stack;

            [FieldOffset(4)]
            public float Timer;

            [FieldOffset(8)]
            public uint ActorID;
        }
    }
}
