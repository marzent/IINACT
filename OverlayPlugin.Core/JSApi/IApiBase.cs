namespace RainbowMage.OverlayPlugin
{
    internal interface IApiBase : IEventReceiver
    {
        void OverlayMessage(string msg);

        void InitModernAPI();
    }
}
