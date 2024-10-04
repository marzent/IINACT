using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    // @TODO: These need updated for 7.0 still, this is just for initial compatibility as a copy/paste from the 6.55 file
    partial class JobGaugeMemory70 : JobGaugeMemory, IJobGaugeMemory70
    {
        // Due to lack of multi-version support in FFXIVClientStructs, we need to duplicate these structures here per-version
        // We use FFXIVClientStructs versions of the structs because they have more required details than FFXIV_ACT_Plugin's struct definitions
        #region FFXIVClientStructs structs
        [StructLayout(LayoutKind.Explicit, Size = 0x60)]
        public unsafe partial struct JobGaugeManager
        {
            [JsonIgnore]
            [FieldOffset(0x00)] public JobGauge* CurrentGauge;

            [FieldOffset(0x08)] public JobGauge EmptyGauge;

            [FieldOffset(0x08)] public WhiteMageGauge WhiteMage;
            [FieldOffset(0x08)] public ScholarGauge Scholar;
            [FieldOffset(0x08)] public AstrologianGauge Astrologian;
            [FieldOffset(0x08)] public SageGauge Sage;

            [FieldOffset(0x08)] public BardGauge Bard;
            [FieldOffset(0x08)] public MachinistGauge Machinist;
            [FieldOffset(0x08)] public DancerGauge Dancer;

            [FieldOffset(0x08)] public BlackMageGauge BlackMage;
            [FieldOffset(0x08)] public SummonerGauge Summoner;
            [FieldOffset(0x08)] public RedMageGauge RedMage;

            [FieldOffset(0x08)] public MonkGauge Monk;
            [FieldOffset(0x08)] public DragoonGauge Dragoon;
            [FieldOffset(0x08)] public NinjaGauge Ninja;
            [FieldOffset(0x08)] public SamuraiGauge Samurai;
            [FieldOffset(0x08)] public ReaperGauge Reaper;

            [FieldOffset(0x08)] public DarkKnightGauge DarkKnight;
            [FieldOffset(0x08)] public PaladinGauge Paladin;
            [FieldOffset(0x08)] public WarriorGauge Warrior;
            [FieldOffset(0x08)] public GunbreakerGauge Gunbreaker;

            [JsonIgnore]
            [FieldOffset(0x10)] public fixed byte RawGaugeData[8];

            [FieldOffset(0x58)] public byte ClassJobID;

            public byte[] GetRawGaugeData => new byte[] {
                RawGaugeData[0], RawGaugeData[1], RawGaugeData[2], RawGaugeData[3],
                RawGaugeData[4], RawGaugeData[5], RawGaugeData[6], RawGaugeData[7]
            };
        }
        [StructLayout(LayoutKind.Explicit, Size = 0x08)]
        public struct JobGauge : IBaseJobGauge
        {
            // empty base class for other gauges, this only has the vtable
        }

        #region Healer

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WhiteMageGauge : IBaseWhiteMageGauge
        {
            [FieldOffset(0x0A)] public short LilyTimer;
            [FieldOffset(0x0C)] public byte Lily;
            [FieldOffset(0x0D)] public byte BloodLily;

            short IBaseWhiteMageGauge.LilyTimer => LilyTimer;

            byte IBaseWhiteMageGauge.Lily => Lily;

            byte IBaseWhiteMageGauge.BloodLily => BloodLily;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ScholarGauge : IBaseScholarGauge
        {
            [FieldOffset(0x08)] public byte Aetherflow;
            [FieldOffset(0x09)] public byte FairyGauge;
            [FieldOffset(0x0A)] public short SeraphTimer;
            [FieldOffset(0x0C)] public byte DismissedFairy;

            byte IBaseScholarGauge.Aetherflow => Aetherflow;

            byte IBaseScholarGauge.FairyGauge => FairyGauge;

            short IBaseScholarGauge.SeraphTimer => SeraphTimer;

            byte IBaseScholarGauge.DismissedFairy => DismissedFairy;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct AstrologianGauge : IBaseAstrologianGauge
        {
            [FieldOffset(0x08)] public short Timer;
            [FieldOffset(0x0D)] public byte Card;
            [FieldOffset(0x0E)] public byte Seals; // 6 bits, 0,1-3,1-3,1-3 depending on astrosign

            public AstrologianCard CurrentCard => (AstrologianCard)Card;

            public AstrologianSeal[] CurrentSeals => new[]
            {
                (AstrologianSeal)(3 & (this.Seals >> 0)),
                (AstrologianSeal)(3 & (this.Seals >> 2)),
                (AstrologianSeal)(3 & (this.Seals >> 4)),
            };

            short IBaseAstrologianGauge.Timer => Timer;

            byte IBaseAstrologianGauge.Card => Card;

            byte IBaseAstrologianGauge.Seals => Seals;

            AstrologianCard IBaseAstrologianGauge.CurrentCard => CurrentCard;

            AstrologianSeal[] IBaseAstrologianGauge.CurrentSeals => CurrentSeals;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SageGauge : IBaseSageGauge
        {
            [FieldOffset(0x08)] public short AddersgallTimer;
            [FieldOffset(0x0A)] public byte Addersgall;
            [FieldOffset(0x0B)] public byte Addersting;
            [FieldOffset(0x0C)] public byte Eukrasia;

            public bool EukrasiaActive => Eukrasia > 0;

            short IBaseSageGauge.AddersgallTimer => AddersgallTimer;

            byte IBaseSageGauge.Addersgall => Addersgall;

            byte IBaseSageGauge.Addersting => Addersting;

            byte IBaseSageGauge.Eukrasia => Eukrasia;
        }

        #endregion

        #region MagicDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x30)]
        public struct BlackMageGauge : IBaseBlackMageGauge
        {
            [FieldOffset(0x08)] public short EnochianTimer;
            [FieldOffset(0x0A)] public short ElementTimeRemaining;
            [FieldOffset(0x0C)] public sbyte ElementStance;
            [FieldOffset(0x0D)] public byte UmbralHearts;
            [FieldOffset(0x0E)] public byte PolyglotStacks;
            [FieldOffset(0x0F)] public EnochianFlags EnochianFlags;

            public int UmbralStacks => ElementStance >= 0 ? 0 : ElementStance * -1;
            public int AstralStacks => ElementStance <= 0 ? 0 : ElementStance;
            public bool EnochianActive => EnochianFlags.HasFlag(EnochianFlags.Enochian);
            public bool ParadoxActive => EnochianFlags.HasFlag(EnochianFlags.Paradox);

            short IBaseBlackMageGauge.EnochianTimer => EnochianTimer;

            short IBaseBlackMageGauge.ElementTimeRemaining => ElementTimeRemaining;

            sbyte IBaseBlackMageGauge.ElementStance => ElementStance;

            byte IBaseBlackMageGauge.UmbralHearts => UmbralHearts;

            byte IBaseBlackMageGauge.PolyglotStacks => PolyglotStacks;

            EnochianFlags IBaseBlackMageGauge.EnochianFlags => EnochianFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SummonerGauge : IBaseSummonerGauge
        {
            [FieldOffset(0x8)] public ushort SummonTimer; // millis counting down
            [FieldOffset(0xA)] public ushort AttunementTimer; // millis counting down
            [FieldOffset(0xC)] public byte ReturnSummon; // Pet sheet (23=Carbuncle, the only option now)
            [FieldOffset(0xD)] public byte ReturnSummonGlam; // PetMirage sheet
            [FieldOffset(0xE)] public byte Attunement; // Count of "Attunement cost" resource
            [FieldOffset(0xF)] public AetherFlags AetherFlags; // bitfield

            ushort IBaseSummonerGauge.SummonTimer => SummonTimer;

            ushort IBaseSummonerGauge.AttunementTimer => AttunementTimer;

            byte IBaseSummonerGauge.ReturnSummon => ReturnSummon;

            byte IBaseSummonerGauge.ReturnSummonGlam => ReturnSummonGlam;

            byte IBaseSummonerGauge.Attunement => Attunement;

            AetherFlags IBaseSummonerGauge.AetherFlags => AetherFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x50)]
        public struct RedMageGauge : IBaseRedMageGauge
        {
            [FieldOffset(0x08)] public byte WhiteMana;
            [FieldOffset(0x09)] public byte BlackMana;
            [FieldOffset(0x0A)] public byte ManaStacks;

            byte IBaseRedMageGauge.WhiteMana => WhiteMana;

            byte IBaseRedMageGauge.BlackMana => BlackMana;

            byte IBaseRedMageGauge.ManaStacks => ManaStacks;
        }

        #endregion

        #region RangeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct BardGauge : IBaseBardGauge
        {
            [FieldOffset(0x08)] public ushort SongTimer;
            [FieldOffset(0x0C)] public byte Repertoire;
            [FieldOffset(0x0D)] public byte SoulVoice;
            [FieldOffset(0x0E)] public SongFlags SongFlags; // bitfield

            ushort IBaseBardGauge.SongTimer => SongTimer;

            byte IBaseBardGauge.Repertoire => Repertoire;

            byte IBaseBardGauge.SoulVoice => SoulVoice;

            SongFlags IBaseBardGauge.SongFlags => SongFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MachinistGauge : IBaseMachinistGauge
        {
            [FieldOffset(0x08)] public short OverheatTimeRemaining;
            [FieldOffset(0x0A)] public short SummonTimeRemaining;
            [FieldOffset(0x0C)] public byte Heat;
            [FieldOffset(0x0D)] public byte Battery;
            [FieldOffset(0x0E)] public byte LastSummonBatteryPower;
            [FieldOffset(0x0F)] public byte TimerActive;

            short IBaseMachinistGauge.OverheatTimeRemaining => OverheatTimeRemaining;

            short IBaseMachinistGauge.SummonTimeRemaining => SummonTimeRemaining;

            byte IBaseMachinistGauge.Heat => Heat;

            byte IBaseMachinistGauge.Battery => Battery;

            byte IBaseMachinistGauge.LastSummonBatteryPower => LastSummonBatteryPower;

            byte IBaseMachinistGauge.TimerActive => TimerActive;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public unsafe struct DancerGauge : IBaseDancerGauge
        {
            [FieldOffset(0x08)] public byte Feathers;
            [FieldOffset(0x09)] public byte Esprit;
            [FieldOffset(0x0A)] public fixed byte DanceSteps[4];
            [FieldOffset(0x0E)] public byte StepIndex;

            public DanceStep CurrentStep => (DanceStep)(StepIndex >= 4 ? 0 : DanceSteps[StepIndex]);

            byte IBaseDancerGauge.Feathers => Feathers;

            byte IBaseDancerGauge.Esprit => Esprit;

            byte[] IBaseDancerGauge.DanceSteps => new byte[] { DanceSteps[0], DanceSteps[1], DanceSteps[2], DanceSteps[3] };

            byte IBaseDancerGauge.StepIndex => StepIndex;
        }

        #endregion

        #region MeleeDPS

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct MonkGauge : IBaseMonkGauge
        {
            [FieldOffset(0x08)] public byte Chakra; // Chakra count

            [FieldOffset(0x09)]
            public BeastChakraType BeastChakra1; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0A)]
            public BeastChakraType BeastChakra2; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0B)]
            public BeastChakraType BeastChakra3; // CoeurlChakra = 1, RaptorChakra = 2, OpoopoChakra = 3 (only one value)

            [FieldOffset(0x0C)] public NadiFlags Nadi; // LunarNadi = 2, SolarNadi = 4 (If both then 2+4=6)
            [FieldOffset(0x0E)] public ushort BlitzTimeRemaining; // 20 seconds

            public BeastChakraType[] BeastChakra => new[] { BeastChakra1, BeastChakra2, BeastChakra3 };

            byte IBaseMonkGauge.Chakra => Chakra;

            BeastChakraType IBaseMonkGauge.BeastChakra1 => BeastChakra1;

            BeastChakraType IBaseMonkGauge.BeastChakra2 => BeastChakra2;

            BeastChakraType IBaseMonkGauge.BeastChakra3 => BeastChakra3;

            NadiFlags IBaseMonkGauge.Nadi => Nadi;

            ushort IBaseMonkGauge.BlitzTimeRemaining => BlitzTimeRemaining;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DragoonGauge : IBaseDragoonGauge
        {
            [FieldOffset(0x08)] public short LotdTimer;
            [FieldOffset(0x0A)] public byte LotdState; // This seems to only ever be 0 or 2 now
            [FieldOffset(0x0B)] public byte EyeCount;
            [FieldOffset(0x0C)] public byte FirstmindsFocusCount;

            short IBaseDragoonGauge.LotdTimer => LotdTimer;

            byte IBaseDragoonGauge.LotdState => LotdState;

            byte IBaseDragoonGauge.EyeCount => EyeCount;

            byte IBaseDragoonGauge.FirstmindsFocusCount => FirstmindsFocusCount;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct NinjaGauge : IBaseNinjaGauge
        {
            [FieldOffset(0x08)] public ushort HutonTimer;
            [FieldOffset(0x0A)] public byte Ninki;
            [FieldOffset(0x0B)] public byte HutonManualCasts;

            ushort IBaseNinjaGauge.HutonTimer => HutonTimer;

            byte IBaseNinjaGauge.Ninki => Ninki;

            byte IBaseNinjaGauge.HutonManualCasts => HutonManualCasts;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct SamuraiGauge : IBaseSamuraiGauge
        {
            [FieldOffset(0x0A)] public KaeshiAction Kaeshi;
            [FieldOffset(0x0B)] public byte Kenki;
            [FieldOffset(0x0C)] public byte MeditationStacks;
            [FieldOffset(0x0D)] public SenFlags SenFlags;

            KaeshiAction IBaseSamuraiGauge.Kaeshi => Kaeshi;

            byte IBaseSamuraiGauge.Kenki => Kenki;

            byte IBaseSamuraiGauge.MeditationStacks => MeditationStacks;

            SenFlags IBaseSamuraiGauge.SenFlags => SenFlags;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct ReaperGauge : IBaseReaperGauge
        {
            [FieldOffset(0x08)] public byte Soul;
            [FieldOffset(0x09)] public byte Shroud;
            [FieldOffset(0x0A)] public ushort EnshroudedTimeRemaining;
            [FieldOffset(0x0C)] public byte LemureShroud;
            [FieldOffset(0x0D)] public byte VoidShroud;

            byte IBaseReaperGauge.Soul => Soul;

            byte IBaseReaperGauge.Shroud => Shroud;

            ushort IBaseReaperGauge.EnshroudedTimeRemaining => EnshroudedTimeRemaining;

            byte IBaseReaperGauge.LemureShroud => LemureShroud;

            byte IBaseReaperGauge.VoidShroud => VoidShroud;
        }

        #endregion

        #region Tanks

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct DarkKnightGauge : IBaseDarkKnightGauge
        {
            [FieldOffset(0x08)] public byte Blood;
            [FieldOffset(0x0A)] public ushort DarksideTimer;
            [FieldOffset(0x0C)] public byte DarkArtsState;
            [FieldOffset(0x0E)] public ushort ShadowTimer;

            byte IBaseDarkKnightGauge.Blood => Blood;

            ushort IBaseDarkKnightGauge.DarksideTimer => DarksideTimer;

            byte IBaseDarkKnightGauge.DarkArtsState => DarkArtsState;

            ushort IBaseDarkKnightGauge.ShadowTimer => ShadowTimer;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct PaladinGauge : IBasePaladinGauge
        {
            [FieldOffset(0x08)] public byte OathGauge;

            byte IBasePaladinGauge.OathGauge => OathGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct WarriorGauge : IBaseWarriorGauge
        {
            [FieldOffset(0x08)] public byte BeastGauge;

            byte IBaseWarriorGauge.BeastGauge => BeastGauge;
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x10)]
        public struct GunbreakerGauge : IBaseGunbreakerGauge
        {
            [FieldOffset(0x08)] public byte Ammo;
            [FieldOffset(0x0A)] public short MaxTimerDuration;
            [FieldOffset(0x0C)] public byte AmmoComboStep;

            byte IBaseGunbreakerGauge.Ammo => Ammo;

            short IBaseGunbreakerGauge.MaxTimerDuration => MaxTimerDuration;

            byte IBaseGunbreakerGauge.AmmoComboStep => AmmoComboStep;
        }

        #endregion

        #endregion
    }
}
