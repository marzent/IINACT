using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party
{
    interface IPartyMemory70 : IPartyMemory { }

    public class PartyMemory70 : PartyMemory, IPartyMemory70
    {
        // Due to lack of multi-version support in FFXIVClientStructs, we need to duplicate these structures here per-version
        // We use FFXIVClientStructs versions of the structs because they have more required details than FFXIV_ACT_Plugin's struct definitions
        #region FFXIVClientStructs structs
        [StructLayout(LayoutKind.Explicit, Size = 0x2F0)]
        public unsafe partial struct StatusManager
        {
            [FieldOffset(0x0)]
            public void* Owner;
            [FieldOffset(0x8)]
            public fixed byte Status[0xC * 60]; // Client::Game::Status array
            [FieldOffset(0x2D8)]
            public uint Flags1;
            [FieldOffset(0x2DC)]
            public ushort Flags2;
            [FieldOffset(0x2E0)]
            public long Unk_178;
            [FieldOffset(0x2E8)]
            public byte NumValidStatuses;
        }
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct ExtraProperty
        {
            [FieldOffset(0)] public byte Key;
            [FieldOffset(4)] public int Value;
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x3A0)]
        public unsafe struct PartyMember
        {
            [FieldOffset(0x0)] public StatusManager StatusManager;
            [FieldOffset(0x2F0)] public float X;
            [FieldOffset(0x2F4)] public float Y;
            [FieldOffset(0x2F8)] public float Z;
            [FieldOffset(0x300)] public ulong Unk300;
            [FieldOffset(0x308)] public ulong ContentId;
            [FieldOffset(0x310)] public uint EntityId;
            [FieldOffset(0x31C)] public uint CurrentHP;
            [FieldOffset(0x320)] public uint MaxHP;
            [FieldOffset(0x324)] public ushort CurrentMP;
            [FieldOffset(0x326)] public ushort MaxMP;
            [FieldOffset(0x328)] public ushort TerritoryType;
            [FieldOffset(0x32A)] public ushort HomeWorld;
            [FieldOffset(0x32C)]
            public fixed byte Name[0x40];
            [FieldOffset(0x370)] public void* UnkName;
            [FieldOffset(0x378)] public byte Sex;
            [FieldOffset(0x379)] public byte ClassJob;
            [FieldOffset(0x37A)] public byte Level;
            [FieldOffset(0x37B)] public byte DamageShield;
            [FieldOffset(0x37C)] public ExtraProperty ExtraProperty1;
            [FieldOffset(0x384)] public ExtraProperty ExtraProperty2;
            [FieldOffset(0x38C)] public ExtraProperty ExtraProperty3;
            [FieldOffset(0x394)] public byte Flags;
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x65B0)]
        public unsafe partial struct GroupManager
        {
            [FieldOffset(0x0)]
            public fixed byte PartyMembers[0x3A0 * 8]; // PartyMember type
            [FieldOffset(0x1D00)]
            public fixed byte AllianceMembers[0x3A0 * 20]; // PartyMember type
            // Immediately after `AllianceMembers`, e.g. ((0x390 * 20)+0x1D00) = 0x6400
            [FieldOffset(0x6400)]
            public uint CurrentPartyFlags; // FFXIVClientStructs doesn't map this, but it contains flags that indicate the alliance the player is in, as well as other flags.
            [FieldOffset(0x6588)]
            public long PartyId; // both seem to be unique per party and replicated to every member
            [FieldOffset(0x6590)]
            public long PartyId_2;
            [FieldOffset(0x6598)]
            public uint PartyLeaderIndex; // index of party leader in array
            [FieldOffset(0x659C)]
            public byte MemberCount;
            [FieldOffset(0x65A1)]
            public byte AllianceFlags; // 0x01 == is alliance; 0x02 == alliance with 5 4-man groups rather than 2 8-man
        }
        #endregion
        [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
        private unsafe struct DoubleGroupManager
        {
            public fixed byte vtbls[0x20];
            public GroupManager groupManager1;
            public GroupManager groupManager2;
        }

        public PartyMemory70(TinyIoCContainer container) : base(container) { }

        public override Version GetVersion()
        {
            return new Version(7, 0);
        }


        public unsafe PartyListsStruct GetPartyLists()
        {
            if (!IsValid())
            {
                return new PartyListsStruct();
            }

            ScanPointers();

            if (partyInstanceAddress.ToInt64() == 0)
            {
                return new PartyListsStruct();
            }
            var groupManager = Marshal.PtrToStructure<DoubleGroupManager>(partyInstanceAddress);

            // `PartyMembers` is a standard array, members are moved up/down as they're added/removed.
            // As such, limit extracting members to the current count to avoid "ghost" members
            var partyMembers = extractPartyMembers(groupManager.groupManager1.PartyMembers, Math.Min((int)groupManager.groupManager1.MemberCount, 8));

            // `AllianceMembers` is a fixed-position array, with removed elements being mostly zero'd out
            // Easiest way to check if an entry is still active is to check for `Flags != 0`
            var alliance1Members = extractAllianceMembers(groupManager.groupManager1.AllianceMembers, 20, 0, 8);
            var alliance2Members = extractAllianceMembers(groupManager.groupManager1.AllianceMembers, 20, 8, 8);
            // TOOD: Actually verify D/E/F alliance info?
            var alliance3Members = extractAllianceMembers(groupManager.groupManager2.PartyMembers, 8, 0, 8);
            var alliance4Members = extractAllianceMembers(groupManager.groupManager2.AllianceMembers, 20, 0, 8);
            var alliance5Members = extractAllianceMembers(groupManager.groupManager2.AllianceMembers, 20, 8, 8);

            return new PartyListsStruct()
            {
                partyId = groupManager.groupManager1.PartyId,
                partyId_2 = groupManager.groupManager1.PartyId_2,
                partyLeaderIndex = groupManager.groupManager1.PartyLeaderIndex,
                memberCount = groupManager.groupManager1.MemberCount,
                allianceFlags = groupManager.groupManager1.AllianceFlags,

                currentPartyFlags = groupManager.groupManager1.CurrentPartyFlags,

                partyMembers = partyMembers,
                alliance1Members = alliance1Members,
                alliance2Members = alliance2Members,
                alliance3Members = alliance3Members,
                alliance4Members = alliance4Members,
                alliance5Members = alliance5Members,
            };
        }

        private unsafe PartyListEntry[] extractAllianceMembers(byte* allianceMembers, int elementCount, int start, int count)
        {
            var allMembers = extractPartyMembers(allianceMembers, elementCount);
            var retMembers = new PartyListEntry[count];
            for (var i = start; i < start + count && i < allMembers.Length; ++i)
            {
                var member = allMembers[i];
                if (member.flags == 0)
                {
                    continue;
                }

                retMembers[i - start] = member;
            }
            return retMembers;
        }

        private unsafe PartyListEntry[] extractPartyMembers(byte* ptr, int count)
        {
            var ret = new PartyListEntry[count];
            for (int i = 0; i < count; ++i)
            {
                var member = Marshal.PtrToStructure<PartyMember>(new IntPtr(ptr + (i * sizeof(PartyMember))));
                ret[i] = new PartyListEntry()
                {
                    x = member.X,
                    y = member.Y,
                    z = member.Z,
                    contentId = (long)member.ContentId,
                    objectId = member.EntityId,
                    currentHP = member.CurrentHP,
                    maxHP = member.MaxHP,
                    currentMP = member.CurrentMP,
                    maxMP = member.MaxMP,
                    territoryType = member.TerritoryType,
                    homeWorld = member.HomeWorld,
                    name = FFXIVMemory.GetStringFromBytes(member.Name, 0x40),
                    sex = member.Sex,
                    classJob = member.ClassJob,
                    level = member.Level,
                    flags = member.Flags,
                };
            }
            return ret;
        }

    }
}
