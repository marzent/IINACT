using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Newtonsoft.Json.Serialization;

namespace RainbowMage.OverlayPlugin.EventSources
{
    public class SortedPartyList
    {
        public string PartyType;
        public int MemberCount;
        public int TrustCount;
        public byte PetCount;
        public byte ChocoboCount;
        public List<Entry> Entries = new();

        public struct Entry
        {
            public int Index;
            public string Name;
            public EntryType Type;

            public enum EntryType
            {
                Party = 0,
                Trust = 1,
            }
        }
    }

    public class FFXIVClientStructsEventSource : EventSourceBase
    {
        private readonly IAtkStageMemory atkStageMemory;

        public BuiltinEventConfig Config { get; set; }

        public FFXIVClientStructsEventSource(TinyIoCContainer container) : base(container)
        {
            atkStageMemory = container.Resolve<IAtkStageMemory>();
            
            RegisterEventHandler("getFFXIVCSAddonSlow", (msg) =>
            {
                var key = msg["name"]?.ToString();
                return key == null ? null : GetAddon(key);
            });

            RegisterEventHandler("getSortedPartyList", (_) => GetSortedPartyList());
        }

        private unsafe JObject GetSortedPartyList()
        {
            var partyList = new SortedPartyList();

            if (!atkStageMemory.IsValid()) return null;

            var addonPartyListCandidate = atkStageMemory.GetAddon<AddonPartyList>();
            if (!addonPartyListCandidate.HasValue) return null;
            var addonPartyList = addonPartyListCandidate.Value;


            partyList.ChocoboCount = addonPartyList.ChocoboCount;
            partyList.MemberCount = addonPartyList.MemberCount;
            partyList.PartyType = addonPartyList.PartyTypeTextNode->NodeText.ToString();
            partyList.PetCount = addonPartyList.PetCount;
            partyList.TrustCount = addonPartyList.TrustCount;

            for (var i = 0; i < partyList.MemberCount; ++i)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMembers, i,
                                                         SortedPartyList.Entry.EntryType.Party));
            }

            for (var i = 0; i < partyList.TrustCount; ++i)
            {
                partyList.Entries.Add(TrustMemberToEntry(addonPartyList.TrustMembers, i,
                                                         SortedPartyList.Entry.EntryType.Trust));
            }

            return JObject.FromObject(partyList);
        }

        private static unsafe SortedPartyList.Entry PartyMemberToEntry(
            Span<AddonPartyList.PartyListMemberStruct> partyMembers, int index, SortedPartyList.Entry.EntryType type)
        {
            var member = partyMembers[index];
            return TextNodeToEntry(member.Name, index, type);
        }
        
        private static unsafe SortedPartyList.Entry TrustMemberToEntry(
            Span<AddonPartyList.PartyListMemberStruct> trustMembers, int index, SortedPartyList.Entry.EntryType type)
        {
            var member = trustMembers[index];
            return TextNodeToEntry(member.Name, index, type);
        }
        
        private static unsafe SortedPartyList.Entry TextNodeToEntry(
            AtkTextNode* textNode, int index, SortedPartyList.Entry.EntryType type)
        {
            var nameStr = textNode == null ? "" : textNode->NodeText.ToString();

            // Trim the utf8 chars at the start of the string by splitting on first space
            // Example raw string:
            // " Player Name"
            // Example hex bytes:
            // "E06A" "E069" "E060" "20" etc
            // This translates to the following text in the special FFXIV UTF font
            // "Lv" "9" "0" " " etc
            var parts = nameStr.Split(new[] { ' ' }, 2);
            if (parts.Length > 1)
            {
                nameStr = parts[1];
            }

            return new SortedPartyList.Entry
            {
                Type = type,
                Index = index,
                Name = nameStr,
            };
        }

        private JObject GetAddon(string key)
        {
            if (!atkStageMemory.IsValid())
            {
                return null;
            }

            var addon = atkStageMemory.GetAddon(key);

            if (addon == null)
            {
                return null;
            }

            static void HandleDeserializationError(object sender, ErrorEventArgs errorArgs) => 
                errorArgs.ErrorContext.Handled = true;

            var settings = new Newtonsoft.Json.JsonSerializerSettings
            {
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                Error = HandleDeserializationError
            };
            var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault(settings);

            var jObject = JObject.FromObject(addon, serializer);

            return jObject;
        }

        public override void Start() { }

        public override void LoadConfig(IPluginConfig cfg)
        {
            Config = container.Resolve<BuiltinEventConfig>();
        }

        public override void SaveConfig(IPluginConfig config) { }

        protected override void Update() { }
    }
}
