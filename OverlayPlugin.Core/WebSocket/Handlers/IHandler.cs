#nullable enable

using System.Net.Sockets;

namespace RainbowMage.OverlayPlugin.WebSocket.Handlers;

internal interface IHandler
{
    public void OnOpen();
    public void OnMessage(string message);
    public void OnClose();
    public void OnError(SocketError error);
}
    
