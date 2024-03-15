using System;

namespace RainbowMage.OverlayPlugin.MemoryProcessors.ContentFinderSettings
{
    interface IContentFinderSettingsMemory651 : IContentFinderSettingsMemory { }

    class ContentFinderSettingsMemory651 : ContentFinderSettingsMemory, IContentFinderSettingsMemory651
    {
        // FUN_140985ee0:140985fae
        private const string settingsSignature = "0FB63D????????EB??488B0D????????BA????????4883C1??E8????????8378????410F97C6";

        // FUN_1400aa840:1400aa862
        // IsLocalPlayerInParty:1400aa862 (after rename)
        private const string inContentFinderSignature = "803D??????????0F85????????33D2488D0D????????E8????????80B8??????????0F87";

        // Client::Game::InstanceContent::PublicContentDirector.HandleEnterContentInfoPacket, call to FUN_140988430 with static address 0x14220de08
        // Static address = settingsSignature resolved address - 0x18, probably a full struct that hasn't been deconstructed yet
        // FUN_140988430 returns address + 0x20 = 0x14220de28, so settingsSignature + 0x8
        // HandleEnterContentInfoPacket calls to FUN_140987600 with resolved static address as first param
        // param_9 = unrestricted party flag
        // param_10 = min item level flag
        // param_11 = silence echo flag
        // param_12 = explorer mode flag
        // param_13 = level sync flag
        // FUN_140987600 directly sets memory based on static address passed in, the five flags we're interested in are at address + 0x88
        // So 0x8 + 0x88 = 0x90
        // There didn't seem to be a good signature to find the base address (passed into FUN_140988430) that would resolve across both
        // 6.51 and 6.51-hotfix versions, so we're using the sig for the duty finder settings holder instead.
        private const int inContentSettingsOffset = 0x90;

        public ContentFinderSettingsMemory651(TinyIoCContainer container)
            : base(container, settingsSignature, inContentFinderSignature, inContentSettingsOffset)
        { }

        public override Version GetVersion()
        {
            return new Version(6, 5, 1);
        }
    }
}
