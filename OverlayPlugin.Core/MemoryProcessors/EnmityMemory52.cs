using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors
{
    public class EnmityMemory52 : EnmityMemory
    {
        private FFXIVMemory memory;
        private ILogger logger;
        private DateTime lastSigScan = DateTime.MinValue;
        private DateTimeOffset lastDateTimeDynamicAddressChecked = DateTimeOffset.UtcNow;
        private uint loggedScanErrors = 0;

        private IntPtr charmapAddress = IntPtr.Zero;
        private IntPtr targetAddress = IntPtr.Zero;
        private IntPtr enmityAddress = IntPtr.Zero;
        private IntPtr aggroAddress = IntPtr.Zero;
        private IntPtr inCombatAddress = IntPtr.Zero;
        private IntPtr enmityHudAddress = IntPtr.Zero;
        private IntPtr enmityHudDynamicAddress = IntPtr.Zero;

        private const string charmapSignature = "48c1ea0381faa7010000????8bc2488d0d";
        private const string targetSignatureH0 = "83E901740832C04883C4205BC3488D0D"; // pre hotfix
        private const string targetSignatureH1 = "e8f2652f0084c00f8591010000488d0d"; // post hotfix 1
        private const string enmitySignature = "83f9ff7412448b048e8bd3488d0d";
        private const string inCombatSignature = "84c07425450fb6c7488d0d";
        private const string enmityHudSignature = "48895C246048897C2470488B3D";

        // Offsets from the signature to find the correct address.
        private const int charmapSignatureOffset = 0;
        private const int targetSignatureOffset = 0;
        private const int enmitySignatureOffset = -2608;
        private const int aggroEnmityOffset = -2336;
        private const int inCombatSignatureBaseOffset = 0;
        private const int inCombatSignatureOffsetOffset = 5;
        private const int enmityHudSignatureOffset = 0;
        private readonly int[] enmityHudPointerPath = new int[] { 0x30, 0x58, 0x98, 0x20 };

        // Offsets from the targetAddress to find the correct target type.
        private const int targetTargetOffset = 176;
        private const int focusTargetOffset = 248;
        private const int hoverTargetOffset = 208;

        // Offsets from the enmityHudAddress tof find various enmity HUD data structures.
        private const int enmityHudCountOffset = 4;
        private const int enmityHudEntryOffset = 20;

        // Constants.
        private const uint emptyID = 0xE0000000;
        private const int numMemoryCombatants = 421;

        public EnmityMemory52(TinyIoCContainer container)
        {
            this.memory = new FFXIVMemory(container);
            this.memory.OnProcessChange += ResetPointers;
            this.logger = container.Resolve<ILogger>();
            GetPointerAddress();
        }

        private void ResetPointers(object sender, EventArgs _)
        {
            charmapAddress = IntPtr.Zero;
            targetAddress = IntPtr.Zero;
            enmityAddress = IntPtr.Zero;
            aggroAddress = IntPtr.Zero;
            inCombatAddress = IntPtr.Zero;
        }

        private bool HasValidPointers()
        {
            if (charmapAddress == IntPtr.Zero)
                return false;
            if (targetAddress == IntPtr.Zero)
                return false;
            if (enmityAddress == IntPtr.Zero)
                return false;
            if (aggroAddress == IntPtr.Zero)
                return false;
            if (inCombatAddress == IntPtr.Zero)
                return false;
            return true;
        }

        public override bool IsValid()
        {
            if (!memory.IsValid())
                return false;

            if (!HasValidPointers())
                GetPointerAddress();

            if (!HasValidPointers())
                return false;

            return true;
        }

        private bool GetPointerAddress()
        {
            if (!memory.IsValid())
                return false;

            // Don't scan too often to avoid excessive CPU load
            if ((DateTime.Now - lastSigScan) < TimeSpan.FromSeconds(5))
                return false;

            lastSigScan = DateTime.Now;
            var success = true;
            var bRIP = true;

            var fail = new List<string>();

            /// CHARMAP
            var list = memory.SigScan(charmapSignature, 0, bRIP);
            if (list != null && list.Count > 0)
            {
                charmapAddress = list[0] + charmapSignatureOffset;
            }
            else
            {
                charmapAddress = IntPtr.Zero;
                fail.Add(nameof(charmapAddress));
                success = false;
            }

            // ENMITY
            list = memory.SigScan(enmitySignature, 0, bRIP);
            if (list != null && list.Count > 0)
            {
                enmityAddress = IntPtr.Add(list[0], enmitySignatureOffset);
                aggroAddress = IntPtr.Add(list[0], aggroEnmityOffset);
            }
            else
            {
                enmityAddress = IntPtr.Zero;
                aggroAddress = IntPtr.Zero;
                fail.Add(nameof(enmityAddress));
                fail.Add(nameof(aggroAddress));
                success = false;
            }

            /// TARGET
            list = memory.SigScan(targetSignatureH1, 0, bRIP);
            if (list == null || list.Count == 0)
                list = memory.SigScan(targetSignatureH0, 0, bRIP);

            if (list != null && list.Count > 0)
            {
                targetAddress = list[0] + targetSignatureOffset;
            }
            else
            {
                targetAddress = IntPtr.Zero;
                fail.Add(nameof(targetAddress));
                success = false;
            }

            /// IN COMBAT
            // The in combat address is set from a combination of two values, a base address and an offset.
            // They are found adjacent to the same signature, but at different offsets.
            var baseList = memory.SigScan(inCombatSignature, inCombatSignatureBaseOffset, bRIP);
            // SigScan returns pointers, but the offset is a 32-bit immediate value.  Do not use RIP.
            var offsetList = memory.SigScan(inCombatSignature, inCombatSignatureOffsetOffset, false);
            if (baseList != null && baseList.Count > 0 && offsetList != null && offsetList.Count > 0)
            {
                var baseAddress = baseList[0];
                var offset = (int)(((UInt64)offsetList[0]) & 0xFFFFFFFF);
                inCombatAddress = IntPtr.Add(baseAddress, offset);
            }
            else
            {
                inCombatAddress = IntPtr.Zero;
                fail.Add(nameof(inCombatAddress));
                success = false;
            }

            // EnmityList
            list = memory.SigScan(enmityHudSignature, 0, bRIP);
            if (list != null && list.Count == 1)
            {
                enmityHudAddress = list[0] + enmityHudSignatureOffset;

                if (enmityHudAddress == IntPtr.Zero)
                {
                    fail.Add(nameof(enmityHudAddress));
                    success = false;
                }
            }
            else
            {
                enmityHudAddress = IntPtr.Zero;
                fail.Add(nameof(enmityHudAddress));
                success = false;
            }

            logger.Log(LogLevel.Debug, "charmapAddress: 0x{0:X}", charmapAddress.ToInt64());
            logger.Log(LogLevel.Debug, "enmityAddress: 0x{0:X}", enmityAddress.ToInt64());
            logger.Log(LogLevel.Debug, "aggroAddress: 0x{0:X}", aggroAddress.ToInt64());
            logger.Log(LogLevel.Debug, "targetAddress: 0x{0:X}", targetAddress.ToInt64());
            logger.Log(LogLevel.Debug, "inCombatAddress: 0x{0:X}", inCombatAddress.ToInt64());
            logger.Log(LogLevel.Debug, "enmityListAddress: 0x{0:X}", enmityHudAddress.ToInt64());
            var c = GetSelfCombatant();
            if (c != null)
            {
                logger.Log(LogLevel.Debug, "MyCharacter: '{0}' (0x{1:X})", c.Name, c.ID);
            }

            if (!success)
            {
                if (loggedScanErrors < 10)
                {
                    logger.Log(LogLevel.Error, "Failed to find enmity memory for 5.2: {0}.", String.Join(",", fail));
                    loggedScanErrors++;

                    if (loggedScanErrors == 10)
                    {
                        logger.Log(LogLevel.Error, "Further enmity errors won't be logged.");
                    }
                }
            } else
            {
                logger.Log(LogLevel.Info, "Found enmity memory for 5.2.");
                loggedScanErrors = 0;
            }

            return success;
        }

        private Combatant GetTargetRelativeCombatant(int offset)
        {
            var address = memory.ReadIntPtr(IntPtr.Add(targetAddress, offset));
            if (address == IntPtr.Zero)
                return null;
            var source = memory.GetByteArray(address, CombatantMemory.Size);
            return GetCombatantFromByteArray(source, 0, false);
        }

        public override Combatant GetTargetCombatant()
        {
            return GetTargetRelativeCombatant(targetTargetOffset);
        }

        public override Combatant GetSelfCombatant()
        {
            var address = memory.ReadIntPtr(charmapAddress);
            if (address == IntPtr.Zero)
                return null;
            var source = memory.GetByteArray(address, CombatantMemory.Size);
            return GetCombatantFromByteArray(source, 0, true, true);
        }

        public override Combatant GetFocusCombatant()
        {
            return GetTargetRelativeCombatant(focusTargetOffset);
        }

        public override Combatant GetHoverCombatant()
        {
            return GetTargetRelativeCombatant(hoverTargetOffset);
        }

        public override unsafe List<Combatant> GetCombatantList()
        {
            var result = new List<Combatant>();
            var seen = new HashSet<uint>();
            var mychar = GetSelfCombatant();

            var sz = 8;
            var source = memory.GetByteArray(charmapAddress, sz * numMemoryCombatants);
            if (source == null || source.Length == 0)
                return result;

            for (var i = 0; i < numMemoryCombatants; i++)
            {
                IntPtr p;
                fixed (byte* bp = source) p = new IntPtr(*(Int64*)&bp[i * sz]);

                if (p == IntPtr.Zero)
                    continue;

                var c = memory.GetByteArray(p, CombatantMemory.Size);
                var combatant = GetMobFromByteArray(c, mychar == null ? 0 : mychar.ID);
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
        public unsafe Combatant GetMobFromByteArray(byte[] source, uint mycharID)
        {
            fixed (byte* p = source)
            {
                var mem = *(CombatantMemory*)&p[0];
                var type = (ObjectType)mem.Type;
                if (type != ObjectType.PC && type != ObjectType.Monster)
                    return null;
                if (mem.ID == 0 || mem.ID == emptyID)
                    return null;
            }
            return GetCombatantFromByteArray(source, mycharID, false);
        }

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct CombatantMemory
        {
            public static int Size => Marshal.SizeOf(typeof(CombatantMemory));

            // Unknown size, but this is the bytes up to the next field.
            public const int nameBytes = 68;

            // (effect container size: 12) * (Max. effects: 60)
            public const int effectBytes = 720;

            [FieldOffset(0x30)]
            public fixed byte Name[nameBytes];

            [FieldOffset(0x74)]
            public uint ID;

            [FieldOffset(0x84)]
            public uint OwnerID;

            [FieldOffset(0x8C)]
            public byte Type;

            [FieldOffset(0x92)]
            public byte EffectiveDistance;

            [FieldOffset(0xA0)]
            public Single PosX;

            [FieldOffset(0xA4)]
            public Single PosY;

            [FieldOffset(0xA8)]
            public Single PosZ;

            [FieldOffset(0xB0)]
            public Single Rotation;

            [FieldOffset(0x17F8)]
            public uint TargetID;

            [FieldOffset(0x1898)]
            public int CurrentHP;

            [FieldOffset(0x189C)]
            public int MaxHP;

            [FieldOffset(0x18D6)]
            public byte Job;

            [FieldOffset(0x1958)]
            public fixed byte Effects[effectBytes];
        }

        [StructLayout(LayoutKind.Explicit, Size = 12)]
        public struct EffectMemory
        {
            public static int Size => Marshal.SizeOf(typeof(EffectMemory));

            [FieldOffset(0)]
            public ushort BuffID;

            [FieldOffset(2)]
            public ushort Stack;

            [FieldOffset(4)]
            public float Timer;

            [FieldOffset(8)]
            public uint ActorID;
        }

        // Will return any kind of combatant, even if not a mob.
        // This function always returns a combatant object, even if empty.
        public unsafe Combatant GetCombatantFromByteArray(byte[] source, uint mycharID, bool isPlayer, bool exceptEffects = false)
        {
            fixed (byte* p = source)
            {
                var mem = *(CombatantMemory*)&p[0];

                if (isPlayer)
                {
                    mycharID = mem.ID;
                }

                var combatant = new Combatant()
                {
                    Name = FFXIVMemory.GetStringFromBytes(mem.Name, CombatantMemory.nameBytes),
                    Job = mem.Job,
                    ID = mem.ID,
                    OwnerID = mem.OwnerID == emptyID ? 0 : mem.OwnerID,
                    Type = (ObjectType)mem.Type,
                    RawEffectiveDistance = mem.EffectiveDistance,
                    PosX = mem.PosX,
                    PosY = mem.PosY,
                    PosZ = mem.PosZ,
                    Rotation = mem.Rotation,
                    TargetID = mem.TargetID,
                    CurrentHP = mem.CurrentHP,
                    MaxHP = mem.MaxHP,
                    Effects = exceptEffects ? new List<EffectEntry>() : GetEffectEntries(mem.Effects, (ObjectType)mem.Type, mycharID),
                };
                if (combatant.Type != ObjectType.PC && combatant.Type != ObjectType.Monster)
                {
                    // Other types have garbage memory for hp.
                    combatant.CurrentHP = 0;
                    combatant.MaxHP = 0;
                }
                return combatant;
            }
        }

        [StructLayout(LayoutKind.Explicit, Size=8)]
        private struct EnmityListEntry
        {
            public static int Size => Marshal.SizeOf(typeof(EnmityListEntry));

            [FieldOffset(0x00)]
            public uint ID;

            [FieldOffset(0x04)]
            public uint Enmity;
        }

        // A byte[] -> EnmityListEntry[] converter.
        // Owns the memory and returns out EnmityListEntry objects from it.
        private class EnmityList
        {
            public int numEntries = 0;
            private byte[] buffer;

            public const short maxEntries = 31; // or 32?
            public const int numEntryOffset = 256;
            // The number of entries is a short at the end of the array of entries.  Hence, +2.
            public const int totalBytesSize = numEntryOffset + 2;

            public unsafe EnmityListEntry GetEntry(int i)
            {
                fixed (byte* p = buffer)
                {
                    return *(EnmityListEntry*)&p[i * EnmityListEntry.Size];
                }
            }

            public unsafe EnmityList(byte[] buffer)
            {
                Debug.Assert(maxEntries * EnmityListEntry.Size <= totalBytesSize);
                Debug.Assert(buffer.Length >= totalBytesSize);

                this.buffer = buffer;
                fixed (byte* p = buffer) numEntries = Math.Min((short)p[numEntryOffset], maxEntries);
            }
        }

        private EnmityList ReadEnmityList(IntPtr address)
        {
            return new EnmityList(memory.GetByteArray(address, EnmityList.totalBytesSize));
        }

        // Converts an EnmityList into a List<EnmityEntry>.
        public override List<EnmityEntry> GetEnmityEntryList(List<Combatant> combatantList)
        {
            uint topEnmity = 0;
            var mychar = GetSelfCombatant();
            var result = new List<EnmityEntry>();

            var list = ReadEnmityList(enmityAddress);
            for (var i = 0; i < list.numEntries; i++)
            {
                var e = list.GetEntry(i);
                topEnmity = Math.Max(topEnmity, e.Enmity);

                Combatant c = null;
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

        [StructLayout(LayoutKind.Explicit, Size = 72)]
        private struct AggroListEntry
        {
            public static int Size => Marshal.SizeOf(typeof(AggroListEntry));

            [FieldOffset(0x38)]
            public uint ID;

            [FieldOffset(0x3C)]
            public uint Enmity;
        }

        // A byte[] -> AggroListEntry[] converter.
        // Owns the memory and returns out AggroListEntry objects from it.
        private class AggroList
        {
            public int numEntries = 0;
            private byte[] buffer;

            public const short maxEntries = 31;
            public const int numEntryOffset = 0x8F8;
            // The number of entries is a short at the end of the array of entries.  Hence, +2.
            public const int totalBytesSize = numEntryOffset + 2;

            public unsafe AggroListEntry GetEntry(int i)
            {
                fixed (byte* p = buffer)
                {
                    return *(AggroListEntry*)&p[i * AggroListEntry.Size];
                }
            }

            public unsafe AggroList(byte[] buffer)
            {
                Debug.Assert(maxEntries * AggroListEntry.Size <= totalBytesSize);
                Debug.Assert(buffer.Length >= totalBytesSize);

                this.buffer = buffer;
                fixed (byte* p = buffer) numEntries = Math.Min((short)p[numEntryOffset], maxEntries);
            }
        }

        private AggroList ReadAggroList(IntPtr address)
        {
            return new AggroList(memory.GetByteArray(address, AggroList.totalBytesSize));
        }

        // Converts an EnmityList into a List<AggroEntry>.
        public override unsafe List<AggroEntry> GetAggroList(List<Combatant> combatantList)
        {
            var mychar = GetSelfCombatant();

            uint currentTargetID = 0;
            var targetCombatant = GetTargetCombatant();
            if (targetCombatant != null)
            {
                currentTargetID = targetCombatant.ID;
            }

            var result = new List<AggroEntry>();

            var list = ReadAggroList(aggroAddress);
            for (var i = 0; i < list.numEntries; i++)
            {
                var e = list.GetEntry(i);
                if (e.ID <= 0)
                    continue;
                var c = combatantList.Find(x => x.ID == e.ID);
                if (c == null)
                    continue;

                var entry = new AggroEntry()
                {
                    ID = e.ID,
                    // Rather than storing enmity, this is hate rate for the aggro list.
                    // This is likely because we're reading the memory for the aggro sidebar.
                    HateRate = (int)e.Enmity,
                    isCurrentTarget = (e.ID == currentTargetID),
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
                    var t = combatantList.Find(x => x.ID == c.TargetID);
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

        // Satisfying abstract method.
        public override List<TargetableEnemyEntry> GetTargetableEnemyList(List<Combatant> combatantList)
        {
            return new List<TargetableEnemyEntry>();
        }

        public unsafe List<EffectEntry> GetEffectEntries(byte* source, ObjectType type, uint mycharID)
        {
            var result = new List<EffectEntry>();
            var maxEffects = (type == ObjectType.PC) ? 30 : 60;
            var size = EffectMemory.Size * maxEffects;

            var bytes = new byte[size];
            Marshal.Copy((IntPtr)source, bytes, 0, size);

            for (var i = 0; i < maxEffects; i++)
            {
                var effect = GetEffectEntryFromBytes(bytes, i);

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

        public unsafe EffectEntry GetEffectEntryFromBytes(byte[] source, int num = 0)
        {
            fixed (byte* p = source)
            {
                var mem = *(EffectMemory*)&p[num * EffectMemory.Size];

                var effectEntry = new EffectEntry()
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

        public override bool GetInCombat()
        {
            if (inCombatAddress == IntPtr.Zero)
                return false;
            var bytes = memory.Read8(inCombatAddress, 1);
            return bytes[0] != 0;
        }

        [StructLayout(LayoutKind.Explicit, Size = 20)]
        private struct EnmityHudEntryMemory
        {
            public static int Size => Marshal.SizeOf(typeof(EnmityHudEntryMemory));

            [FieldOffset(0x00)]
            public uint HPPercent;

            [FieldOffset(0x04)]
            public uint EnmityPercent;

            [FieldOffset(0x08)]
            public uint CastPercent;

            [FieldOffset(0x0C)]
            public uint ID;

            [FieldOffset(0x10)]
            public uint Unknown01;
        }

        public override List<EnmityHudEntry> GetEnmityHudEntries()
        {
            var entries = new List<EnmityHudEntry>();
            if (enmityHudAddress == IntPtr.Zero) return entries;

            // Resolve Dynamic Pointers
            if (DateTimeOffset.UtcNow - lastDateTimeDynamicAddressChecked > TimeSpan.FromSeconds(30))
            {
                lastDateTimeDynamicAddressChecked = DateTimeOffset.UtcNow;
                var tmpEnmityHudDynamicAddress = memory.ReadIntPtr(enmityHudAddress);
                for (var i = 0; i < enmityHudPointerPath.Length; i++)
                {
                    var p = enmityHudPointerPath[i];
                    tmpEnmityHudDynamicAddress = tmpEnmityHudDynamicAddress + p;
                    tmpEnmityHudDynamicAddress = memory.ReadIntPtr(tmpEnmityHudDynamicAddress);
                    if (tmpEnmityHudDynamicAddress == IntPtr.Zero)
                    {
                        enmityHudDynamicAddress = IntPtr.Zero;
                        return entries;
                    }
                }
                enmityHudDynamicAddress = new IntPtr(tmpEnmityHudDynamicAddress.ToInt64());
            }

            // Get EnmityHud Count, Empty(Min) = 0, Max = 8
            var count = memory.GetInt32(enmityHudDynamicAddress, enmityHudCountOffset);
            if (count < 0) count = 0;
            if (count > 8) count = 8;

            // Get data from memory (all 8 entries)
            var buffer = memory.GetByteArray(enmityHudDynamicAddress + enmityHudEntryOffset, 8 * EnmityHudEntryMemory.Size);

            // Parse data
            for (var i = 0; i < count; i++)
            {
                entries.Add(GetEnmityHudEntryFromBytes(buffer, i));
            }

            return entries;
        }

        public unsafe EnmityHudEntry GetEnmityHudEntryFromBytes(byte[] source, int num = 0)
        {
            if (num < 0) throw new ArgumentException();
            if (num > 8) throw new ArgumentException();

            fixed (byte* p = source)
            {
                var mem = *(EnmityHudEntryMemory*)&p[num * EnmityHudEntryMemory.Size];

                var enmityHudEntry = new EnmityHudEntry()
                {
                    Order = num,
                    ID = mem.ID,
                    HPPercent = mem.HPPercent > 100 ? 0 : mem.HPPercent,
                    EnmityPercent = mem.EnmityPercent > 100 ? 0 : mem.EnmityPercent,
                    CastPercent = mem.CastPercent > 100 ? 0 : mem.CastPercent,
                };

                return enmityHudEntry;
            }
        }
    }
}
