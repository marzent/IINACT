#nullable enable

using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using RainbowMage.OverlayPlugin.Handlers.WebSocket;

namespace RainbowMage.OverlayPlugin.WebSocket;

internal class OverlaySession : WsSession
{
    private EventDispatcher Dispatcher { get; }
    private FFXIVRepository Repository { get; }
    private ILogger Logger { get; }
    private ISocketHandler? Handler { get; set; }

    public OverlaySession(WsServer server, TinyIoCContainer container) : base(server)
    {
        Dispatcher = container.Resolve<EventDispatcher>();
        Repository = container.Resolve<FFXIVRepository>();
        Logger = container.Resolve<ILogger>();
    }

    public override void OnWsConnected(HttpRequest request)
    {
        Logger.Log(LogLevel.Debug, $"Overlay WebSocket session with Id {Id} connected!");

        switch (request.Url)
        {
            case "/ws":
                Handler = new SocketHandler(Logger, Dispatcher, this);
                break;
            case "/MiniParse":
            case "/BeforeLogLineRead":
                Handler = new LegacySocketHandler(Logger, Dispatcher, Repository, this);
                break;
        }
    }

    public override void OnWsDisconnected()
    {
        Logger.Log(LogLevel.Debug, $"Overlay WebSocket session with Id {Id} disconnected!");
        Handler?.Dispose();
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        var message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
#if DEBUG
        Logger.Log(LogLevel.Trace, $"Overlay WebSocket {Id} received message: {message}");
#endif
        Handler?.OnMessage(message);
    }

    protected override void OnError(SocketError error)
    {
        Handler?.OnError(error);
    }
}
    
