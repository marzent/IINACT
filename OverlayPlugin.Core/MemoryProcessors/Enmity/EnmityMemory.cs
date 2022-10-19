using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using RainbowMage.OverlayPlugin.MemoryProcessors.Combatant;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Enmity
{
    public abstract class EnmityMemory : IEnmityMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;
        private ICombatantMemory combatantMemory;

        private IntPtr enmityAddress = IntPtr.Zero;

        private string enmitySignature;
        private int enmitySignatureOffset;

        public EnmityMemory(TinyIoCContainer container, string enmitySignature, int enmitySignatureOffset)
        {
            this.enmitySignature = enmitySignature;
            this.enmitySignatureOffset = enmitySignatureOffset;
            logger = container.Resolve<ILogger>();
            memory = container.Resolve<FFXIVMemory>();
            combatantMemory = container.Resolve<ICombatantMemory>();
        }

        private void ResetPointers()
        {
            enmityAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (enmityAddress == IntPtr.Zero)
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

            List<IntPtr> list = memory.SigScan(enmitySignature, 0, true);
            if (list != null && list.Count > 0)
            {
                enmityAddress = IntPtr.Add(list[0], enmitySignatureOffset);
            }
            else
            {
                enmityAddress = IntPtr.Zero;
                fail.Add(nameof(enmityAddress));
            }

            logger.Log(LogLevel.Debug, "enmityAddress: 0x{0:X}", enmityAddress.ToInt64());

            if (fail.Count == 0)
            {
                logger.Log(LogLevel.Info, $"Found enmity memory via {GetType().Name}.");
                return;
            }

            logger.Log(LogLevel.Error, $"Failed to find enmity memory via {GetType().Name}: {string.Join(", ", fail)}.");
            return;
        }

        public abstract Version GetVersion();

        [StructLayout(LayoutKind.Explicit, Size = Size)]
        struct MemoryEnmityListEntry
        {
            public const int Size = 8;

            [FieldOffset(0x00)]
            public uint ID;

            [FieldOffset(0x04)]
            public uint Enmity;
        }

        [StructLayout(LayoutKind.Explicit)]
        unsafe struct MemoryEnmityList
        {
            public const int MaxEntries = 32;
            public static int Size => Marshal.SizeOf(typeof(MemoryEnmityList));

            [FieldOffset(0x00)]
            public fixed byte EntryBuffer[MaxEntries];

            [FieldOffset(0x100)]
            public short Count;

            public MemoryEnmityListEntry this[int index]
            {
                get
                {
                    if (index >= Count)
                    {
                        return new MemoryEnmityListEntry();
                    }
                    fixed (byte* p = EntryBuffer)
                    {
                        return *(MemoryEnmityListEntry*)&p[index * MemoryEnmityListEntry.Size];
                    }
                }
            }
        }

        private unsafe MemoryEnmityList ReadEnmityList()
        {
            byte[] source = memory.GetByteArray(enmityAddress, MemoryEnmityList.Size);
            fixed (byte* p = source)
            {
                return *(MemoryEnmityList*)&p[0];
            }
        }

        public List<EnmityEntry> GetEnmityEntryList(List<Combatant.Combatant> combatantList)
        {
            if (!IsValid() || !combatantMemory.IsValid())
            {
                return new List<EnmityEntry>();
            }

            var mychar = combatantMemory.GetSelfCombatant();

            uint topEnmity = 0;
            var result = new List<EnmityEntry>();

            MemoryEnmityList list = ReadEnmityList();
            for (int i = 0; i < list.Count; i++)
            {
                MemoryEnmityListEntry e = list[i];
                topEnmity = Math.Max(topEnmity, e.Enmity);

                Combatant.Combatant c = null;
                if (e.ID > 0)
                {
                    c = combatantList.Find(x => x.ID == e.ID);
                }

                var entry = new EnmityEntry()
                {
                    ID = e.ID,
                    Enmity = e.Enmity,
                    isMe = e.ID == mychar.ID,
                    Name = c == null ? "Unknown" : c.Name,
                    OwnerID = c == null ? 0 : c.OwnerID,
                    HateRate = (int)(((double)e.Enmity / (double)topEnmity) * 100),
                    Job = c == null ? (byte)0 : c.Job,
                };

                result.Add(entry);
            }
            return result;
        }

    }
}
