using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkGui.FFXIVClientStructs;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage
{
    using AtkStage = global::FFXIVClientStructs.FFXIV.Component.GUI.AtkStage;
    interface IAtkStageMemory62 : IAtkStageMemory { }

    class AtkStageMemory62 : AtkStageMemory, IAtkStageMemory62
    {
        private static long GetAtkStageSingletonAddress(TinyIoCContainer container)
        {
            var data = container.Resolve<FFXIVClientStructs.Data>();
            return (long)data.GetClassInstanceAddress(FFXIVClientStructs.DataNamespace.Global, "Component::GUI::AtkStage");
        }

        public AtkStageMemory62(TinyIoCContainer container) : base(container, GetAtkStageSingletonAddress(container)) { }

        public override Version GetVersion()
        {
            return new Version(6, 2);
        }

        public unsafe IntPtr GetAddonAddress(string name)
        {
            if (!IsValid())
            {
                return IntPtr.Zero;
            }

            // Our current address points to an instance of AtkStage
            // We need to traverse the object to AtkUnitManager, then check each pointer to see if it's the addon we're looking for

            if (atkStageInstanceAddress.ToInt64() == 0)
            {
                return IntPtr.Zero;
            }
            dynamic atkStage = ManagedType<AtkStage>.GetManagedTypeFromIntPtr(atkStageInstanceAddress, memory);
            dynamic raptureAtkUnitManager = atkStage.RaptureAtkUnitManager;
            dynamic unitMgr = raptureAtkUnitManager.AtkUnitManager;
            AtkUnitList list = unitMgr.AllLoadedUnitsList;
            long* entries = (long*)&list.AtkUnitEntries;

            for (var i = 0; i < list.Count; ++i)
            {
                var ptr = new IntPtr(entries[i]);
                dynamic atkUnit = ManagedType<AtkUnitBase>.GetManagedTypeFromIntPtr(ptr, memory);
                byte[] atkUnitName = atkUnit.Name;

                var atkUnitNameValue = FFXIVMemory.GetStringFromBytes(atkUnitName, 0, atkUnitName.Length);
                if (atkUnitNameValue.Equals(name))
                {
                    return atkUnit.ptr;
                }
            }

            return IntPtr.Zero;
        }

        private static Dictionary<string, Type> AddonMap = new Dictionary<string, Type>() {
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
            { "Teleport", typeof(global::FFXIVClientStructs.FFXIV.Client.UI.AddonTeleport) },

            // These addons are known to exist but not mapped yet:
            /*
             FadeBack
             FadeMiddle
             NowLoading
             Filter
             ScreenFrameSystem
             FilterSystem
             ContextMenu
             ContextIconMenu
             AddonContextMenuTitle
             AddonContextSub
             Tooltip
             CursorLocation
             InventoryGrid
             InventoryGridCrystal
             Inventory
             InventoryGrid1
             InventoryGrid0
             InventoryEventGrid0
             InventoryEventGrid1
             InventoryEventGrid2
             InventoryCrystalGrid
             InventoryLarge
             InventoryGrid3E
             InventoryGrid2E
             InventoryGrid1E
             InventoryGrid0E
             InventoryEventGrid2E
             InventoryEventGrid1E
             InventoryEventGrid0E
             InventoryCrystalGrid
             InventoryExpansion
             ChatLog
             Talk
             AreaMap
             NamePlate
             ScreenLog
             ItemDetail
             MiragePrismPrismItemDetail
             ActionDetail
             DragDropS
             LoadingTips
             Hud
             _Money
             _MainCommand
             _MainCross
             _ParameterWidget
             _Status
             _StatusCustom0
             _StatusCustom1
             _StatusCustom2
             _StatusCustom3
             _Exp
             _BagWidget
             _TargetInfo
             _TargetInfoBuffDebuff
             _TargetInfoCastBar
             _TargetInfoMainTarget
             _TargetCursor
             _TargetCursorGround
             _ScreenInfoFront
             _ScreenInfoBack
             _Notification
             _DTR
             _NaviMap
             _ActionBar
             _ActionBar03
             _ActionBar04
             _ActionBar05
             _ActionBar06
             _ActionBar07
             _ActionBar08
             _ActionBar09
             _ActionBarEx
             _ActionContents
             _AllianceList1
             _AllianceList2
             _EnemyList
             _ToDoList
             _ContentGauge
             _FocusTargetInfo
             _BattleTalk
             _LimitBreak
             ScenarioTree
             QuestRedoHud
             _PopUpText
             _FlyText
             _MiniTalk
             _AreaText (this is present twice?)
             _Image (this is present twice?)
             _LocationTitle
             _LocationTitleShort
             _ScreenText
             _WideText (this is present twice?)
             _PoisonText (this is present twice?)
             _TextError
             _Image3 (this is present twice?)
             _AreaText (this is present twice?)
             _Image (this is present twice?)
             _WideText (this is present twice?)
             _PoisonText (this is present twice?)
             _TextChain
             _TextClassChange
             _Image3 (this is present twice?)
             JobHudWHM
             JobHudWHM0
             CursorAddon
             OperationGuide
             */
        };

        public unsafe dynamic GetAddon(string name)
        {
            if (!AddonMap.ContainsKey(name) || !IsValid())
            {
                return null;
            }

            var ptr = GetAddonAddress(name);

            if (ptr != IntPtr.Zero)
            {
                return ManagedType<AtkStage>.GetDynamicManagedTypeFromIntPtr(ptr, memory, AddonMap[name]);
            }

            return null;
        }
    }
}
