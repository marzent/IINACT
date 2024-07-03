using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.NetworkProcessors.PacketHelper
{
    // This is a copy/paste from Machina, and hasn't changed basically ever.
    // We don't pull this reference directly from Machina to avoid linking and runtime DLL loading issues due to version differences.
    // While this has never changed in the past, we shouldn't assume it won't change in the future, so allow for regional differences
    // if it changes at some point.
    [StructLayout(LayoutKind.Explicit)]
    public struct Server_MessageHeader
    {
        [FieldOffset(0)]
        public uint MessageLength;
        [FieldOffset(4)]
        public uint ActorID;
        [FieldOffset(8)]
        public uint LoginUserID;
        [FieldOffset(12)]
        public uint Unknown1;
        [FieldOffset(16)]
        public ushort Unknown2;
        [FieldOffset(18)]
        public ushort MessageType;
        [FieldOffset(20)]
        public uint Unknown3;
        [FieldOffset(24)]
        public uint Seconds;
        [FieldOffset(28)]
        public uint Unknown4;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_MessageHeader_Global : IHeaderStruct
    {
        public Server_MessageHeader header;

        public uint ActorID => header.ActorID;

        public uint Opcode => header.MessageType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_MessageHeader_CN : IHeaderStruct
    {
        public Server_MessageHeader header;

        public uint ActorID => header.ActorID;

        public uint Opcode => header.MessageType;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct Server_MessageHeader_KR : IHeaderStruct
    {
        public Server_MessageHeader header;

        public uint ActorID => header.ActorID;

        public uint Opcode => header.MessageType;
    }

}
