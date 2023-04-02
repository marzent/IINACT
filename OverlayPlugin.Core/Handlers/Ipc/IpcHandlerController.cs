using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin.Handlers.Ipc;

public class IpcHandlerController : IDisposable
{
    public IpcHandlerController(TinyIoCContainer container)
    {
        Container = container;
        Logger = container.Resolve<ILogger>();
        Handlers = new ConcurrentDictionary<string, IHandler>();
        HandlerFactory = new HandlerFactory();
        LegacyHandlerFactory = new LegacyHandlerFactory();
    }
    
    private TinyIoCContainer Container { get; }
    private ILogger Logger { get; }
    private ConcurrentDictionary<string, IHandler> Handlers { get; }
    private IHandlerFactory HandlerFactory { get; }
    private IHandlerFactory LegacyHandlerFactory { get; }

    public bool CreateSubscriber(string name) => CreateSubscriber(name, HandlerFactory);
    public bool CreateLegacySubscriber(string name) => CreateSubscriber(name, LegacyHandlerFactory);

    private bool CreateSubscriber(string name, IHandlerFactory handlerFactory)
    {
        try
        {
            var handler = handlerFactory.Create(name, Container);
            if (Handlers.TryAdd(name, handler))
            {
                Logger.Log(LogLevel.Debug, $"Successfully added IPC handler {name}");
                return true;
            }
            Logger.Log(LogLevel.Error, $"Failed adding already existing IPC handler {name}");
            handler.Dispose();
            return false;
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Failed cresting IPC handler {name}: {ex}");
            return false;
        }
    }

    public bool Unsubscribe(string name)
    {
        if (!Handlers.Remove(name, out var handler))
        {
            Logger.Log(LogLevel.Warning, $"Cannot unsubscribe from non-existing IPC handler {name}");
            return false;
        }
        handler.Dispose();
        return true;
    }

    public void Dispose()
    {
        foreach (var (_, handler) in Handlers) handler.Dispose();
    }
}
