using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin;

public class IpcEventReceiver : IEventReceiver
{
    public string Name => "WSHandler";
    public event EventHandler<string> OnSendMessageOverIpc;

    public void HandleEvent(JObject e)
    {
        OnSendMessageOverIpc?.Invoke("", e.ToString(Formatting.None));
    }
}
