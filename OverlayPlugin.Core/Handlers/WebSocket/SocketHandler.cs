#nullable enable
using System;
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.WebSocket;

namespace RainbowMage.OverlayPlugin.Handlers.WebSocket;

internal class SocketHandler : Handler, ISocketHandler
{
    private OverlaySession Session { get; }

    public SocketHandler(
        ILogger logger, EventDispatcher eventDispatcher, OverlaySession session) : base(
        "WSHandler", logger, eventDispatcher)
    {
        Session = session;
    }
        
    protected override void Send(JObject e) => Session.SendTextAsync(e.ToString(Formatting.None));

    public void OnError(SocketError error)
    {
        Logger.Log(LogLevel.Error, Resources.WSMessageSendFailed, Enum.GetName(error)); 
        Dispose();
    }

    public void OnMessage(string message)
    {
        JObject data;

        try
        {
            data = JObject.Parse(message);
        }
        catch (JsonException ex)
        {
            Logger.Log(LogLevel.Error, Resources.WSInvalidDataRecv, ex, message);
            return;
        }
        
        DataReceived(data);
    }
}
