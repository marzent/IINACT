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

            RegisterEventHandler("getSortedPartyList", (msg) =>
            {
                return GetSortedPartyList();
            });
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
            partyList.PartyType = Utilities.Utf8StringToString(addonPartyList.PartyTypeTextNode.NodeText, memory);
            partyList.PetCount = addonPartyList.PetCount;
            partyList.TrustCount = addonPartyList.TrustCount;
            if (partyList.MemberCount > 0)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember0, 0, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 1)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember1, 1, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 2)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember2, 2, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 3)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember3, 3, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 4)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember4, 4, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 5)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember5, 5, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 6)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember6, 6, SortedPartyList.Entry.EntryType.Party));
            }
            if (partyList.MemberCount > 7)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.PartyMember7, 7, SortedPartyList.Entry.EntryType.Party));
            }
            // Index of trust member intentionally increased by 1
            if (partyList.TrustCount > 0)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember0, 1, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 1)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember1, 2, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 2)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember2, 3, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 3)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember3, 4, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 4)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember4, 5, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 5)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember5, 6, SortedPartyList.Entry.EntryType.Trust));
            }
            if (partyList.TrustCount > 6)
            {
                partyList.Entries.Add(PartyMemberToEntry(addonPartyList.PartyMember.TrustMember6, 7, SortedPartyList.Entry.EntryType.Trust));
            }

            return JObject.FromObject(partyList);
        }

        private SortedPartyList.Entry PartyMemberToEntry(dynamic member, int index, SortedPartyList.Entry.EntryType type)
        {
            var dynamicType = ManagedType<int>.GetDynamicManagedTypeFromBaseType(member, memory, ((object)member).GetType());
            var name = dynamicType.Name;
            var nt = name.NodeText;
            string nameStr = Utilities.Utf8StringToString(nt, memory);
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

            return addon.ToJObject();
        }

        public override void Start()
        {
        }

        public override void LoadConfig(IPluginConfig cfg)
        {
            Config = container.Resolve<BuiltinEventConfig>();
        }

        public override void SaveConfig(IPluginConfig config)
        {
        }

        protected override void Update()
        {
        }
    }
}
