using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Gauge;
#if !DEBUG
using Newtonsoft.Json;
#endif

namespace RainbowMage.OverlayPlugin.MemoryProcessors.JobGauge
{
    public enum JobGaugeJob : byte
    {
        None = 0,
        GLA = 1,
        PGL = 2,
        MRD = 3,
        LNC = 4,
        ARC = 5,
        CNJ = 6,
        THM = 7,
        CRP = 8,
        BSM = 9,
        ARM = 10,
        GSM = 11,
        LTW = 12,
        WVR = 13,
        ALC = 14,
        CUL = 15,
        MIN = 16,
        BTN = 17,
        FSH = 18,
        PLD = 19,
        MNK = 20,
        WAR = 21,
        DRG = 22,
        BRD = 23,
        WHM = 24,
        BLM = 25,
        ACN = 26,
        SMN = 27,
        SCH = 28,
        ROG = 29,
        NIN = 30,
        MCH = 31,
        DRK = 32,
        AST = 33,
        SAM = 34,
        RDM = 35,
        BLU = 36,
        GNB = 37,
        DNC = 38,
        RPR = 39,
        SGE = 40,
    }

    public interface IJobGauge : IEquatable<IJobGauge>
    {
        JobGaugeJob Job { get; }
        IBaseJobGauge Data { get; }
        int[] RawData { get; }
#if !DEBUG
            [JsonIgnore]
#endif
        object BaseObject { get; }
    }

    public interface IJobGaugeMemory : IVersionedMemory
    {
        IJobGauge GetJobGauge();
    }

    class JobGaugeMemoryManager : IJobGaugeMemory
    {
        private readonly TinyIoCContainer container;
        private readonly FFXIVRepository repository;
        protected readonly ILogger logger;
        private IJobGaugeMemory memory = null;

        public JobGaugeMemoryManager(TinyIoCContainer container)
        {
            this.container = container;
            container.Register<IJobGaugeMemory655, JobGaugeMemory655>();
            container.Register<IJobGaugeMemory74, JobGaugeMemory74>();
            repository = container.Resolve<FFXIVRepository>();
            logger = container.Resolve<ILogger>();

            var memory = container.Resolve<FFXIVMemory>();
            memory.RegisterOnProcessChangeHandler(FindMemory);
        }

        private void FindMemory(object sender, Process p)
        {
            memory = null;
            if (p == null)
            {
                return;
            }
            ScanPointers();
        }

        public void ScanPointers()
        {
            List<IJobGaugeMemory> candidates = new List<IJobGaugeMemory>();
            candidates.Add(container.Resolve<IJobGaugeMemory655>());
            candidates.Add(container.Resolve<IJobGaugeMemory74>());
            memory = FFXIVMemory.FindCandidate(candidates, repository.GetMachinaRegion());
        }

        public bool IsValid()
        {
            if (memory == null || !memory.IsValid())
            {
                return false;
            }
            return true;
        }

        Version IVersionedMemory.GetVersion()
        {
            if (!IsValid())
                return null;
            return memory.GetVersion();
        }

        public IJobGauge GetJobGauge()
        {
            if (!IsValid())
                return null;
            return memory.GetJobGauge();
        }
    }

    public interface IBaseJobGauge
    {
    }

    #region Healer

    public interface IBaseWhiteMageGauge : IBaseJobGauge
    {
        short LilyTimer { get; }
        byte Lily { get; }
        byte BloodLily { get; }
    }

    public interface IBaseScholarGauge : IBaseJobGauge
    {
        byte Aetherflow { get; }
        byte FairyGauge { get; }
        short SeraphTimer { get; }
        byte DismissedFairy { get; }
    }

    public interface IBaseAstrologianGauge : IBaseJobGauge
    {
        short Timer { get; }
        byte Card { get; }
        byte Seals { get; }

        AstrologianCard CurrentCard { get; }

        AstrologianSeal[] CurrentSeals { get; }
    }

    public interface IBaseSageGauge : IBaseJobGauge
    {
        short AddersgallTimer { get; }
        byte Addersgall { get; }
        byte Addersting { get; }
        byte Eukrasia { get; }

        bool EukrasiaActive { get; }
    }

    #endregion

    #region MagicDPS

    public interface IBaseBlackMageGauge : IBaseJobGauge
    {
        short EnochianTimer { get; }
        short ElementTimeRemaining { get; }
        sbyte ElementStance { get; }
        byte UmbralHearts { get; }
        byte PolyglotStacks { get; }
        EnochianFlags EnochianFlags { get; }

        int UmbralStacks { get; }
        int AstralStacks { get; }
        bool EnochianActive { get; }
        bool ParadoxActive { get; }
    }

    public interface IBaseSummonerGauge : IBaseJobGauge
    {
        ushort SummonTimer { get; }
        ushort AttunementTimer { get; }
        byte ReturnSummon { get; }
        byte ReturnSummonGlam { get; }
        byte Attunement { get; }
        AetherFlags AetherFlags { get; }
    }

    public interface IBaseRedMageGauge : IBaseJobGauge
    {
        byte WhiteMana { get; }
        byte BlackMana { get; }
        byte ManaStacks { get; }
    }

    #endregion

    #region RangeDPS

