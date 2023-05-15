#nullable enable
using System.Net.Sockets;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.WebSocket;

namespace RainbowMage.OverlayPlugin.Handlers.WebSocket;

internal class LegacySocketHandler : LegacyHandler, ISocketHandler
{
    private OverlaySession Session { get; }

    public LegacySocketHandler(
        ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository, OverlaySession session) : base(
        "WSLegacyHandler", logger, eventDispatcher, repository)
    {
        Session = session;

        Start();
    }
    
    protected override void Send(JObject data) => Session.SendTextAsync(data.ToString(Formatting.None));
    

    public void OnError(SocketError error)
    {
        Logger.Log(LogLevel.Error, "Failed to send legacy WS message: {0}", error);
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

