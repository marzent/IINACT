#pragma warning disable CS0169 // Field is never used
namespace IINACT.Network;

public unsafe struct PacketDispatcher
{
    private void* Unknown1;
    private void* Unknown2;
    private void* NetworkModuleProxy;

    public uint GameRandom;
    public uint LastPacketRandom;
    public uint Key0;
    public uint Key1;
    public uint Key2;
    public uint Unknown_32;

    public static PacketDispatcher* GetInstance()
    {
        var framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework.Instance();
        if (framework == null) return null;
        var nmp = framework->NetworkModuleProxy;
        if (nmp == null) return null;
        var rcb = nmp->ReceiverCallback;
        if (rcb == null) return null;
        return (PacketDispatcher*)&rcb->PacketDispatcher;
    }
    
    public static nint GetOnReceivePacketAddress()
    {
        var vtable = FFXIVClientStructs.FFXIV.Client.Network.PacketDispatcher.StaticVirtualTablePointer;
        return (nint)vtable->OnReceivePacket;
    }
}
