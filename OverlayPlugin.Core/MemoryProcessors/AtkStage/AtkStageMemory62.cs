using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage
{
    using AtkStage = global::FFXIVClientStructs.FFXIV.Component.GUI.AtkStage;

    internal interface IAtkStageMemory62 : IAtkStageMemory { }

    internal class AtkStageMemory62 : AtkStageMemory, IAtkStageMemory62
    {
        public AtkStageMemory62(TinyIoCContainer container) :
            base(container) { }

        public override Version GetVersion() => new(6, 2);

        public unsafe IntPtr GetAddonAddress(string name)
        {
            var atkStage = AtkStage.Instance();
            if (atkStage == null)
                return nint.Zero;

            var unitMgr = atkStage->RaptureAtkUnitManager;
            if (unitMgr == null)
                return nint.Zero;

            var addon = unitMgr->GetAddonByName(name);
            if (addon == null)
                return nint.Zero;

            return (IntPtr)addon;
        }

        private static readonly Dictionary<string, Type> AddonMap = new()
        {
            // These addon entries are confirmed from the FFXIVClientStructs repos
            { "_ActionCross", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionCross) },
            { "_ActionBar01", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar02", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionDoubleCrossL", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionDoubleCrossBase) },
            { "_ActionDoubleCrossR", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionDoubleCrossBase) },
            { "_CastBar", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonCastBar) },
            { "CharacterInspect", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonCharacterInspect) },
            { "ChatLogPanel_0", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonChatLogPanel) },
            { "ChatLogPanel_1", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonChatLogPanel) },
            { "ChatLogPanel_2", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonChatLogPanel) },
            { "ChatLogPanel_3", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonChatLogPanel) },
            { "ItemSearchResult", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonItemSearchResult) },
            { "_PartyList", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonPartyList) },
            { "Macro", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonMacro) },
            { "Teleport", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonTeleport) },

            // These addons are guessed based on patterns
            { "_ActionBar", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar03", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar04", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar05", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar06", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar07", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar08", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBar09", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },
            { "_ActionBarEx", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonActionBarX) },

            // These addons are guessed based on names matching up or based on github code search
            { "AOZNotebook", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonAOZNotebook) },
            { "ChocoboBreedTraining", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonChocoboBreedTraining) },
            { "ContentsFinder", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonContentsFinder) },
            { "ContentsFinderConfirm", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonContentsFinderConfirm) },
            { "ContextIconMenu", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonContextIconMenu) },
            { "ContextMenu", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonContextMenu) },
            { "_EnemyList", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonEnemyList) },
            { "_Exp", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonExp) },
            { "FateReward", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonFateReward) },
            { "FieldMarker", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonFieldMarker) },
            { "Gathering", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonGathering) },
            { "GatheringMasterpiece", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonGatheringMasterpiece) },
            {
                "GrandCompanySupplyReward",
                typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonGrandCompanySupplyReward)
            },
            { "GuildLeve", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonGuildLeve) },
            { "_HudLayoutScreen", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonHudLayoutScreen) },
            { "_HudLayoutWindow", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonHudLayoutWindow) },
            { "ItemInspectionList", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonItemInspectionList) },
            { "ItemInspectionResult", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonItemInspectionResult) },
            { "JournalDetail", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonJournalDetail) },
            { "JournalResult", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonJournalResult) },
            { "LotteryDaily", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonLotteryDaily) },
            { "MaterializeDialog", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonMaterializeDialog) },
            { "MateriaRetrieveDialog", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonMateriaRetrieveDialog) },
            { "NamePlate", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonNamePlate) },
            { "NeedGreed", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonNeedGreed) },
            { "RaceChocoboResult", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRaceChocoboResult) },
            { "RecipeNote", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRecipeNote) },
            { "ReconstructionBox", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonReconstructionBox) },
            { "RelicNoteBook", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRelicNoteBook) },
            { "Repair", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRepair) },
            { "Request", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRequest) },
            { "RetainerList", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerList) },
            { "RetainerSell", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerSell) },
            { "RetainerTaskAsk", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerTaskAsk) },
            { "RetainerTaskList", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerTaskList) },
            { "RetainerTaskResult", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonRetainerTaskResult) },
            { "SalvageDialog", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSalvageDialog) },
            { "SalvageItemSelector", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSalvageItemSelector) },
            { "SatisfactionSupply", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSatisfactionSupply) },
            { "SelectIconString", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSelectIconString) },
            { "SelectOk", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSelectOk) },
            { "SelectString", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSelectString) },
            // Both of these seem to exist in memory somehow??
            { "SelectYesno", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSelectYesno) },
            { "_SelectYesNo", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSelectYesno) },
            { "ShopCardDialog", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonShopCardDialog) },
            { "Synthesis", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonSynthesis) },
            { "Talk", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonTalk) },
            { "WeeklyBingo", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonWeeklyBingo) },
            { "WeeklyPuzzle", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonWeeklyPuzzle) },


            // These addons are known to exist but not mapped yet:
            // (double entries are intentional, they appear twice in the list in game memory)
            /**
            Achievement
            ActionDetail
            ActionMenu
            AddonContextMenuTitle
            AddonContextSub
            AdventureNoteBook
            AetherCurrent
            AreaMap
            ArmouryBoard
            Character
            CharacterStatus
            ChatLog
            CircleFinder
            CircleList
            ConfigKeybind
            ConfigSystem
            ContactList
            ContentsInfo
            ContentsNote
            ContentsReplaySetting
            CountDownSettingDialog
            CrossWorldLinkshell
            Currency
            CursorAddon
            CursorLocation
            Dawn
            DawnStory
            DragDropS
            Emote
            FadeBack
            FadeMiddle
            FateProgress
            Filter
            FilterSystem
            FishGuide2
            FishingNote
            FreeCompany
            FreeCompanyTopics
            GSInfoGeneral
            GatheringNote
            GoldSaucerInfo
            HousingMenu
            HowToList
            Hud
            HudLayout
            Inventory
            InventoryCrystalGrid
            InventoryCrystalGrid
            InventoryEventGrid0
            InventoryEventGrid0E
            InventoryEventGrid1
            InventoryEventGrid1E
            InventoryEventGrid2
            InventoryEventGrid2E
            InventoryExpansion
            InventoryGrid
            InventoryGrid0
            InventoryGrid0E
            InventoryGrid1
            InventoryGrid1E
            InventoryGrid2E
            InventoryGrid3E
            InventoryGridCrystal
            InventoryLarge
            ItemDetail
            JobHudWHM
            JobHudWHM0
            Journal
            JournalDetail
            JournalDetail
            LicenseViewer
            LinkShell
            LoadingTips
            LookingForGroup
            Marker
            McGuffin
            MinionNoteBook
            MiragePrismPrismItemDetail
            MonsterNote
            MountNoteBook
            MountSpeed
            NowLoading
            OperationGuide
            Orchestrion
            OrnamentNoteBook
            PlayGuide
            PvpProfile
            PvpProfileColosseum
            QuestRedoHud
            RecommendList
            ScenarioTree
            ScreenFrameSystem
            ScreenLog
            Social
            SocialList
            SupportDesk
            Tooltip
            VVDFinder
            WebLauncher
            _ActionContents
            _AllianceList1
            _AllianceList2
            _AreaText
            _AreaText
            _BagWidget
            _BattleTalk
            _ContentGauge
            _DTR
            _FlyText
            _FocusTargetInfo
            _Image
            _Image
            _Image3
            _Image3
            _LimitBreak
            _LocationTitle
            _LocationTitleShort
            _MainCommand
            _MainCross
            _MiniTalk
            _Money
            _NaviMap
            _Notification
            _ParameterWidget
            _PoisonText
            _PoisonText
            _PopUpText
            _ScreenInfoBack
            _ScreenInfoFront
            _ScreenText
            _Status
            _StatusCustom0
            _StatusCustom1
            _StatusCustom2
            _StatusCustom3
            _TargetCursor
            _TargetCursorGround
            _TargetInfo
            _TargetInfoBuffDebuff
            _TargetInfoCastBar
            _TargetInfoMainTarget
            _TextChain
            _TextClassChange
            _TextError
            _ToDoList
            _WideText
            _WideText
            */
        };

        public T? GetAddon<T>() where T : struct
        {
            var name = AddonMap.FirstOrDefault(x => x.Value == typeof(T)).Key;
            return (T?)GetAddon(name);
        }
        
        public object GetAddon(string name)
        {
            if (!AddonMap.ContainsKey(name) || !IsValid())
                return null;

            var ptr = GetAddonAddress(name);
            if (ptr == nint.Zero) return null;
            var addonType = AddonMap[name];
            
            var addon = Marshal.PtrToStructure(ptr, addonType);
            return addon;
        }
    }
}
