using System.Runtime.CompilerServices;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;
using Unscrambler;
using Unscrambler.Constants;
using Unscrambler.Derivation;
using Unscrambler.Unscramble;

namespace IINACT.Network;

public unsafe class ZoneDownHookManager : IDisposable
{
	private const string GenericDownSignature = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";
    
    private readonly INotificationManager notificationManager;
	private delegate nuint DownPrototype(byte* data, byte* a2, nuint a3, nuint a4, nuint a5);
	
	private readonly Hook<DownPrototype> zoneDownHook;
    
	private readonly SimpleBuffer buffer;
    
    private bool obfuscationOverride;
    private readonly VersionConstants versionConstants;
    private readonly IKeyGenerator keyGenerator;
    private readonly IUnscrambler unscrambler;

	public ZoneDownHookManager(
        INotificationManager notificationManager,
		ISigScanner sigScanner,
		IGameInteropProvider hooks)
    {
        this.notificationManager = notificationManager;
		buffer = new SimpleBuffer(1024 * 1024);
        
        var version = GetRunningGameVersion();
        versionConstants = VersionConstants.ForGameVersion(version);
        keyGenerator = KeyGeneratorFactory.ForGameVersion(version);
        unscrambler = UnscramblerFactory.ForGameVersion(version);

        var multiScanner = new MultiSigScanner();
		var rxPtrs = multiScanner.ScanText(GenericDownSignature, 3);
		zoneDownHook = hooks.HookFromAddress<DownPrototype>(rxPtrs[2], ZoneDownDetour);

		Enable();
    }

	public void Enable()
	{
        var dispatcher = PacketDispatcher.GetInstance();
        if (dispatcher != null)
        {
            var gameRandom = dispatcher->GameRandom;
            var packetRandom = dispatcher->LastPacketRandom;
			
            obfuscationOverride = dispatcher->Key0 >= gameRandom + packetRandom;

            if (obfuscationOverride)
            {
                keyGenerator.Keys[0] = (byte)(dispatcher->Key0 - gameRandom - packetRandom);
                keyGenerator.Keys[1] = (byte)(dispatcher->Key1 - gameRandom - packetRandom);
                keyGenerator.Keys[2] = (byte)(dispatcher->Key2 - gameRandom - packetRandom);	
            }
            else
            {
                keyGenerator.Keys[0] = 0;
                keyGenerator.Keys[1] = 0;
                keyGenerator.Keys[2] = 0;
            }
			
            Plugin.Log.Debug($"[Enable] obfuscation override is {obfuscationOverride}");
            Plugin.Log.Debug($"[Enable] keys {dispatcher->Key0}, {dispatcher->Key1}, {dispatcher->Key2}");
            Plugin.Log.Debug($"[Enable] game random {dispatcher->GameRandom}, packet random {dispatcher->LastPacketRandom}");
        }
        else
        {
            Plugin.Log.Warning("[Enable] Dispatcher was null, so not initializing keys");
        }
        
		zoneDownHook?.Enable();
	}
	
	public void Disable()
	{
		zoneDownHook?.Disable();
	}
	
	public void Dispose()
	{
		Disable();
		zoneDownHook?.Dispose();
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
            var pktOpcode = OpcodeUtility.GetOpcodeFromPacketAtIpcStart(pktData);
            var canInitDeobfuscation = pktOpcode == versionConstants.InitZoneOpcode ||
                                       pktOpcode == versionConstants.UnknownObfuscationInitOpcode;
            var needsDeobfuscation = versionConstants.ObfuscatedOpcodes.ContainsValue(pktOpcode);
            
            buffer.Clear();
            buffer.Write(pktHdrSlice);
            
            if (canInitDeobfuscation)
            {
                if (pktOpcode == versionConstants.InitZoneOpcode)
                    keyGenerator.GenerateFromInitZone(pktData);
                else if (pktOpcode == versionConstants.UnknownObfuscationInitOpcode)
                    keyGenerator.GenerateFromUnknownInitializer(pktData);
                
                obfuscationOverride = false;
            }

            if (needsDeobfuscation && (keyGenerator.ObfuscationEnabled || obfuscationOverride))
            {
                var pos = buffer.Size;
                buffer.Write(pktData);
                var slice = buffer.Get(pos, pktData.Length);
                var opcodeBasedKey = keyGenerator.GetOpcodeBasedKey(pktOpcode);
                unscrambler.Unscramble(slice, keyGenerator.Keys[0], keyGenerator.Keys[1], keyGenerator.Keys[2], opcodeBasedKey);
            }
            else
            {
                buffer.Write(pktData);    
            }
            
            EnqueueToMachina(buffer.GetBuffer());
            
            offset += (int)pktHdr.Size;
        }
    }

    private static void EnqueueToMachina(ReadOnlySpan<byte> data)
    {
        var queue = Machina.FFXIV.Dalamud.DalamudClient.MessageQueue;
        queue?.Enqueue((GameServerTime.LastSeverTimestamp, data.ToArray()));
    }
    
    private static string GetRunningGameVersion()
    {
        var path = Environment.ProcessPath!;
        var parent = Directory.GetParent(path)!.FullName;
        var ffxivVerFile = Path.Combine(parent, "ffxivgame.ver");
        return File.Exists(ffxivVerFile) ? File.ReadAllText(ffxivVerFile) : "0000.00.00.0000.0000";
    }
}
