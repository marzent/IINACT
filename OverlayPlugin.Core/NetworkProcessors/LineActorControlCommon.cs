namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public enum Server_ActorControlCategory : ushort
    {
        SetAnimationState = 0x003E, // 62
        StatusUpdate = 0x01F8, // 504
        // Name is a guess
        DisplayLogMessage = 0x020F, // 527
        DisplayLogMessageParams = 0x0210, // 528
        DisplayPublicContentTextMessage = 0x0834, // 2100
    }
}
