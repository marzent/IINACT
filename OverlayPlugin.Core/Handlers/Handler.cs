#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers;

internal abstract class Handler : IHandler, IEventReceiver
{
    public string Name { get; }
    protected ILogger Logger { get; }
    private EventDispatcher Dispatcher { get; }

    protected Handler(string name, ILogger logger, EventDispatcher eventDispatcher)
    {
        Name = name;
        Logger = logger;
        Dispatcher = eventDispatcher;
    }

    protected abstract void Send(JObject data);
    public void HandleEvent(JObject e) => Send(e);

    public void DataReceived(JObject data)
    {
        if (!data.ContainsKey("call")) return;

        var msgType = data["call"]?.ToString();
        switch (msgType)
        {
            case "subscribe":
                try
                {
                    foreach (var item in data["events"]?.ToList() ?? new List<JToken>())
                        Dispatcher.Subscribe(item.ToString(), this);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogLevel.Error, Resources.WSNewSubFail, ex);
                }

                return;
            case "unsubscribe":
                try
                {
                    foreach (var item in data["events"]?.ToList() ?? new List<JToken>())
                        Dispatcher.Unsubscribe(item.ToString(), this);
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
                            throw new Exception("Handler response must be an object or null");

                        if (response == null)
                        {
                            response = new JObject();
                            response["$isNull"] = true;
                        }

                        if (data.ContainsKey("rseq")) response["rseq"] = data["rseq"];

                        var jObject = response.ToObject<JObject>()!;
                        
                        Send(jObject);
                    }
                    catch (Exception ex)
                    {
                        Logger.Log(LogLevel.Error, Resources.WSHandlerException, ex);
                    }
                });
                break;
        }
    }

    public virtual void Dispose() => Dispatcher.UnsubscribeAll(this);
}
