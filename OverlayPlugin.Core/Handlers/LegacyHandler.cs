#nullable enable
using Advanced_Combat_Tracker;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers;

internal abstract class LegacyHandler : IHandler, IEventReceiver
{
    public string Name { get; }
    protected ILogger Logger { get; }
    private EventDispatcher Dispatcher { get; }
    private FFXIVRepository Repository { get; }

    protected LegacyHandler(string name, ILogger logger, EventDispatcher eventDispatcher, FFXIVRepository repository)
    {
        Name = name;
        Logger = logger;
        Dispatcher = eventDispatcher;
        Repository = repository;
    }

    protected void Start()
    {
        Dispatcher.Subscribe("CombatData", this);
        Dispatcher.Subscribe("LogLine", this);
        Dispatcher.Subscribe("ChangeZone", this);
        Dispatcher.Subscribe("ChangePrimaryPlayer", this);
        
        Send((JObject)JToken.FromObject(new
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

    protected abstract void Send(JObject data);

    public virtual void Dispose() => Dispatcher.UnsubscribeAll(this);

    public void HandleEvent(JObject e)
    {
        var data = (JObject)JToken.FromObject(new {type = "broadcast"});
        
        switch ( e["type"]?.ToString())
        {
            case "CombatData":
                data["msgtype"] = "CombatData";
                data["msg"] = e;
                break;
            case "LogLine":
                data["msgtype"] = "Chat";
                data["msg"] = e["rawLine"];
                break;
            case "ChangeZone":
                data["msgtype"] = "ChangeZone";
                data["msg"] = e;
                break;
            case "ChangePrimaryPlayer":
                data["msgtype"] = "SendCharName";
                data["msg"] = e;
                break;
            default:
                return;
        }
        
        Send(data);
    }

    public void DataReceived(JObject data)
    {
        if (!data.ContainsKey("type") || !data.ContainsKey("msgtype")) return;

        switch (data["msgtype"]?.ToString())
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

