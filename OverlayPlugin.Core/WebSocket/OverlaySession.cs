#nullable enable

using System.Net.Sockets;
using System.Text;
using NetCoreServer;
using RainbowMage.OverlayPlugin.WebSocket.Handlers;

namespace RainbowMage.OverlayPlugin.WebSocket;

internal class OverlaySession : WsSession
{
    private TinyIoCContainer Container { get; }
    private ILogger Logger { get; }
    private IHandler? Handler { get; set; }

    public OverlaySession(WsServer server, TinyIoCContainer container) : base(server)
    {
        Container = container;
        Logger = container.Resolve<ILogger>();
    }

    public override void OnWsConnected(HttpRequest request)
    {
        Logger.Log(LogLevel.Debug, $"Overlay WebSocket session with Id {Id} connected!");

        switch (request.Url)
        {
            case "/ws":
                Handler = new SocketHandler(Container, this);
                break;
            case "/MiniParse":
            case "/BeforeLogLineRead":
                Handler = new LegacyHandler(Container, this);
                break;
        }
        
        Handler?.OnOpen();
    }

    public override void OnWsDisconnected()
    {
        Logger.Log(LogLevel.Debug, $"Overlay WebSocket session with Id {Id} disconnected!");
        
        Handler?.OnClose();
    }

    public override void OnWsReceived(byte[] buffer, long offset, long size)
    {
        var message = Encoding.UTF8.GetString(buffer, (int)offset, (int)size);
        
        Logger.Log(LogLevel.Trace, $"Overlay WebSocket {Id} received message: {message}");
        
        Handler?.OnMessage(message);
    }

    protected override void OnError(SocketError error)
    {
        Handler?.OnError(error);
    }
}
    
