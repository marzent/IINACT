using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    interface ICombatantMemory65 : ICombatantMemory { }

    class CombatantMemory65 : CombatantMemory, ICombatantMemory65
    {
        private const string charmapSignature = "488B5720B8000000E0483BD00F84????????488D0D";

        public CombatantMemory65(TinyIoCContainer container)
            : base(container, charmapSignature, CombatantMemory.Size, EffectMemory.Size)
        {

        }

        public override Version GetVersion()
        {
            return new Version(6, 5);
        }

        // Returns a combatant if the combatant is a mob or a PC.
        protected override unsafe Combatant GetMobFromByteArray(byte[] source, uint mycharID)
        {
            fixed (byte* p = source)
            {
                CombatantMemory mem = *(CombatantMemory*)&p[0];
                ObjectType type = (ObjectType)mem.Type;
                if (mem.ID == 0 || mem.ID == emptyID)
                    return null;
            }
            return GetCombatantFromByteArray(source, mycharID, false);
        }

        // Will return any kind of combatant, even if not a mob.
        // This function always returns a combatant object, even if empty.
        protected override unsafe Combatant GetCombatantFromByteArray(byte[] source, uint mycharID, bool isPlayer, bool exceptEffects = false)
        {
            fixed (byte* p = source)
            {
                CombatantMemory mem = *(CombatantMemory*)&p[0];

                if (isPlayer)
                {
                    mycharID = mem.ID;
                }

                Combatant combatant = new Combatant()
                {
                    Name = FFXIVMemory.GetStringFromBytes(mem.Name, CombatantMemory.NameBytes),
                    Job = mem.Job,
                    ID = mem.ID,
                    OwnerID = mem.OwnerID == emptyID ? 0 : mem.OwnerID,
                    Type = (ObjectType)mem.Type,
                    MonsterType = (MonsterType)mem.MonsterType,
                    Status = (ObjectStatus)mem.Status,
                    ModelStatus = (ModelStatus)mem.ModelStatus,
                    // Normalize all possible aggression statuses into the basic 4 ones.
                    AggressionStatus = (AggressionStatus)(mem.AggressionStatus - (mem.AggressionStatus / 4) * 4),
                    NPCTargetID = mem.NPCTargetID,
                    RawEffectiveDistance = mem.EffectiveDistance,
                    PosX = mem.PosX,
                    // Y and Z are deliberately swapped to match FFXIV_ACT_Plugin's data model
                    PosY = mem.PosZ,
                    PosZ = mem.PosY,
                    Heading = mem.Heading,
                    Radius = mem.Radius,
                    // In-memory there are separate values for PC's current target and NPC's current target
                    TargetID = (ObjectType)mem.Type == ObjectType.PC ? mem.PCTargetID : mem.NPCTargetID,
                    CurrentHP = mem.CurrentHP,
                    MaxHP = mem.MaxHP,
                    Effects = exceptEffects ? new List<EffectEntry>() : GetEffectEntries(mem.Effects, (ObjectType)mem.Type, mycharID),

                    BNpcID = mem.BNpcID,
                    CurrentMP = mem.CurrentMP,
                    MaxMP = mem.MaxMP,
                    CurrentGP = mem.CurrentGP,
                    MaxGP = mem.MaxGP,
                    CurrentCP = mem.CurrentCP,
                    MaxCP = mem.MaxCP,
                    Level = mem.Level,
                    PCTargetID = mem.PCTargetID,

                    BNpcNameID = mem.BNpcNameID,

                    WorldID = mem.WorldID,
                    CurrentWorldID = mem.CurrentWorldID,

                    IsCasting1 = mem.IsCasting1,
                    IsCasting2 = mem.IsCasting2,
                    CastBuffID = mem.CastBuffID,
                    CastTargetID = mem.CastTargetID,
                    CastDurationCurrent = mem.CastDurationCurrent,
                    CastDurationMax = mem.CastDurationMax,

                    TransformationId = mem.TransformationId,
                    WeaponId = mem.WeaponId
                };
                combatant.IsTargetable =
                    (combatant.ModelStatus == ModelStatus.Visible)
                    && ((combatant.Status == ObjectStatus.NormalActorStatus) || (combatant.Status == ObjectStatus.NormalSubActorStatus));
                if (combatant.Type != ObjectType.PC && combatant.Type != ObjectType.Monster)
                {
                    // Other types have garbage memory for hp.
                    combatant.CurrentHP = 0;
                    combatant.MaxHP = 0;
                }
                return combatant;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private unsafe struct CombatantMemory
        {
            public static int Size => Marshal.SizeOf(typeof(CombatantMemory));

            // 64 bytes per both FFXIV_ACT_Plugin and aers/FFXIVClientStructs
            public const int NameBytes = 64;

            public const int EffectCount = 60;
            public const int EffectBytes = EffectMemory.Size * EffectCount;

            [FieldOffset(0x30)]
            public fixed byte Name[NameBytes];

            [FieldOffset(0x74)]
            public uint ID;

            [FieldOffset(0x80)]
            public uint BNpcID;

            [FieldOffset(0x84)]
            public uint OwnerID;

            [FieldOffset(0x8C)]
            public byte Type;

            [FieldOffset(0x92)]
            public byte EffectiveDistance;

            [FieldOffset(0x94)]
            public byte Status;

            [FieldOffset(0xB0)]
            public Single PosX;

            [FieldOffset(0xB4)]
            public Single PosY;

            [FieldOffset(0xB8)]
            public Single PosZ;

            [FieldOffset(0xC0)]
            public Single Heading;

            [FieldOffset(0xD0)]
            public Single Radius;

            [FieldOffset(0x114)]
            public int ModelStatus;

            [FieldOffset(0x1BC)]
            public int CurrentHP;

            [FieldOffset(0x1C0)]
            public int MaxHP;

            [FieldOffset(0x1C4)]
            public int CurrentMP;

            [FieldOffset(0x1C8)]
            public int MaxMP;

            [FieldOffset(0x1CC)]
            public ushort CurrentGP;

            [FieldOffset(0x1CE)]
            public ushort MaxGP;

            [FieldOffset(0x1D0)]
            public ushort CurrentCP;

            [FieldOffset(0x1D2)]
            public ushort MaxCP;

            [FieldOffset(0x1D4)]
            public short TransformationId;

            [FieldOffset(0x1DA)]
            public byte Job;

            [FieldOffset(0x1DB)]
            public byte Level;

            [FieldOffset(0xC30)]
            public byte WeaponId;

            [FieldOffset(0xD00)]
            public uint PCTargetID;

            // TODO: this is still incorrect as of 6.5, should we drop this field if we can't find it again?
            [FieldOffset(0x19C3)]
            public byte MonsterType;

            // TODO: this is still incorrect as of 6.5, should we drop this field if we can't find it again?
            [FieldOffset(0x19DF)]
            public byte AggressionStatus;

            [FieldOffset(0x1B58)]
            public uint NPCTargetID;

            [FieldOffset(0x1B98)]
            public uint BNpcNameID;

            [FieldOffset(0x1BB0)]
            public ushort CurrentWorldID;

            [FieldOffset(0x1BB2)]
            public ushort WorldID;

            [FieldOffset(0x1C18)]
            public fixed byte Effects[EffectBytes];

            [FieldOffset(0x1F00)]
            public byte IsCasting1;

            [FieldOffset(0x1F02)]
            public byte IsCasting2;

            [FieldOffset(0x1F04)]
            public uint CastBuffID;

            [FieldOffset(0x1F10)]
            public uint CastTargetID;

            [FieldOffset(0x1F34)]
            public float CastDurationCurrent;

            [FieldOffset(0x1F38)]
            public float CastDurationMax;

            // Missing PartyType
        }
    }
}
