#nullable enable

using System;
using System.Net;
using System.Net.Sockets;
using NetCoreServer;

namespace RainbowMage.OverlayPlugin.WebSocket;

internal class OverlayServer : WsServer
{
    private TinyIoCContainer Container { get; }
    private ILogger Logger { get; }
    
    public OverlayServer(IPAddress address, int port, TinyIoCContainer container) : base(address, port)
    {
        Container = container;
        Logger = container.Resolve<ILogger>();
    }

    protected override TcpSession CreateSession()
    {
        return new OverlaySession(this, Container);
    }

    protected override void OnError(SocketError error)
    {
        Logger.Log(LogLevel.Error, $"Overlay WebSocket server caught an error with code {error}");
    }
}
    
