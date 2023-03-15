using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.MemoryProcessors;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkGui.FFXIVClientStructs;
using RainbowMage.OverlayPlugin.MemoryProcessors.AtkStage;

namespace RainbowMage.OverlayPlugin.EventSources
{
    using Utilities = MemoryProcessors.AtkStage.FFXIVClientStructs.Utilities;

    public class SortedPartyList
    {
        public string PartyType;
        public int MemberCount;
        public int TrustCount;
        public byte PetCount;
        public byte ChocoboCount;
        public List<Entry> Entries = new List<Entry>();

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
        private IAtkStageMemory atkStageMemory;
        private FFXIVMemory memory;

        public BuiltinEventConfig Config { get; set; }

        public FFXIVClientStructsEventSource(TinyIoCContainer container) : base(container)
        {
            atkStageMemory = container.Resolve<IAtkStageMemory>();
            memory = container.Resolve<FFXIVMemory>();

            RegisterEventHandler("getFFXIVCSAddonSlow", (msg) =>
            {
                var key = msg["name"]?.ToString();
                if (key == null)
                    return null;
                return GetAddon(key);
            });

            RegisterEventHandler("getSortedPartyList", (msg) => { return GetSortedPartyList(); });
        }

        private JObject GetSortedPartyList()
        {
            var partyList = new SortedPartyList();

            if (!atkStageMemory.IsValid())
            {
                return null;
            }

            dynamic addonPartyList = atkStageMemory.GetAddon("_PartyList");

            partyList.ChocoboCount = addonPartyList.ChocoboCount;
            partyList.MemberCount = addonPartyList.MemberCount;
            partyList.PartyType = addonPartyList.PartyTypeTextNode.NodeText;
            partyList.PetCount = addonPartyList.PetCount;
            partyList.TrustCount = addonPartyList.TrustCount;

            for (int i = 0; i < partyList.MemberCount; ++i)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember, i,
                                                         SortedPartyList.Entry.EntryType.Party));
            }

            for (int i = 0; i < partyList.TrustCount; ++i)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember, i,
                                                         SortedPartyList.Entry.EntryType.Party));
            }

            return JObject.FromObject(partyList);
        }

        private SortedPartyList.Entry PartyMemberToEntry(
            dynamic partyMemberListStruct, int index, SortedPartyList.Entry.EntryType type)
        {
            string key = type.ToString() + "Member" + index;
            var member = partyMemberListStruct[key];

            var name = member.Name;
            var nameStr = name.NodeText;
            if (nameStr == null)
            {
                nameStr = "";
            }

            // Trim the utf8 chars at the start of the string by splitting on first space
            // Example raw string:
            // " Player Name"
            // Example hex bytes:
            // "E06A" "E069" "E060" "20" etc
            // This translates to the following text in the special FFXIV UTF font
            // "Lv" "9" "0" " " etc
            var parts = nameStr.Split(new char[] { ' ' }, 2);
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

            dynamic addon = atkStageMemory.GetAddon(key);

            if (addon == null)
            {
                return null;
            }

            var settings = new Newtonsoft.Json.JsonSerializerSettings();
            settings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore;
            var serializer = Newtonsoft.Json.JsonSerializer.CreateDefault(settings);

            var jobj = JObject.FromObject(addon, serializer);

            return jobj;
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
