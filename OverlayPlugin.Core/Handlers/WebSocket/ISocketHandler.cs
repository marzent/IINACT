#nullable enable

using System;
using System.Net.Sockets;

namespace RainbowMage.OverlayPlugin.Handlers.WebSocket;

internal interface ISocketHandler : IDisposable
{
    public void OnMessage(string message);
    public void OnError(SocketError error);
}
    
