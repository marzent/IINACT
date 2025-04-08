using System.Diagnostics;
using System.Runtime.CompilerServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Microsoft.Extensions.ObjectPool;
using Reloaded.Hooks.Definitions.X64;

namespace IINACT.Network;

public unsafe class ZoneDownHookManager : IDisposable
{
	private const string GenericDownSignature = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";

	private const string OtherCreateTargetCaller =
		"48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 50 48 8B FA 48 8B F1 0F B7 12";
	private const string CreateTargetSignature = "3B 0D ?? ?? ?? ?? 74 0E";
    
    private readonly INotificationManager notificationManager;
	private delegate nuint DownPrototype(byte* data, byte* a2, nuint a3, nuint a4, nuint a5);
	
	private readonly Hook<DownPrototype> zoneDownHook;

	private delegate byte OtherCreateTargetCallerPrototype(void* a1, void* a2, void* a3);
	private readonly Hook<OtherCreateTargetCallerPrototype> otherCreateTargetCallerHook;

	[Function([FunctionAttribute.Register.rcx, FunctionAttribute.Register.rsi], FunctionAttribute.Register.rax, false)]
	private delegate byte CreateTargetPrototype(int entityId, nint packetPtr);
	private readonly Hook<CreateTargetPrototype> createTargetHook;
    
	private readonly SimpleBuffer buffer;
    
	private bool ignoreCreateTarget;
    
    /// <summary>
    /// Queue for frames observed prior to receiving all data from a previous Zone Rx frame with IPC packets.
    /// </summary>
    private readonly Queue<QueuedFrame> frameQueue;
    
    /// <summary>
    /// Queue for Zone Rx IPC packets that have yet to have their data filled in via CreateTarget.
    /// </summary>
    private readonly Queue<QueuedPacket> zoneDownIpcQueue;
    
    /// <summary>
    /// A pool of QueuedFrames to reduce GC pressure.
    /// </summary>
    private readonly ObjectPool<QueuedFrame> framePool;
    
    /// <summary>
    /// A pool of QueuedPackets to reduce GC pressure.
    /// </summary>
    private readonly ObjectPool<QueuedPacket> packetPool;
    
    private readonly DeucalionController deucalionController;

	public ZoneDownHookManager(
        INotificationManager notificationManager,
		ISigScanner sigScanner,
		IGameInteropProvider hooks)
    {
        this.notificationManager = notificationManager;
        deucalionController = new DeucalionController(Process.GetCurrentProcess(), hooks, notificationManager);
		buffer = new SimpleBuffer(1024 * 1024);
        frameQueue = new Queue<QueuedFrame>();
		zoneDownIpcQueue = new Queue<QueuedPacket>();
        
        framePool = new DefaultObjectPool<QueuedFrame>(new DefaultPooledObjectPolicy<QueuedFrame>(), 200);
        packetPool = new DefaultObjectPool<QueuedPacket>(new DefaultPooledObjectPolicy<QueuedPacket>(), 1000);

        var multiScanner = new MultiSigScanner();
		var rxPtrs = multiScanner.ScanText(GenericDownSignature, 3);
		zoneDownHook = hooks.HookFromAddress<DownPrototype>(rxPtrs[2], ZoneDownDetour);
		
		var createTargetPtr = sigScanner.ScanText(CreateTargetSignature);
		createTargetHook = hooks.HookFromAddress<CreateTargetPrototype>(createTargetPtr, CreateTargetDetour);
		
		var otherCreateTargetCallerPtr = sigScanner.ScanText(OtherCreateTargetCaller);
		otherCreateTargetCallerHook = hooks.
			HookFromAddress<OtherCreateTargetCallerPrototype>(otherCreateTargetCallerPtr, OtherCreateTargetCallerDetour);

		Enable();
        deucalionController.AllowLoads();
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
        deucalionController.Dispose();
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
        Plugin.Log.Verbose($"[OtherCreateTargetCaller]: ignoring next CreateTarget");
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
		
        var queuedPacket = zoneDownIpcQueue.Peek();
        Plugin.Log.Verbose($"[CreateTarget]: entity {entityId} meta source {queuedPacket.Source} size {queuedPacket.DataSize}");

		if (queuedPacket.Source != entityId)
		{
            Plugin.Log.Error($"[CreateTarget]: Please report this problem: srcId {entityId} | queuedSrcId {queuedPacket.Source}");
            // Try to see if the problem can be rectified...
            if (zoneDownIpcQueue.ToArray().Any(packet => packet.Source == entityId))
            {
                while (zoneDownIpcQueue.Count > 0)
                {
                    queuedPacket = zoneDownIpcQueue.Dequeue();
                    if (queuedPacket.Source == entityId)
                        break;
                }
            }
            else
            {
                return createTargetHook.Original(entityId, packetPtr);
            }
        }
        else
        {
            zoneDownIpcQueue.Dequeue();
        }
        
        // Set the packet's data
        var data = new Span<byte>((byte*)packetPtr, queuedPacket.DataSize);
        queuedPacket.Data = data.ToArray();

        // We dequeued the final packet in the zone rx ipc queue, we can commit all data now
        if (zoneDownIpcQueue.Count == 0)
        {
            while (frameQueue.TryDequeue(out var frame))
            {
                if (frame.Packets.All(p => p.Data != null))
                {
                    WriteFrameAndReturn(frame);
                }
                else
                {
                    // Show an error if there are any frames in which a packet does not have data
                    // This is significant because the zone rx IPC queue is empty - everything should have data
                    SendNotification("ZoneDown IPC Queue is empty, but not all packets have data.");
                }
            }
        }
        
		return createTargetHook.Original(entityId, packetPtr);
	}
    
