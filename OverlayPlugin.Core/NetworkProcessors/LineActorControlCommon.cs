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
        // Note that these names are used directly as strings in `LineFateControl`
        // Changing them will necessitate updating the logic in that class
        FateAdd = 0x0942, // 2370
        FateRemove = 0x0935, // 2357
        FateUpdate = 0x093C, // 2364
    }
}
