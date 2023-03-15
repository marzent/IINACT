using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin
{
    internal interface IEventReceiver
    {
        string Name { get; }

        void HandleEvent(JObject e);
    }
}
