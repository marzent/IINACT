namespace IINACT.Network;

internal class QueuedPacket
{
    internal uint Source { get; set; }
    internal int DataSize { get; set; }
    internal byte[]? Header { get; set; }
    internal byte[]? Data { get; set; }
    
    public QueuedPacket() {}

    internal QueuedPacket(uint source, int dataSize, byte[] header, byte[] data)
    {
        Source = source;
        DataSize = dataSize;
        Header = header;
        Data = data;
    }

    internal QueuedPacket(uint source, int dataSize, Span<byte> header, Span<byte> data)
    {
        Source = source;
        DataSize = dataSize;
        Header = header.ToArray();
        Data = data.ToArray();
    }

    internal void Clear()
    {
        Source = 0;
        DataSize = 0;
        Header = null;
        Data = null;
    }
}
