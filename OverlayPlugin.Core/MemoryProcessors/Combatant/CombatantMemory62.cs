using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.Combatant
{
    interface ICombatantMemory62 : ICombatantMemory { }

    class CombatantMemory62 : CombatantMemory, ICombatantMemory62
    {
        private const string charmapSignature = "488B5720B8000000E0483BD00F84????????488D0D";

        public CombatantMemory62(TinyIoCContainer container)
            : base(container, charmapSignature, CombatantMemory.Size, EffectMemory.Size)
        {

        }

        public override Version GetVersion()
        {
            return new Version(6, 2);
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

            // Unknown size, but this is the bytes up to the next field.
            public const int NameBytes = 68;

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

            [FieldOffset(0xA0)]
            public Single PosX;

            [FieldOffset(0xA4)]
            public Single PosY;

            [FieldOffset(0xA8)]
            public Single PosZ;

            [FieldOffset(0xB0)]
            public Single Heading;

            [FieldOffset(0xC0)]
            public Single Radius;

            [FieldOffset(0x104)]
            public int ModelStatus;

            [FieldOffset(0x1C4)]
            public int CurrentHP;

            [FieldOffset(0x1C8)]
            public int MaxHP;

            [FieldOffset(0x1CC)]
            public int CurrentMP;

            [FieldOffset(0x1D0)]
            public int MaxMP;

            [FieldOffset(0x1D4)]
            public ushort CurrentGP;

            [FieldOffset(0x1D6)]
            public ushort MaxGP;

            [FieldOffset(0x1D8)]
            public ushort CurrentCP;

            [FieldOffset(0x1DA)]
            public ushort MaxCP;

            [FieldOffset(0x1E0)]
            public byte Job;

            [FieldOffset(0x1E1)]
            public byte Level;

            [FieldOffset(0xC60)]
            public uint PCTargetID;

            [FieldOffset(0x19C3)]
            public byte MonsterType;

            [FieldOffset(0x19DF)]
            public byte AggressionStatus;

            [FieldOffset(0x1A68)]
            public uint NPCTargetID;

            [FieldOffset(0x1AAC)]
            public uint BNpcNameID;

            [FieldOffset(0x1AC8)]
            public ushort CurrentWorldID;

            [FieldOffset(0x1ACA)]
            public ushort WorldID;

            [FieldOffset(0x1B48)]
            public fixed byte Effects[EffectBytes];

            [FieldOffset(0x1CD0)]
            public byte IsCasting1;

            [FieldOffset(0x1CD2)]
            public byte IsCasting2;

            [FieldOffset(0x1CD4)]
            public uint CastBuffID;

            [FieldOffset(0x1CE0)]
            public uint CastTargetID;

            [FieldOffset(0x1D04)]
            public float CastDurationCurrent;

            [FieldOffset(0x1D08)]
            public float CastDurationMax;
            // Missing PartyType
        }
    }
}