    public interface IBaseBardGauge : IBaseJobGauge
    {
        ushort SongTimer { get; }
        byte Repertoire { get; }
        byte SoulVoice { get; }
        SongFlags SongFlags { get; }
    }

    public interface IBaseMachinistGauge : IBaseJobGauge
    {
        short OverheatTimeRemaining { get; }
        short SummonTimeRemaining { get; }
        byte Heat { get; }
        byte Battery { get; }
        byte LastSummonBatteryPower { get; }
        byte TimerActive { get; }
    }

    public interface IBaseDancerGauge : IBaseJobGauge
    {
        byte Feathers { get; }
        byte Esprit { get; }
        byte[] DanceSteps { get; }
        byte StepIndex { get; }

        DanceStep CurrentStep { get; }
    }

    #endregion

    #region MeleeDPS

    public interface IBaseMonkGauge : IBaseJobGauge
    {
        byte Chakra { get; }

        BeastChakraType BeastChakra1 { get; }

        BeastChakraType BeastChakra2 { get; }

        BeastChakraType BeastChakra3 { get; }

        NadiFlags Nadi { get; }
        ushort BlitzTimeRemaining { get; }

        BeastChakraType[] BeastChakra { get; }
    }

    public interface IBaseDragoonGauge : IBaseJobGauge
    {
        short LotdTimer { get; }
        byte LotdState { get; }
        byte EyeCount { get; }
        byte FirstmindsFocusCount { get; }
    }

    public interface IBaseNinjaGauge : IBaseJobGauge
    {
        ushort HutonTimer { get; }
        byte Ninki { get; }
        byte HutonManualCasts { get; }
    }

    public interface IBaseSamuraiGauge : IBaseJobGauge
    {
        KaeshiAction Kaeshi { get; }
        byte Kenki { get; }
        byte MeditationStacks { get; }
        SenFlags SenFlags { get; }
    }

    public interface IBaseReaperGauge : IBaseJobGauge
    {
        byte Soul { get; }
        byte Shroud { get; }
        ushort EnshroudedTimeRemaining { get; }
        byte LemureShroud { get; }
        byte VoidShroud { get; }
    }

    #endregion

    #region Tanks

    public interface IBaseDarkKnightGauge : IBaseJobGauge
    {
        byte Blood { get; }
        ushort DarksideTimer { get; }
        byte DarkArtsState { get; }
        ushort ShadowTimer { get; }
    }

    public interface IBasePaladinGauge : IBaseJobGauge
    {
        byte OathGauge { get; }
    }

    public interface IBaseWarriorGauge : IBaseJobGauge
    {
        byte BeastGauge { get; }
    }

    public interface IBaseGunbreakerGauge : IBaseJobGauge
    {
        byte Ammo { get; }
        short MaxTimerDuration { get; }
        byte AmmoComboStep { get; }
    }

    public enum AstrologianCard
    {
        None = 0,
        Balance = 1,
        Bole = 2,
        Arrow = 3,
        Spear = 4,
        Ewer = 5,
        Spire = 6,
        Lord = 0x70,
        Lady = 0x80
    }

    public enum AstrologianSeal
    {
        Solar = 1,
        Lunar = 2,
        Celestial = 3
    }

    public enum DanceStep : byte
    {
        Finish = 0,
        Emboite = 1,
        Entrechat = 2,
        Jete = 3,
        Pirouette = 4
    }

    [Flags]
    public enum EnochianFlags : byte
    {
        None = 0,
        Enochian = 1,
        Paradox = 2
    }

    public enum KaeshiAction : byte
    {
        Higanbana = 1,
        Goken = 2,
        Setsugekka = 3,
        Namikiri = 4
    }

    [Flags]
    public enum SenFlags : byte
    {
        None = 0,
        Setsu = 1 << 0,
        Getsu = 1 << 1,
        Ka = 1 << 2
    }

    [Flags]
    public enum SongFlags : byte
    {
        None = 0,
        MagesBallad = 1 << 0,
        ArmysPaeon = 1 << 1,
        WanderersMinuet = MagesBallad | ArmysPaeon,
        MagesBalladLastPlayed = 1 << 2,
        ArmysPaeonLastPlayed = 1 << 3,
        WanderersMinuetLastPlayed = MagesBalladLastPlayed | ArmysPaeonLastPlayed,
        MagesBalladCoda = 1 << 4,
        ArmysPaeonCoda = 1 << 5,
        WanderersMinuetCoda = 1 << 6
    }

    [Flags]
    public enum AetherFlags : byte
    {
        None = 0,
        Aetherflow1 = 1 << 0,
        Aetherflow2 = 1 << 1,
        Aetherflow = Aetherflow1 | Aetherflow2,
        IfritAttuned = 1 << 2,
        TitanAttuned = 1 << 3,
        GarudaAttuned = TitanAttuned | IfritAttuned,
        PhoenixReady = 1 << 4,
        IfritReady = 1 << 5,
        TitanReady = 1 << 6,
        GarudaReady = 1 << 7
    }

    public enum BeastChakraType : byte
    {
        None = 0,
        Coeurl = 1,
        OpoOpo = 2,
        Raptor = 3
    }

    [Flags]
    public enum NadiFlags : byte
    {
        Lunar = 2,
        Solar = 4
    }

    #endregion
}
