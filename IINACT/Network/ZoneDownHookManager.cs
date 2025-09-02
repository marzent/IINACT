using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Unscrambler;
using Unscrambler.Constants;
using Unscrambler.Derivation;
using Unscrambler.Derivation.Versions;
using Unscrambler.Unscramble;
using Unscrambler.Unscramble.Versions;

namespace IINACT.Network;

public unsafe class ZoneDownHookManager : IDisposable
{
	private const string GenericDownSignature = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";
    private const string OpcodeKeyTableSignature = "6B ?? ?? ?? ?? ?? 8B ?? 8A ?? ?? ?? ?? 41 81";
    
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
		IGameInteropProvider hooks,
        IDalamudPluginInterface pluginInterface)
    {
        this.notificationManager = notificationManager;
		buffer = new SimpleBuffer(1024 * 1024);
        var multiScanner = new MultiSigScanner();
        
        var version = GetRunningGameVersion();
        if (VersionConstants.Constants.ContainsKey(version))
        {
            versionConstants = VersionConstants.ForGameVersion(version);
            keyGenerator = KeyGeneratorFactory.ForGameVersion(version);
            unscrambler = UnscramblerFactory.ForGameVersion(version);
        }
        else
        {
            Plugin.Log.Warning("[ZoneDownHookManager] Creating fallback Unscrambler constants dynamically");
            var opcodeKeyTableIns =
                MultiSigScanner.Scan(PacketDispatcher.GetOnReceivePacketAddress(), 0x1000, OpcodeKeyTableSignature);
            var bytes = new byte[13];
            Marshal.Copy(opcodeKeyTableIns, bytes, 0, 13);
            var opcodeKeyTableOffset = BitConverter.ToUInt32(bytes, 9);
            var opcodeKeyTableSize = bytes[2] * 4;
            Plugin.Log.Debug(
                $"[ZoneDownHookManager] opcodeKeyTableOffset {opcodeKeyTableOffset:X}, opcodeKeyTableSize {opcodeKeyTableSize:X}");
            var moduleBase = Process.GetCurrentProcess().MainModule!.BaseAddress;
            var opcodeKeyTableAddress = moduleBase + (nint)opcodeKeyTableOffset;
            var opcodeKeyTableBytes = new byte[opcodeKeyTableSize];
            Marshal.Copy(opcodeKeyTableAddress, opcodeKeyTableBytes, 0, opcodeKeyTableSize);
            var emptyTableBytes = Array.Empty<byte>();
            var tableBinaryBasePath = pluginInterface.AssemblyLocation.Directory!.CreateSubdirectory($"unscrambler_tables_{version}").FullName;
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "table0.bin"), emptyTableBytes);
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "table1.bin"), emptyTableBytes);
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "table2.bin"), emptyTableBytes);
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "midtable.bin"), emptyTableBytes);
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "daytable.bin"), emptyTableBytes);
            File.WriteAllBytes(Path.Combine(tableBinaryBasePath, "opcodekeytable.bin"), opcodeKeyTableBytes);
            versionConstants = GetFallbackVersionConstant(opcodeKeyTableOffset, opcodeKeyTableSize);
            keyGenerator = new KeyGenerator73();
            keyGenerator.Initialize(versionConstants, tableBinaryBasePath);
            unscrambler = new Unscrambler73();
            unscrambler.Initialize(versionConstants);
        }

        var rxPtrs = multiScanner.ScanText(GenericDownSignature, 3);
		zoneDownHook = hooks.HookFromAddress<DownPrototype>(rxPtrs[2], ZoneDownDetour);

		Enable();
    }

	public void Enable()
    {
        UpdateKeys();
		zoneDownHook?.Enable();
	}
    
    private void UpdateKeys()
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
            
            Plugin.Log.Debug($"[UpdateKeys] obfuscation override is {obfuscationOverride}");
            Plugin.Log.Debug($"[UpdateKeys] keys {dispatcher->Key0}, {dispatcher->Key1}, {dispatcher->Key2}");
            Plugin.Log.Debug($"[UpdateKeys] game random {dispatcher->GameRandom}, packet random {dispatcher->LastPacketRandom}");
        }
        else
        {
            Plugin.Log.Warning("[UpdateKeys] Dispatcher was null, so not initializing keys");
        }
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
            
            var canDeobfuscate = keyGenerator.ObfuscationEnabled || obfuscationOverride;

            if (needsDeobfuscation && !canDeobfuscate)
            {
                UpdateKeys();
                canDeobfuscate = obfuscationOverride;
            }

            if (needsDeobfuscation && canDeobfuscate)
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
    
    public static VersionConstants GetFallbackVersionConstant(uint opcodeKeyTableOffset, int opcodeKeyTableSize)
    {
        var opcodes = Machina.FFXIV.Headers.Opcodes.OpcodeManager.Instance.CurrentOpcodes;
        return new VersionConstants
        {
            GameVersion = GetRunningGameVersion(),
            InitZoneOpcode = 0x0,
            UnknownObfuscationInitOpcode = 0x0,
            OpcodeKeyTableOffset = opcodeKeyTableOffset,
            OpcodeKeyTableSize = opcodeKeyTableSize,
            ObfuscatedOpcodes = new Dictionary<string, int>
            {
                { "PlayerSpawn", opcodes["PlayerSpawn"] },
                { "NpcSpawn", opcodes["NpcSpawn"] },
                { "NpcSpawn2", opcodes["NpcSpawn2"] },

                { "ActionEffect01", opcodes["Ability1"] },
                { "ActionEffect08", opcodes["Ability8"] },
                { "ActionEffect16", opcodes["Ability16"] },
                { "ActionEffect24", opcodes["Ability24"] },
                { "ActionEffect32", opcodes["Ability32"] },

                { "StatusEffectList", opcodes["StatusEffectList"] },
                { "StatusEffectList3", opcodes["StatusEffectList3"] },

                { "Examine", 0x0 },
                { "UpdateGearset", 0x0 },
                { "UpdateParty", 0x0 },
                { "ActorControl", opcodes["ActorControl"] },
                { "ActorCast", opcodes["ActorCast"] },

                { "UnknownEffect01", 0x0 },
                { "UnknownEffect16", 0x0 },
                { "ActionEffect02", 0x0 },
                { "ActionEffect04", 0x0 }
            }
        };
    }
}
