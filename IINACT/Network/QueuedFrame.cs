using Microsoft.Extensions.ObjectPool;

namespace IINACT.Network;

internal class QueuedFrame
{
    internal byte[]? Header { get; set; }
    internal List<QueuedPacket> Packets { get; init; }

    public QueuedFrame()
    {
        Packets = [];
    }

    internal QueuedFrame(byte[] header)
    {
        Header = header;
        Packets = [];
    }

    internal void Clear(ObjectPool<QueuedPacket> pool)
    {
        Header = null;
        foreach (var packet in Packets)
        {
            packet.Clear();
            pool.Return(packet);
        }
        Packets.Clear();
    }

    internal void Write(SimpleBuffer buffer)
    {
        buffer.Write(Header);
        foreach (var packet in Packets)
        {
            buffer.Write(packet.Header);
            buffer.Write(packet.Data);
        }
    }
}
