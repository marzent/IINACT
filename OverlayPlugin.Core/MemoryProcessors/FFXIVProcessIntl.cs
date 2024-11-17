using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors
{
    public class FFXIVProcessIntl : FFXIVProcess
    {
        // Last updated for FFXIV 7.1

        [StructLayout(LayoutKind.Explicit)]
        public unsafe struct EntityMemory
        {
            public static int Size => Marshal.SizeOf(typeof(EntityMemory));

            // Unknown size, but this is the bytes up to the next field.
            public const int nameBytes = 68;

            [FieldOffset(0x30)]
            public fixed byte Name[nameBytes];

            [FieldOffset(0x74)]
            public uint id;

            [FieldOffset(0x8C)]
            public EntityType type;

            [FieldOffset(0x92)]
            public ushort distance;

            [FieldOffset(0xB0)]
            public Single pos_x;

            [FieldOffset(0xB4)]
            public Single pos_z;

            [FieldOffset(0xB8)]
            public Single pos_y;

            [FieldOffset(0xC0)]
            public Single rotation;

            [FieldOffset(0x1AC)]
            public CharacterDetails charDetails;

            [FieldOffset(0x1D6)]
            public byte shieldPercentage;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct CharacterDetails
        {
            [FieldOffset(0x00)]
            public int hp;

            [FieldOffset(0x04)]
            public int max_hp;

            [FieldOffset(0x08)]
            public short mp;

            [FieldOffset(0x10)]
            public short gp;

            [FieldOffset(0x12)]
            public short max_gp;

            [FieldOffset(0x14)]
            public short cp;

            [FieldOffset(0x16)]
            public short max_cp;

            [FieldOffset(0x1E)]
            public EntityJob job;

            [FieldOffset(0x1F)]
            public byte level;
        }

        public FFXIVProcessIntl(TinyIoCContainer container) : base(container) { }

        // TODO: all of this could be refactored into structures of some sort
        // instead of just being loose variables everywhere.

        // A piece of code that reads the pointer to the list of all entities, that we
        // refer to as the charmap.
        private static String kCharmapSignature = "488b5720b8000000e0483Bd00f84????????488d0d";

        private static int kCharmapSignatureOffset = 0;

        // The signature finds a pointer in the executable code which uses RIP addressing.
        private static bool kCharmapSignatureRIP = true;

        // The pointer is to a structure as:
        //
        // CharmapStruct* outer;  // The pointer found from the signature.
        // CharmapStruct {
        //   EntityStruct* player;
        // }
        private static int kCharmapStructOffsetPlayer = 0;

        // In combat boolean.
        // This address is written to by "mov [rax+rcx],bl" and has three readers.
        // This reader is "cmp byte ptr [ffxiv_dx11.exe+????????],00 { (0),0 }"
        private static String kInCombatSignature = "803D??????????74??488B03488BCBFF50";
        private static int kInCombatSignatureOffset = -15;

        private static bool kInCombatSignatureRIP = true;

        // Because this line is a cmp byte line, the signature is not at the end of the line.
        private static int kInCombatRipOffset = 1;

        // A piece of code that reads the job data.
        // The pointer of interest is the first ???????? in the signature.
        private static String kJobDataSignature = "488B3D????????33ED";
        private static int kJobDataSignatureOffset = -6;

        // The signature finds a pointer in the executable code which uses RIP addressing.
        private static bool kJobDataSignatureRIP = true;

        internal override void ReadSignatures()
        {
            List<IntPtr> p;

            // TODO: for now, support multiple matches on charmap signature.
            // This sig returns two matches that are identical for many, many characters.
            // They both point to the same spot, so verify these have the same value.
            p = SigScan(kCharmapSignature, kCharmapSignatureOffset, kCharmapSignatureRIP);
            if (p.Count == 0)
            {
                logger_.Log(LogLevel.Error, "Charmap signature found " + p.Count + " matches");
            }
            else
            {
                var player_ptr_value = IntPtr.Zero;
                foreach (var ptr in p)
                {
                    var addr = IntPtr.Add(ptr, kCharmapStructOffsetPlayer);
                    var value = ReadIntPtr(addr);
                    if (player_ptr_value == IntPtr.Zero || player_ptr_value == value)
                    {
                        player_ptr_value = value;
                        player_ptr_addr_ = addr;
                    }
                    else
                    {
                        logger_.Log(LogLevel.Error, "Charmap signature found, but conflicting match");
                    }
                }
            }

            p = SigScan(kJobDataSignature, kJobDataSignatureOffset, kJobDataSignatureRIP);
            if (p.Count != 1)
            {
                logger_.Log(LogLevel.Error, "Job signature found " + p.Count + " matches");
            }
            else
            {
                job_data_outer_addr_ = IntPtr.Add(p[0], kJobDataOuterStructOffset);
            }

            p = SigScan(kInCombatSignature, kInCombatSignatureOffset, kInCombatSignatureRIP, kInCombatRipOffset);
            if (p.Count != 1)
            {
                logger_.Log(LogLevel.Error, "In combat signature found " + p.Count + " matches");
            }
            else
            {
                in_combat_addr_ = p[0];
            }
        }

        public unsafe override EntityData GetEntityDataFromByteArray(byte[] source)
        {
            fixed (byte* p = source)
            {
                var mem = *(EntityMemory*)&p[0];

                // dump '\0' string terminators
                var memoryName = System.Text.Encoding.UTF8.GetString(mem.Name, EntityMemory.nameBytes)
                                       .Split(new[] { '\0' }, 2)[0];

                var entity = new EntityData()
                {
                    name = memoryName,
                    id = mem.id,
                    type = mem.type,
                    distance = mem.distance,
                    pos_x = mem.pos_x,
                    pos_y = mem.pos_y,
                    pos_z = mem.pos_z,
                    rotation = mem.rotation,
                };
                if (entity.type == EntityType.PC || entity.type == EntityType.Monster)
                {
                    entity.job = mem.charDetails.job;

                    entity.hp = mem.charDetails.hp;
                    entity.max_hp = mem.charDetails.max_hp;
                    entity.mp = mem.charDetails.mp;
                    // This doesn't exist in memory, so just send the right value.
                    // As there are other versions that still have it, don't change the event.
                    entity.max_mp = 10000;
                    entity.shield_value = mem.shieldPercentage * entity.max_hp / 100;

                    if (IsGatherer(entity.job))
                    {
                        entity.gp = mem.charDetails.gp;
                        entity.max_gp = mem.charDetails.max_gp;
                    }

                    if (IsCrafter(entity.job))
                    {
                        entity.cp = mem.charDetails.cp;
                        entity.max_cp = mem.charDetails.max_cp;
                    }

                    entity.level = mem.charDetails.level;

                    var job_bytes = GetRawJobSpecificDataBytes();
                    if (job_bytes != null)
                    {
                        for (var i = 0; i < job_bytes.Length; ++i)
                        {
                            if (entity.debug_job != "")
                                entity.debug_job += " ";
                            entity.debug_job += string.Format("{0:x2}", job_bytes[i]);
                        }
                    }
                }

                return entity;
            }
        }

        internal override EntityData GetEntityData(IntPtr entity_ptr)
        {
            if (entity_ptr == IntPtr.Zero)
                return null;
            var source = Read8(entity_ptr, EntityMemory.Size);
            return GetEntityDataFromByteArray(source);
        }

        public override EntityData GetSelfData()
        {
            if (!HasProcess() || player_ptr_addr_ == IntPtr.Zero)
                return null;

            var entity_ptr = ReadIntPtr(player_ptr_addr_);
            if (entity_ptr == IntPtr.Zero)
                return null;
            return GetEntityData(entity_ptr);
        }

        public override unsafe JObject GetJobSpecificData(EntityJob job)
        {
            if (!HasProcess() || job_data_outer_addr_ == IntPtr.Zero)
                return null;

            var job_inner_ptr = ReadIntPtr(job_data_outer_addr_);
            if (job_inner_ptr == IntPtr.Zero)
            {
                // The pointer can be null when not logged in.
                return null;
            }

            job_inner_ptr = IntPtr.Add(job_inner_ptr, kJobDataInnerStructOffset);

            fixed (byte* p = Read8(job_inner_ptr, kJobDataInnerStructSize))
            {
                if (p == null)
                    return null;

                return job switch
                {
                    EntityJob.RDM => JObject.FromObject(*(RedMageJobMemory*)&p[0]),
                    EntityJob.WAR => JObject.FromObject(*(WarriorJobMemory*)&p[0]),
                    EntityJob.DRK => JObject.FromObject(*(DarkKnightJobMemory*)&p[0]),
                    EntityJob.PLD => JObject.FromObject(*(PaladinJobMemory*)&p[0]),
                    EntityJob.GNB => JObject.FromObject(*(GunbreakerJobMemory*)&p[0]),
                    EntityJob.BRD => JObject.FromObject(*(BardJobMemory*)&p[0]),
                    EntityJob.DNC => JObject.FromObject(*(DancerJobMemory*)&p[0]),
                    EntityJob.DRG => JObject.FromObject(*(DragoonJobMemory*)&p[0]),
                    EntityJob.NIN => JObject.FromObject(*(NinjaJobMemory*)&p[0]),
                    EntityJob.THM => JObject.FromObject(*(ThaumaturgeJobMemory*)&p[0]),
                    EntityJob.BLM => JObject.FromObject(*(BlackMageJobMemory*)&p[0]),
                    EntityJob.WHM => JObject.FromObject(*(WhiteMageJobMemory*)&p[0]),
                    EntityJob.ACN => JObject.FromObject(*(ArcanistJobMemory*)&p[0]),
                    EntityJob.SMN => JObject.FromObject(*(SummonerJobMemory*)&p[0]),
                    EntityJob.SCH => JObject.FromObject(*(ScholarJobMemory*)&p[0]),
                    EntityJob.MNK => JObject.FromObject(*(MonkJobMemory*)&p[0]),
                    EntityJob.MCH => JObject.FromObject(*(MachinistJobMemory*)&p[0]),
                    EntityJob.AST => JObject.FromObject(*(AstrologianJobMemory*)&p[0]),
                    EntityJob.SAM => JObject.FromObject(*(SamuraiJobMemory*)&p[0]),
                    EntityJob.SGE => JObject.FromObject(*(SageJobMemory*)&p[0]),
                    EntityJob.RPR => JObject.FromObject(*(ReaperJobMemory*)&p[0]),
                    EntityJob.VPR => JObject.FromObject(*(ViperJobMemory*)&p[0]),
                    EntityJob.PCT => JObject.FromObject(*(PictomancerJobMemory*)&p[0]),
                    _ => null
                };
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct RedMageJobMemory
        {
            [FieldOffset(0x00)]
            public byte whiteMana;

            [FieldOffset(0x01)]
            public byte blackMana;

            [FieldOffset(0x02)]
            public byte manaStacks;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct WarriorJobMemory
        {
            [FieldOffset(0x00)]
            public byte beast;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct DarkKnightJobMemory
        {
            [FieldOffset(0x00)]
            public byte blood;

            [FieldOffset(0x02)]
            public ushort darksideMilliseconds;

            [FieldOffset(0x04)]
            public byte darkArts;

            [FieldOffset(0x06)]
            public ushort livingShadowMilliseconds;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct PaladinJobMemory
        {
            [FieldOffset(0x00)]
            public byte oath;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct GunbreakerJobMemory
        {
            [FieldOffset(0x00)]
            public byte cartridges;

            [FieldOffset(0x02)]
            private ushort continuationMilliseconds; // Is 15000 if and only if continuationState is not zero.

            [FieldOffset(0x04)]
            public byte continuationState;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct BardJobMemory
        {
            [Flags]
            private enum SongFlags : byte
            {
                None = 0,
                Ballad = 1,          // Mage's Ballad.
                Paeon = 1 << 1,      // Army's Paeon.
                Minuet = 1 | 1 << 1, // The Wanderer's Minuet.
                BalladLastPlayed = 1 << 2,
                PaeonLastPlayed = 1 << 3,
                MinuetLastPlayed = 1 << 2 | 1 << 3,
                BalladCoda = 1 << 4,
                PaeonCoda = 1 << 5,
                MinuetCoda = 1 << 6,
            }

            [FieldOffset(0x00)]
            public ushort songMilliseconds;

            [FieldOffset(0x04)]
            public byte songProcs;

            [FieldOffset(0x05)]
            public byte soulGauge;

            [NonSerialized]
            [FieldOffset(0x06)]
            private SongFlags songFlags;

            public String songName
            {
                get
                {
                    if (songFlags.HasFlag(SongFlags.Minuet))
                        return "Minuet";
                    if (songFlags.HasFlag(SongFlags.Ballad))
                        return "Ballad";
                    if (songFlags.HasFlag(SongFlags.Paeon))
                        return "Paeon";
                    return "None";
                }
            }

            public String lastPlayed
            {
                get
                {
                    if (songFlags.HasFlag(SongFlags.MinuetLastPlayed))
                        return "Minuet";
                    if (songFlags.HasFlag(SongFlags.BalladLastPlayed))
                        return "Ballad";
                    if (songFlags.HasFlag(SongFlags.PaeonLastPlayed))
                        return "Paeon";
                    return "None";
                }
            }

            public String[] coda
            {
                get
                {
                    return new[]
                    {
                        this.songFlags.HasFlag(SongFlags.BalladCoda) ? "Ballad" : "None",
                        this.songFlags.HasFlag(SongFlags.PaeonCoda) ? "Paeon" : "None",
                        this.songFlags.HasFlag(SongFlags.MinuetCoda) ? "Minuet" : "None",
                    };
                }
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct DancerJobMemory
        {
            private enum Step : byte
            {
                None = 0,
                Emboite = 1,
                Entrechat = 2,
                Jete = 3,
                Pirouette = 4,
            }

            [FieldOffset(0x00)]
            public byte feathers;

            [FieldOffset(0x01)]
            public byte esprit;

            [NonSerialized]
            [FieldOffset(0x02)]
            private Step step1; // Order of steps in current Standard Step/Technical Step combo.

            [NonSerialized]
            [FieldOffset(0x03)]
            private Step step2;

            [NonSerialized]
            [FieldOffset(0x04)]
            private Step step3;

            [NonSerialized]
            [FieldOffset(0x05)]
            private Step step4;

            [FieldOffset(0x06)]
            public byte currentStep; // Number of steps executed in current Standard Step/Technical Step combo.

            public string[] steps
            {
                get
                {
                    Step[] _steps = { step1, step2, step3, step4 };
                    return _steps.Select(s => s.ToString()).Where(s => s != "None").ToArray();
                }
            }
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct DragoonJobMemory
        {
            [NonSerialized]
            [FieldOffset(0x00)]
            private ushort blood_or_life_ms;

            [NonSerialized]
            [FieldOffset(0x02)]
            private byte stance; // 0 = None, 1 = Blood, 2 = Life

            [FieldOffset(0x03)]
            public byte eyesAmount;

            public uint bloodMilliseconds
            {
                get
                {
                    if (stance == 1)
                        return blood_or_life_ms;
                    else
                        return 0;
                }
            }

            public uint lifeMilliseconds
            {
                get
                {
                    if (stance == 2)
                        return blood_or_life_ms;
                    else
                        return 0;
                }
            }

            [FieldOffset(0x04)]
            public byte firstmindsFocus;
        };

        [Serializable]
        [StructLayout(LayoutKind.Explicit)]
        public struct NinjaJobMemory
        {
            [FieldOffset(0x00)]
            public ushort hutonMilliseconds;

            [FieldOffset(0x02)]
            public byte ninkiAmount;

            [FieldOffset(0x03)]
            private byte hutonCount; // Why though?
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct ThaumaturgeJobMemory
        {
            [FieldOffset(0x02)]
            public ushort umbralMilliseconds; // Number of ms left in umbral fire/ice.

            [FieldOffset(0x04)]
            public sbyte umbralStacks; // Positive = Umbral Fire Stacks, Negative = Umbral Ice Stacks.
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct BlackMageJobMemory
        {
            [Flags]
            public enum EnochianFlags : byte
            {
                None = 0,
                Enochian = 1,
                Paradox = 2,
            }

            [FieldOffset(0x00)]
            public ushort nextPolyglotMilliseconds; // Number of ms left before polyglot proc.

            [FieldOffset(0x02)]
            public ushort umbralMilliseconds; // Number of ms left in umbral fire/ice.

            [FieldOffset(0x04)]
            public sbyte umbralStacks; // Positive = Umbral Fire Stacks, Negative = Umbral Ice Stacks.

            [FieldOffset(0x05)]
            public byte umbralHearts;

            [FieldOffset(0x06)]
            public byte polyglot;

            [NonSerialized]
            [FieldOffset(0x07)]
            private EnochianFlags enochian_state;

            public bool enochian => enochian_state.HasFlag(EnochianFlags.Enochian);

            public bool paradox => enochian_state.HasFlag(EnochianFlags.Paradox);
            
            public int astralSoulStacks {
                get {
                    return ((int)enochian_state >> 2) & 0x7; // = 0b111, to get the last 3 bits.
                }
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct WhiteMageJobMemory
        {
            [FieldOffset(0x02)]
            public ushort lilyMilliseconds; // Number of ms left before lily gain.

            [FieldOffset(0x04)]
            public byte lilyStacks;

            [FieldOffset(0x05)]
            public byte bloodlilyStacks;
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct ArcanistJobMemory
        {
            [FieldOffset(0x04)]
            public byte aetherflowStacks;
        };
        
        [StructLayout(LayoutKind.Explicit)]
         public struct SummonerJobMemory {
           public enum ActiveArcanum : byte {
             None = 0,
             Ifrit = 1,
             Titan = 2,
             Garuda = 3,
           }

           [Flags]
           public enum Stance : byte {
             None = 0,
             // 0-1 bits: AetherFlows
             AetherFlow1 = 1 << 0,
             AetherFlow2 = 1 << 1,
             AetherFlow3 = AetherFlow1 | AetherFlow2,
             // 2 bit: Phoenix Ready
             Phoenix = 1 << 2,
             // 3 bit: Solar Bahamut Ready
             // FIXME: guessed, not tested
             SolarBahamut = 1 << 3,
             // 4 bit: Unknown
             // 5-7 bits: Usable Arcanum
             Ruby = 1 << 5, // Fire/Ifrit
             Topaz = 1 << 6, // Earth/Titan
             Emerald = 1 << 7, // Wind/Garuda
           }

           [FieldOffset(0x00)]
           public ushort tranceMilliseconds;

           [FieldOffset(0x02)]
           public ushort attunementMilliseconds;

           /// <summary>
           /// 0x04: 0x17 = Summoned other than Carbuncle, 0x00 = Other Condition
           /// </summary>
           [NonSerialized]
           [FieldOffset(0x04)]
           private byte _summonStatus;

           /// <summary>
           /// (From right to left)
           /// 1-2 bits: Active Primal
           /// 3-5 bits: Counts of Attunement Stacks
           [NonSerialized]
           [FieldOffset(0x06)]
           private byte _attunement;

           [NonSerialized]
           [FieldOffset(0x07)]
           private Stance stance;

           public bool summonStatus {
             get {
               return _summonStatus != 0;
             }
           }

           public int attunement {
             get {
               return (_attunement >> 2) & 0x7; // = 0b111, to get the last 3 bits.
             }
           }

           public string activePrimal {
             get {
               return ((ActiveArcanum)(_attunement & 0x3)).ToString();
             }
           }

           public string[] usableArcanum {
             get {
               var arcanums = new List<string>();
               foreach (var flag in new List<Stance> { Stance.Ruby, Stance.Topaz, Stance.Emerald }) {
                 if (stance.HasFlag(flag))
                   arcanums.Add(flag.ToString());
               }

               return arcanums.ToArray();
             }
           }

           public string nextSummoned {
             get {
               foreach (var flag in new List<Stance> { Stance.SolarBahamut, Stance.Phoenix }) {
                 if (stance.HasFlag(flag))
                   return flag.ToString();
               }
               return "Bahamut";
             }
           }

           public int aetherflowStacks {
             get {
               return stance.HasFlag(Stance.AetherFlow3) ? 3 :
                      stance.HasFlag(Stance.AetherFlow2) ? 2 :
                      stance.HasFlag(Stance.AetherFlow1) ? 1 :
                      0;
             }
           }
         };

        [StructLayout(LayoutKind.Explicit)]
        public struct ScholarJobMemory
        {
            [FieldOffset(0x00)]
            public byte aetherflowStacks;

            [FieldOffset(0x01)]
            public byte fairyGauge;

            [FieldOffset(0x02)]
            public ushort fairyMilliseconds; // Seraph time left ms.

            [FieldOffset(0x04)]
            public byte
                fairyStatus; // Seraph: 6, else 0.
        };


        [StructLayout(LayoutKind.Explicit)]
        public struct MonkJobMemory
        {
            public enum Beast : byte
            {
                None = 0,
                Opo = 1,
                Raptor = 2,
                Coeurl = 3,
            }

            [FieldOffset(0x00)]
            public byte chakraStacks;

            [NonSerialized]
            [FieldOffset(0x01)]
            private Beast beastChakra1;

            [NonSerialized]
            [FieldOffset(0x02)]
            private Beast beastChakra2;

            [NonSerialized]
            [FieldOffset(0x03)]
            private Beast beastChakra3;

            [NonSerialized]
            [FieldOffset(0x04)]
            private byte Fury;

            [NonSerialized]
            [FieldOffset(0x05)]
            private byte Nadi;
            
            [FieldOffset(0x06)]
            public ushort MasterfulReadyMilisecond;

            public string[] beastChakra
            {
                get
                {
                    Beast[] _beasts = { beastChakra1, beastChakra2, beastChakra3 };
                    return _beasts.Select(a => a.ToString()).Where(a => a != "None").ToArray();
                }
            }

            public bool solarNadi
            {
                get
                {
                    if ((Nadi & 0x4) == 0x4)
                        return true;
                    else
                        return false;
                }
            }

            public bool lunarNadi
            {
                get
                {
                    if ((Nadi & 0x1) == 0x1)
                        return true;
                    else
                        return false;
                }
            }
            
            public int opoopoFury {
                get {
                    return Fury & 0x3;
                }
            }

            public int raptorFury {
                get {
                    return (Fury >> 2) & 0x3;
                }
            }

            public int coeurlFury {
                get {
                    return (Fury >> 4) & 0x3;
                }
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct MachinistJobMemory
        {
            [FieldOffset(0x00)]
            public ushort overheatMilliseconds;

            [FieldOffset(0x02)]
            public ushort batteryMilliseconds;

            [FieldOffset(0x04)]
            public byte heat;

            [FieldOffset(0x05)]
            public byte battery;

            [FieldOffset(0x06)]
            public byte lastBatteryAmount;

            [NonSerialized]
            [FieldOffset(0x07)]
            private byte chargeTimerState;

            public bool overheatActive => (chargeTimerState & 0x1) == 1;

            public bool robotActive => (chargeTimerState & 0x2) == 1;
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct AstrologianJobMemory
        {
            public enum Card : byte
            {
                None = 0,
                Balance = 1,
                Bole = 2,
                Arrow = 3,
                Spear = 4,
                Ewer = 5,
                Spire = 6,
                Lord = 7,
                Lady = 8,
            }

            [NonSerialized]
            [FieldOffset(0x00)]
            private ushort _card;

            [NonSerialized]
            [FieldOffset(0x02)]
            private byte _nextdraw;

            public string card1 {
                get {
                    return ((Card)(_card & 0xF)).ToString();
                }
            }

            public string card2 {
                get {
                    return ((Card)((_card >> 4) & 0xF)).ToString();
                }
            }

            public string card3 {
                get {
                    return ((Card)((_card >> 8) & 0xF)).ToString();
                }
            }

            public string card4 {
                get {
                    return ((Card)((_card >> 12) & 0xF)).ToString();
                }
            }

            public string nextdraw {
                get {
                    if (_nextdraw == 0)
                    {
                        return "Astral";
                    } else
                    {
                        return "Umbral";
                    }

                }
            }
        };

        [StructLayout(LayoutKind.Explicit)]
        public struct SamuraiJobMemory
        {
            [FieldOffset(0x03)]
            public byte kenki;

            [FieldOffset(0x04)]
            public byte meditationStacks;

            [NonSerialized]
            [FieldOffset(0x05)]
            private byte sen_bits;

            public bool setsu => (sen_bits & 0x1) != 0;

            public bool getsu => (sen_bits & 0x2) != 0;

            public bool ka => (sen_bits & 0x4) != 0;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SageJobMemory
        {
            [FieldOffset(0x00)]
            public ushort addersgallMilliseconds; // the addersgall gauge elapsed in milliseconds, from 0 to 19999.

            [FieldOffset(0x02)]
            public byte addersgall;

            [FieldOffset(0x03)]
            public byte addersting;

            [FieldOffset(0x04)]
            public byte eukrasia;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct ReaperJobMemory
        {
            [FieldOffset(0x00)]
            public byte soul;

            [FieldOffset(0x01)]
            public byte shroud;

            [FieldOffset(0x02)]
            public ushort enshroudMilliseconds;

            [FieldOffset(0x04)]
            public byte lemureShroud;

            [FieldOffset(0x05)]
            public byte voidShroud;
        }
        
        [StructLayout(LayoutKind.Explicit)]
        public struct ViperJobMemory {
            public enum AdvancedCombo : byte {
                Vicewinder = 1,
                HuntersCoil = 2,
                SwiftskinsCoil = 3,
                Vicepit = 4,
                HuntersDen = 5,
                SwiftskinsDen = 6,
                Reawaken = 7,
                FirstGeneration = 8,
                SecondGeneration = 9,
                ThirdGeneration = 10,
                FourthGeneration = 11,
            }

            [FieldOffset(0x00)]
            public byte rattlingCoilStacks;

            [FieldOffset(0x01)]
            public byte anguineTribute;

            [FieldOffset(0x02)]
            public byte serpentOffering;

            [NonSerialized]
            [FieldOffset(0x03)]
            private AdvancedCombo _advancedCombo;

            public string advancedCombo {
                get {
                    return _advancedCombo.ToString();
                }
            }

            [FieldOffset(0x06)]
            public ushort reawakenedTimer;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PictomancerJobMemory
        {
            [Flags]
            private enum CanvasFlags : byte
            {
                Pom = 1,
                Wing = 1 << 1,
                Claw = 1 << 2,
                Maw = 1 << 3,
                Weapon = 1 << 4,
                Landscape = 1 << 5,
            }

            [Flags]
            private enum CreatureFlags : byte
            {
                Pom = 1,
                Wing = 1 << 1,
                Claw = 1 << 2,

                // Maw = 1 << 3, // Once you paint the Maw motif, it becomes a Madeen portrait.
                MooglePortrait = 1 << 4,
                MadeenPortrait = 1 << 5,
            }

            [FieldOffset(0x00)]
            public byte palleteGauge;

            [FieldOffset(0x02)]
            public byte paint;

            [NonSerialized]
            [FieldOffset(0x03)]
            private CanvasFlags canvasFlags;

            public string creatureMotif
            {
                get
                {
                    if (canvasFlags.HasFlag(CanvasFlags.Pom))
                        return "Pom";
                    if (canvasFlags.HasFlag(CanvasFlags.Wing))
                        return "Wing";
                    if (canvasFlags.HasFlag(CanvasFlags.Claw))
                        return "Claw";
                    if (canvasFlags.HasFlag(CanvasFlags.Maw))
                        return "Maw";
                    return "None";
                }
            }

            public bool weaponMotif => canvasFlags.HasFlag(CanvasFlags.Weapon);
            public bool landscapeMotif => canvasFlags.HasFlag(CanvasFlags.Landscape);

            [NonSerialized]
            [FieldOffset(0x04)]
            private CreatureFlags creatureFlags;

            public string[] depictions
            {
                get
                {
                    var motifs = new List<string>();
                    if (creatureFlags.HasFlag(CreatureFlags.Pom))
                        motifs.Add("Pom");
                    if (creatureFlags.HasFlag(CreatureFlags.Wing))
                        motifs.Add("Wing");
                    if (creatureFlags.HasFlag(CreatureFlags.Claw))
                        motifs.Add("Claw");
                    return motifs.ToArray();
                }
            }

            public bool mooglePortrait => creatureFlags.HasFlag(CreatureFlags.MooglePortrait);
            public bool madeenPortrait => creatureFlags.HasFlag(CreatureFlags.MadeenPortrait);
        }
    }
}
