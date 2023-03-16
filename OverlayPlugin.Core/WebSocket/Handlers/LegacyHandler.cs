#nullable enable
using System.Net.Sockets;
using Advanced_Combat_Tracker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static Newtonsoft.Json.JsonConvert;


namespace RainbowMage.OverlayPlugin.WebSocket.Handlers;

internal class LegacyHandler : IHandler, IEventReceiver
{
    public string Name => "WSLegacyHandler";
    private ILogger Logger { get; }
    private EventDispatcher Dispatcher { get; }
    private FFXIVRepository Repository { get; }
    private OverlaySession Session { get; }

    public LegacyHandler(TinyIoCContainer container, OverlaySession session)
    {
        Logger = container.Resolve<ILogger>();
        Dispatcher = container.Resolve<EventDispatcher>();
        Repository = container.Resolve<FFXIVRepository>();
        Session = session;
    }

    public void OnOpen()
    {
        Dispatcher.Subscribe("CombatData", this);
        Dispatcher.Subscribe("LogLine", this);
        Dispatcher.Subscribe("ChangeZone", this);
        Dispatcher.Subscribe("ChangePrimaryPlayer", this);

        Session.SendTextAsync(SerializeObject(new
        {
            type = "broadcast",
            msgtype = "SendCharName",
            msg = new
            {
                charName = Repository.GetPlayerName() ?? "YOU",
                charID = Repository.GetPlayerID()
            }
        }));
    }

    public void OnClose()
    {
        Dispatcher.UnsubscribeAll(this);
    }
        
    public void HandleEvent(JObject e)
    {
        switch ( e["type"]?.ToString())
        {
            case "CombatData":
                Session.SendTextAsync("{\"type\":\"broadcast\",\"msgtype\":\"CombatData\",\"msg\":" +
                                  e.ToString(Formatting.None) + "}");
                return;
            case "LogLine":
                Session.SendTextAsync("{\"type\":\"broadcast\",\"msgtype\":\"Chat\",\"msg\":" +
                                  SerializeObject(e["rawLine"].ToString()) + "}");
                return;
            case "ChangeZone":
                Session.SendTextAsync("{\"type\":\"broadcast\",\"msgtype\":\"ChangeZone\",\"msg\":" +
                                  e.ToString(Formatting.None) + "}");
                return;
            case "ChangePrimaryPlayer":
                Session.SendTextAsync("{\"type\":\"broadcast\",\"msgtype\":\"SendCharName\",\"msg\":" +
                                  e.ToString(Formatting.None) + "}");
                return;
        }
    }

    public void OnError(SocketError error)
    {
        Logger.Log(LogLevel.Error, "Failed to send legacy WS message: {0}", error);
        Dispatcher.UnsubscribeAll(this);
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

        if (!data.ContainsKey("type") || !data.ContainsKey("msgtype")) return;

        switch (data["msgtype"].ToString())
        {
            case "Capture":
                Logger.Log(LogLevel.Warning, "ACTWS Capture is not supported outside of overlays.");
                break;
            case "RequestEnd":
                ActGlobals.oFormActMain.EndCombat(true);
                break;
        }
    }
}

