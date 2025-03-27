using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Party
{
    interface IPartyMemory72 : IPartyMemory { }

    public class PartyMemory72 : PartyMemory, IPartyMemory72
    {
        // Due to lack of multi-version support in FFXIVClientStructs, we need to duplicate these structures here per-version
        // We use FFXIVClientStructs versions of the structs because they have more required details than FFXIV_ACT_Plugin's struct definitions
        #region FFXIVClientStructs structs
        [StructLayout(LayoutKind.Explicit, Size = 0x3E0)]
        public unsafe partial struct StatusManager
        {
            [FieldOffset(0x0)] public void* Owner;
            [FieldOffset(0x8)] public fixed byte Status[0xC * 60]; // Client::Game::Status array
            [FieldOffset(0x3C8)] public fixed byte Flags[7];
            [FieldOffset(0x3D0)] public long Unk_3D0;
            [FieldOffset(0x3D8)] public byte NumValidStatuses;
            [FieldOffset(0x3D9)] public byte ExtraFlags;
        }
        [StructLayout(LayoutKind.Explicit, Size = 8)]
        public struct ExtraProperty
        {
            [FieldOffset(0)] public byte Key;
            [FieldOffset(4)] public int Value;
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x490)]
        public unsafe struct PartyMember
        {
            [FieldOffset(0x0)] public StatusManager StatusManager;
            [FieldOffset(0x3E0)] public float X;
            [FieldOffset(0x3E4)] public float Y;
            [FieldOffset(0x3E8)] public float Z;

            [FieldOffset(0x3F0)] public ulong Unk300;
            [FieldOffset(0x3F0)] public ulong AccountId;

            [FieldOffset(0x3F8)] public ulong ContentId;
            [FieldOffset(0x400)] public uint EntityId;
            [FieldOffset(0x404)] public uint PetEntityId;
            [FieldOffset(0x408)] public uint CompanionEntityId;
            [FieldOffset(0x40C)] public uint CurrentHP;
            [FieldOffset(0x410)] public uint MaxHP;
            [FieldOffset(0x414)] public ushort CurrentMP;
            [FieldOffset(0x416)] public ushort MaxMP;
            [FieldOffset(0x418)] public ushort TerritoryType;
            [FieldOffset(0x41A)] public ushort HomeWorld;
            [FieldOffset(0x41C)] public fixed byte Name[0x40];

            [FieldOffset(0x460)] public void* UnkName;
            [FieldOffset(0x460)] public void* NameOverride; // if non-null, replaces real name in ui (eg for blacklisted players)

            [FieldOffset(0x468)] public byte Sex;
            [FieldOffset(0x469)] public byte ClassJob;
            [FieldOffset(0x46A)] public byte Level;
            [FieldOffset(0x46B)] public byte DamageShield;
            [FieldOffset(0x46C)] public ExtraProperty ExtraProperty1;
            [FieldOffset(0x474)] public ExtraProperty ExtraProperty2;
            [FieldOffset(0x47C)] public ExtraProperty ExtraProperty3;
            [FieldOffset(0x484)] public byte Flags;
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x10000)]
        public unsafe partial struct GroupManager
        {
            [FieldOffset(0x0000)]
            public fixed byte vtbls[0x20];
            [FieldOffset(0x0020)] public Group MainGroup;
            [FieldOffset(0x8010)] public Group ReplayGroup;
            [StructLayout(LayoutKind.Explicit, Size = 0x7FF0)]
            public unsafe partial struct Group
            {
                [FieldOffset(0x0)]
                public fixed byte PartyMembers[0x490 * 8]; // PartyMember type
                [FieldOffset(0x2480)]
                public fixed byte AllianceMembers[0x490 * 20]; // PartyMember type

                // Immediately after `AllianceMembers`, e.g. ((0x490 * 20)+0x2480) = 0x6580
                [FieldOffset(0x7FC0)]
                public uint CurrentPartyFlags; // FFXIVClientStructs doesn't map this, but it contains flags that indicate the alliance the player is in, as well as other flags.
                [FieldOffset(0x7FC8)] public long PartyId; // both seem to be unique per party and replicated to every member
                [FieldOffset(0x7FD0)] public long PartyId_2;
                [FieldOffset(0x7FD8)] public uint PartyLeaderIndex; // index of party leader in array
                [FieldOffset(0x7FDC)] public byte MemberCount;

                [FieldOffset(0x7FE1)] public byte AllianceFlags; // 0x01 == is alliance; 0x02 == alliance with 5 4-man groups rather than 2 8-man
            }
        }
        #endregion

        public PartyMemory72(TinyIoCContainer container) : base(container) { }

        public override Version GetVersion()
        {
            return new Version(7, 2);
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
            var groupManager = Marshal.PtrToStructure<GroupManager>(partyInstanceAddress);

            // `PartyMembers` is a standard array, members are moved up/down as they're added/removed.
            // As such, limit extracting members to the current count to avoid "ghost" members
            var partyMembers = extractPartyMembers(groupManager.MainGroup.PartyMembers, Math.Min((int)groupManager.MainGroup.MemberCount, 8));

            // `AllianceMembers` is a fixed-position array, with removed elements being mostly zero'd out
            // Easiest way to check if an entry is still active is to check for `Flags != 0`
            var alliance1Members = extractAllianceMembers(groupManager.MainGroup.AllianceMembers, 20, 0, 8);
            var alliance2Members = extractAllianceMembers(groupManager.MainGroup.AllianceMembers, 20, 8, 8);
            // @TODO: Actually verify D/E/F alliance info? FFXIVClientStructs renamed this to ReplayGroup but pretty sure D/E/F are still stored here for 48 man content.
            var alliance3Members = extractAllianceMembers(groupManager.ReplayGroup.PartyMembers, 8, 0, 8);
            var alliance4Members = extractAllianceMembers(groupManager.ReplayGroup.AllianceMembers, 20, 0, 8);
            var alliance5Members = extractAllianceMembers(groupManager.ReplayGroup.AllianceMembers, 20, 8, 8);

            return new PartyListsStruct()
            {
                partyId = groupManager.MainGroup.PartyId,
                partyId_2 = groupManager.MainGroup.PartyId_2,
                partyLeaderIndex = groupManager.MainGroup.PartyLeaderIndex,
                memberCount = groupManager.MainGroup.MemberCount,
                allianceFlags = groupManager.MainGroup.AllianceFlags,

                currentPartyFlags = groupManager.MainGroup.CurrentPartyFlags,

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
