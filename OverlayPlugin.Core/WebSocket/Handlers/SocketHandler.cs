#nullable enable
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;


namespace RainbowMage.OverlayPlugin.WebSocket.Handlers;

internal class SocketHandler : IHandler, IEventReceiver
{
    public string Name => "WSHandler";
    private ILogger Logger { get; }
    private EventDispatcher Dispatcher { get; }
    private OverlaySession Session { get; }

    public SocketHandler(TinyIoCContainer container, OverlaySession session)
    {
        Logger = container.Resolve<ILogger>();
        Dispatcher = container.Resolve<EventDispatcher>();
        Session = session;
    }
        
    public void HandleEvent(JObject e)
    {
        Session.SendTextAsync(e.ToString(Formatting.None));
    }

    public void OnOpen()
    {
    }
    
    public void OnError(SocketError error)
    {
        Logger.Log(LogLevel.Error, Resources.WSMessageSendFailed, Enum.GetName(error)); 
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

        if (!data.ContainsKey("call")) return;

        var msgType = data["call"].ToString();
        switch (msgType)
        {
            case "subscribe":
                try
                {
                    foreach (var item in data["events"].ToList())
                    {
                        Dispatcher.Subscribe(item.ToString(), this);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, Resources.WSNewSubFail, ex);
                }

                return;
            case "unsubscribe":
                try
                {
                    foreach (var item in data["events"].ToList())
                    {
                        Dispatcher.Unsubscribe(item.ToString(), this);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, Resources.WSUnsubFail, ex);
                }

                return;
            default:
                Task.Run(() =>
                {
                    try
                    {
                        var response = Dispatcher.CallHandler(data);

                        if (response != null && response.Type != JTokenType.Object)
                        {
                            throw new Exception("Handler response must be an object or null");
                        }

                        if (response == null)
                        {
                            response = new JObject();
                            response["$isNull"] = true;
                        }

                        if (data.ContainsKey("rseq"))
                        {
                            response["rseq"] = data["rseq"];
                        }

                        Session.SendTextAsync(response.ToString(Formatting.None));
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, Resources.WSHandlerException, ex);
                    }
                });
                break;
        }
    }

    public void OnClose()
    {
        Dispatcher.UnsubscribeAll(this);
    }
}
