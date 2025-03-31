using System.Runtime.CompilerServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;
using Reloaded.Hooks.Definitions.X64;

namespace IINACT.Network;

public unsafe class ZoneDownHookManager : IDisposable
{
	private const string GenericDownSignature = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";

	private const string OtherCreateTargetCaller =
		"48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B FA 48 8B F1 0F B7 12";
	private const string CreateTargetSignature = "3B 0D ?? ?? ?? ?? 74 0E";
	
	private delegate nuint DownPrototype(byte* data, byte* a2, nuint a3, nuint a4, nuint a5);
	
	private readonly Hook<DownPrototype> zoneDownHook;

	private delegate byte OtherCreateTargetCallerPrototype(void* a1, void* a2, void* a3);
	private readonly Hook<OtherCreateTargetCallerPrototype> otherCreateTargetCallerHook;

	[Function([FunctionAttribute.Register.rcx, FunctionAttribute.Register.rsi], FunctionAttribute.Register.rax, false)]
	private delegate byte CreateTargetPrototype(int entityId, nint packetPtr);
	private readonly Hook<CreateTargetPrototype> createTargetHook;
    
	private readonly SimpleBuffer buffer;

	private readonly Queue<PacketMetadata> zoneDownIpcQueue;
	private bool ignoreCreateTarget;

	public ZoneDownHookManager(
		ISigScanner sigScanner,
		IGameInteropProvider hooks)
	{
		buffer = new SimpleBuffer(1024 * 1024);
		zoneDownIpcQueue = new Queue<PacketMetadata>();

        var multiScanner = new MultiSigScanner();
		var rxPtrs = multiScanner.ScanText(GenericDownSignature, 3);
		zoneDownHook = hooks.HookFromAddress<DownPrototype>(rxPtrs[2], ZoneDownDetour);
		
		var createTargetPtr = sigScanner.ScanText(CreateTargetSignature);
		createTargetHook = hooks.HookFromAddress<CreateTargetPrototype>(createTargetPtr, CreateTargetDetour);
		
		var otherCreateTargetCallerPtr = sigScanner.ScanText(OtherCreateTargetCaller);
		otherCreateTargetCallerHook = hooks.
			HookFromAddress<OtherCreateTargetCallerPrototype>(otherCreateTargetCallerPtr, OtherCreateTargetCallerDetour);

		Enable();
	}

	public void Enable()
	{
		zoneDownHook?.Enable();
		
		createTargetHook?.Enable();
		otherCreateTargetCallerHook?.Enable();
	}
	
	public void Disable()
	{
		zoneDownHook?.Disable();
		
		createTargetHook?.Disable();
		otherCreateTargetCallerHook?.Disable();
		zoneDownIpcQueue.Clear();
	}
	
	public void Dispose()
	{
		Disable();
		zoneDownHook?.Dispose();
		
		createTargetHook?.Dispose();
		otherCreateTargetCallerHook?.Dispose();
	}

	// I know this is very silly, but I don't have access to the return address like Deucalion so uhh
	// let me know if you know of a way I can get the return address in CreateTargetDetour and I'll fix it!
	private byte OtherCreateTargetCallerDetour(void* a1, void* a2, void* a3)
	{
		ignoreCreateTarget = true;
		return otherCreateTargetCallerHook.Original(a1, a2, a3);
	}
	
	private byte CreateTargetDetour(int entityId, nint packetPtr)
	{
		if (ignoreCreateTarget)
		{
			ignoreCreateTarget = false;
			return createTargetHook.Original(entityId, packetPtr);
		}
		
		if (zoneDownIpcQueue.Count == 0)
		{
			Plugin.Log.Error($"[CreateTarget]: Please report this problem: no packets in queue");
			return createTargetHook.Original(entityId, packetPtr);
		}
		
		var meta = zoneDownIpcQueue.Dequeue();

		if (meta.Source != entityId)
		{
            Plugin.Log.Error($"[CreateTarget]: Please report this problem: srcId {entityId} | queuedSrcId {meta.Source}");
		}

		var data = new Span<byte>((byte*)packetPtr, meta.DataSize);
        buffer.Clear();
		buffer.Write(meta.Header);
		buffer.Write(data);
        EnqueueToMachina(buffer.GetBuffer());
		
		return createTargetHook.Original(entityId, packetPtr);
	}
    
    private nuint ZoneDownDetour(byte* data, byte* a2, nuint a3, nuint a4, nuint a5)
    {
	    var ret = zoneDownHook.Original(data, a2, a3, a4, a5);

	    var packetOffset = *(uint*)(data + 28);
	    if (packetOffset != 0) return ret;
	    
	    try
	    {
		    PacketsFromFrame((byte*) *(nint*)(data + 16));
	    }
	    catch (Exception e)
	    {
            Plugin.Log.Error(e, "[PacketsFromFrame] Error!");
	    }

        return ret;
    }
    
    private void PacketsFromFrame(byte* framePtr)
    {
        if ((nuint)framePtr == 0)
        {
            Plugin.Log.Error("null ptr");
            return;
        }
        
        var headerSize = Unsafe.SizeOf<FrameHeader>();
        var headerSpan = new Span<byte>(framePtr, headerSize);
        
        var header = headerSpan.Cast<byte, FrameHeader>();
        var span = new Span<byte>(framePtr, (int)header.TotalSize);
        
        var data = span.Slice(headerSize, (int)header.TotalSize - headerSize);
        
        // Compression
        if (header.Compression != CompressionType.None)
        {
            Plugin.Log.Error($"frame compressed: {header.Compression} payload is {header.TotalSize - 40} bytes, decomp'd is {header.DecompressedLength}");
            return;
        }
        
        GameServerTime.SetLastServerTimestamp(header.TimeValue);
        
        // Deobfuscation
        var offset = 0;
        for (var i = 0; i < header.Count; i++)
        {
	        var pktHdrSize = Unsafe.SizeOf<PacketElementHeader>();
            var pktHdrSlice = data.Slice(offset, pktHdrSize);
            var pktHdr = pktHdrSlice.Cast<byte, PacketElementHeader>();
            
            var needsDeobfuscation = pktHdr.Type is PacketType.Ipc;

            if (!needsDeobfuscation)
            {
                buffer.Clear();
                buffer.Write(pktHdrSlice);
            }
            
            var pktData = data.Slice(offset + pktHdrSize, (int)pktHdr.Size - pktHdrSize);

            if (needsDeobfuscation)
            {
	            var meta = new PacketMetadata(pktHdr.SrcEntity, pktData.Length, pktHdrSlice);
	            zoneDownIpcQueue.Enqueue(meta);
            }
            else
            {
                buffer.Write(pktData);
                EnqueueToMachina(buffer.GetBuffer());
            }
            
            offset += (int)pktHdr.Size;
        }
    }

    private static void EnqueueToMachina(ReadOnlySpan<byte> data)
    {
        var queue = Machina.FFXIV.Dalamud.DalamudClient.MessageQueue;
        queue?.Enqueue((GameServerTime.LastSeverTimestamp, data.ToArray()));
    }
}
