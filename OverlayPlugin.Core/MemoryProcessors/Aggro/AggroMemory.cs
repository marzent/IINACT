using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;
using RainbowMage.OverlayPlugin.MemoryProcessors.Enmity;
using RainbowMage.OverlayPlugin.MemoryProcessors.Target;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Aggro
{
    public abstract class AggroMemory : IAggroMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;
        private ICombatantMemory combatantMemory;
        private ITargetMemory targetMemory;

        private IntPtr aggroAddress = IntPtr.Zero;

        private string aggroSignature;
        private int aggroSignatureOffset;

        public AggroMemory(TinyIoCContainer container, string aggroSignature, int aggroSignatureOffset)
        {
            this.aggroSignature = aggroSignature;
            this.aggroSignatureOffset = aggroSignatureOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
            combatantMemory = container.Resolve<ICombatantMemory>();
            targetMemory = container.Resolve<ITargetMemory>();
        }

        private void ResetPointers()
        {
            aggroAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (aggroAddress == IntPtr.Zero)
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

            List<IntPtr> list = memory.SigScan(aggroSignature, 0, true);
            if (list != null && list.Count > 0)
            {
                aggroAddress = IntPtr.Add(list[0], aggroSignatureOffset);
            }
            else
            {
                aggroAddress = IntPtr.Zero;
                fail.Add(nameof(aggroAddress));
            }

            logger.Log(LogLevel.Debug, "aggroAddress: 0x{0:X}", aggroAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found aggro memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find aggro memory via {GetType().Name}: {string.Join(",", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        struct MemoryAggroListEntry
        {
            public const int Size = 72;

            [FieldOffset(0x38)]
            public uint ID;

            [FieldOffset(0x3C)]
            public uint Enmity;
        }

        // @TODO: This seems a bit off.
        // 72*31 = 0x8B8
        // 72*32 = 0x900
        // Seems like this should be MaxEntries = 32, Size = 0x902, Count FieldOffset = 0x900?
        [StructLayout(LayoutKind.Explicit, Size = 0x900)]
        unsafe struct MemoryAggroList
        {
            public const int MaxEntries = 31;
            public static int Size => Marshal.SizeOf(typeof(MemoryAggroList));

            [FieldOffset(0x00)]
            public fixed byte EntryBuffer[MaxEntries];

            [FieldOffset(0x8F8)]
            public short Count;

            public MemoryAggroListEntry this[int index]
            {
                get
                {
                    if (index >= Count)
                    {
                        return new MemoryAggroListEntry();
                    }
                    fixed (byte* p = EntryBuffer)
                    {
                        return *(MemoryAggroListEntry*)&p[index * MemoryAggroListEntry.Size];
                    }
                }
            }
        }

        private unsafe MemoryAggroList ReadAggroList()
        {
            byte[] source = memory.GetByteArray(aggroAddress, MemoryAggroList.Size);
            fixed (byte* p = source)
            {
                return *(MemoryAggroList*)&p[0];
            }
        }

        public List<AggroEntry> GetAggroList(List<Combatant.Combatant> combatantList)
        {
            if (!IsValid() || !combatantMemory.IsValid())
            {
                return new List<AggroEntry>();
            }

            var mychar = combatantMemory.GetSelfCombatant();

            uint currentTargetID = 0;
            var targetCombatant = targetMemory.GetTargetCombatant();
            if (targetCombatant != null)
            {
                currentTargetID = targetCombatant.ID;
            }

            var result = new List<AggroEntry>();

            var list = ReadAggroList();
            for (int i = 0; i < list.Count; i++)
            {
                MemoryAggroListEntry e = list[i];
                if (e.ID <= 0)
                    continue;
                Combatant.Combatant c = combatantList.Find(x => x.ID == e.ID);
                if (c == null)
                    continue;

                var entry = new AggroEntry()
                {
                    ID = e.ID,
                    // Rather than storing enmity, this is hate rate for the aggro list.
                    // This is likely because we're reading the memory for the aggro sidebar.
                    HateRate = (int)e.Enmity,
                    isCurrentTarget = (e.ID == currentTargetID),
                    IsTargetable = c.IsTargetable,
                    Name = c.Name,
                    MaxHP = c.MaxHP,
                    CurrentHP = c.CurrentHP,
                    Effects = c.Effects,
                };

                // TODO: it seems like when your chocobo has aggro, this entry
                // is you, and not your chocobo.  It's not clear if there's
                // anything that can be done about it.
                if (c.TargetID > 0)
                {
                    Combatant.Combatant t = combatantList.Find(x => x.ID == c.TargetID);
                    if (t != null)
                    {
                        entry.Target = new EnmityEntry()
                        {
                            ID = t.ID,
                            Name = t.Name,
                            OwnerID = t.OwnerID,
                            isMe = mychar.ID == t.ID ? true : false,
                            Enmity = 0,
                            HateRate = 0,
                            Job = t.Job,
                        };
                    }
                }
                result.Add(entry);
            }
            return result;
        }
    }
}