    private void SendNotification(string content)
    {
        notificationManager.AddNotification(new Notification
        {
            Content = content,
            Title = "IINACT", 
        });
        Plugin.Log.Debug($"[SendNotification] {content}");
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

        var queuedFrame = GetFrameFromPool();
        queuedFrame.Header = headerSpan.ToArray();

        Plugin.Log.Verbose($"[{(nuint)framePtr:X}] [ZoneDown] proto {header.Protocol} unk {header.Unknown}, {header.Count} pkts size {header.TotalSize} usize {header.DecompressedLength}");
        
        // Compression
        if (header.Compression != CompressionType.None)
        {
            SendNotification($"A frame was compressed.");
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
            var pktData = data.Slice(offset + pktHdrSize, (int)pktHdr.Size - pktHdrSize);
            
            var queuedPacket = GetPacketFromPool();
            queuedFrame.Packets.Add(queuedPacket);
            queuedPacket.Source = pktHdr.SrcEntity;
            queuedPacket.DataSize = pktData.Length;
            queuedPacket.Header = pktHdrSlice.ToArray();

            if (pktHdr.Type == PacketType.Ipc)
            {
	            zoneDownIpcQueue.Enqueue(queuedPacket);
            }
            else
            {
                queuedPacket.Data = pktData.ToArray();
            }
            
            offset += (int)pktHdr.Size;
        }
        
        if (zoneDownIpcQueue.Count == 0)
        {
            WriteFrameAndReturn(queuedFrame);
        }
        else
        {
            Plugin.Log.Verbose($"[PacketsFromFrame] queueing ZoneDown frame with {queuedFrame.Packets.Count(p => p.Data != null)}/{queuedFrame.Packets.Count}");
            frameQueue.Enqueue(queuedFrame);
        }
    }
    
    private void WriteFrameAndReturn(QueuedFrame frame)
    {
        Plugin.Log.Verbose($"[WriteFrameAndReturn] Writing ZoneDown frame with {frame.Packets.Count(p => p.Data != null)}/{frame.Packets.Count}");
	    
        foreach (var queuedPacket in frame.Packets)
        {
            buffer.Clear();
            buffer.Write(queuedPacket.Header);
            buffer.Write(queuedPacket.Data);
            EnqueueToMachina(buffer.GetBuffer());
        }
        framePool.Return(frame);
    }

    private static void EnqueueToMachina(ReadOnlySpan<byte> data)
    {
        var queue = Machina.FFXIV.Dalamud.DalamudClient.MessageQueue;
        queue?.Enqueue((GameServerTime.LastSeverTimestamp, data.ToArray()));
    }
    
    private QueuedFrame GetFrameFromPool()
    {
        var frame = framePool.Get();
        frame.Clear(packetPool);
        return frame;
    }

    private QueuedPacket GetPacketFromPool()
    {
        var packet = packetPool.Get();
        packet.Clear();
        return packet;
    }
}
