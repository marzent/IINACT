namespace IINACT.Network;

internal class PacketMetadata(uint source, int dataSize, Span<byte> header)
{
    public uint Source { get; init; } = source;
    public int DataSize { get; init; } = dataSize;
    public byte[] Header { get; init; } = header.ToArray();
}
