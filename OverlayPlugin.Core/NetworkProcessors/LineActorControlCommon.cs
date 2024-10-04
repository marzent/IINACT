namespace RainbowMage.OverlayPlugin.NetworkProcessors
{
    public enum Server_ActorControlCategory : ushort
    {
        VfxUnknown49 = 0x0031,       // 49
        SetAnimationState = 0x003E,  // 62
        SetModelState = 0x003F,      // 63
        PlayActionTimeline = 0x0197, // 407
        EObjAnimation = 0x19D,       // 413
        StatusUpdate = 0x01F8,       // 504
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
