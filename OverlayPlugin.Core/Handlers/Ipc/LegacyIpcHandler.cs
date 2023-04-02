#nullable enable
using Dalamud.Plugin.Ipc;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers.Ipc;

internal class LegacyIpcHandler : LegacyHandler
{
    private ICallGateProvider<JObject, bool> Receiver { get; }
    private ICallGateSubscriber<JObject, bool> Sender { get; }

    public LegacyIpcHandler(
        string name, ICallGateProvider<JObject, bool> receiver, ICallGateSubscriber<JObject, bool> sender,
        ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository) : base(
        name, logger, eventDispatcher, repository)
    {
        Receiver = receiver;
        Sender = sender;
        
        Receiver.RegisterAction(DataReceived);
        
        Start();
    }

    protected override void Send(JObject e) => Sender.InvokeAction(e);
    
    public override void Dispose()
    {
        Receiver.UnregisterAction();
        base.Dispose();
    }
}

