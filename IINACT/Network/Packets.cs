namespace IINACT.Network;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

internal struct PacketElementHeader
{
    public readonly uint Size;
    public uint SrcEntity;
    public uint DstEntity;
    public PacketType Type;
    public ushort Padding;
}

internal struct FrameHeader
{
    public unsafe fixed byte Prefix[16];
    
    public ulong TimeValue;
    public uint TotalSize;
    public PacketProtocol Protocol;
    public ushort Count;
    public byte Version;
    public CompressionType Compression;
    public ushort Unknown;
    public uint DecompressedLength;
}

#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

internal enum PacketType : ushort {
    None = 0x0,
    SessionInit = 0x1,
    Unknown2 = 0x2,
    Ipc = 0x3,
    Unknown4 = 0x4,
    Unknown5 = 0x5,
    Unknown6 = 0x6,
    KeepAlive = 0x7,
    KeepAliveResponse = 0x8,
    EncryptionInit = 0x9,
    UnknownA = 0xA,
    UnknownB = 0xB,
}

internal enum PacketProtocol : ushort {
    None = 0x0,
    Zone = 0x1,
    Chat = 0x2,
    Lobby = 0x3,
}

internal enum CompressionType : byte {
    None = 0x0,
    Zlib = 0x1,
    Oodle = 0x2,
}
