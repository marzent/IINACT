#nullable enable
using System;
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers.Ipc;

internal class IpcHandler : Handler
{
    private ICallGateProvider<JObject, bool> Receiver { get; }
    private ICallGateSubscriber<JObject, bool> Sender { get; }

    public IpcHandler(string name, ICallGateProvider<JObject, bool> receiver, ICallGateSubscriber<JObject, bool> sender,
                      ILogger logger, EventDispatcher eventDispatcher) : base(name, logger, eventDispatcher)
    {
        Receiver = receiver;
        Sender = sender;
        
        Receiver.RegisterAction(DataReceived);
    }

    protected override void Send(JObject e) => Sender.InvokeAction(e);
    
    public override void Dispose()
    {
        Receiver.UnregisterAction();
        base.Dispose();
    }
}
