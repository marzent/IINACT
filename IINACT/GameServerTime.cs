using System.Runtime.InteropServices;
using Dalamud;
using Dalamud.Game;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace IINACT;

internal class GameServerTime : IDisposable
{
    public static ulong LastSeverTimestamp { get; private set; }
    
    private static long LastSeverTimestampTicks { get; set; }
    
    private static readonly DateTime Date1970 = DateTime.MinValue.AddYears(1969);

    public static DateTime LastServerTime => LastSeverTimestamp > 0
                                                 ? Date1970.AddTicks((long)LastSeverTimestamp * 10_000L).ToLocalTime()
                                                 : DateTime.Now;
    
    public static DateTime CurrentServerTime => LastSeverTimestamp > 0
                                                    ? LastServerTime.AddMilliseconds(
                                                        Environment.TickCount64 - LastSeverTimestampTicks)
                                                    : DateTime.Now;

    [StructLayout(LayoutKind.Explicit)]
    public struct FfxivPacketHeader
    {
        [FieldOffset(16)]
        public ulong timestamp;
    }
    
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    private delegate nint RawPacketReceiveDelegate(
        nint param1, nint param2, nint param3, nint param4, int param5, nint param6, nint param7, nint param8);
    
    private readonly Hook<RawPacketReceiveDelegate> rawPacketReceiveHook;

    public GameServerTime(IGameInteropProvider gameInteropProvider, ISigScanner sigScanner)
    {
        var hookSignature = CreateUniqueRawPacketReceiveSig(sigScanner);
        Plugin.Log.Debug($"Got hook sig for raw zone down: {hookSignature}");
        rawPacketReceiveHook =
            gameInteropProvider.HookFromSignature<RawPacketReceiveDelegate>(hookSignature, RawPacketReceiveDetour);
        rawPacketReceiveHook.Enable();
    }

    private static string CreateUniqueRawPacketReceiveSig(ISigScanner sigScanner)
    {
        const string callPattern = "E8 ?? ?? ?? ?? 4C 8B 4F 10 8B 47 1C 45";
        var baseAddress = sigScanner.TextSectionBase;
        var size = sigScanner.TextSectionSize;
        var currentAddress = baseAddress;
        var remainingSize = size;
        var functionAddress = nint.Zero;

        while (remainingSize > 0)
        {
            try
            {
                var scanRet = SigScanner.Scan(currentAddress, remainingSize, callPattern);
                Plugin.Log.Debug($"Found CALL instruction at {scanRet:X}");
                functionAddress = scanRet;
                remainingSize -= (int)(functionAddress.ToInt64() - currentAddress.ToInt64());
                currentAddress = functionAddress + 1;
                Plugin.Log.Debug($"Remaining size: {remainingSize:X}");
            }
            catch (KeyNotFoundException)
            {
                Plugin.Log.Debug($"No more CALL instructions found. Remaining size: {remainingSize}");
                break;
            }
        }

        if (functionAddress == nint.Zero)
            throw new KeyNotFoundException("Could not resolve RawPacketReceive address.");
        
        var currentByte = Marshal.ReadByte(functionAddress);

        while (currentByte != 0xCC) currentByte = Marshal.ReadByte(--functionAddress);

        functionAddress++;

        if (!SafeMemory.ReadBytes(functionAddress, 120, out var functionBytes))
            throw new KeyNotFoundException("Resolved bad RawPacketReceive address.");
                
        return BitConverter.ToString(functionBytes).Replace("-", " ");
    }

    private unsafe nint RawPacketReceiveDetour(
        nint param1, nint param2, nint param3, nint param4, int param5, nint param6, nint param7, nint param8)
    {
        var timestamp = ((FfxivPacketHeader*)(*(void**)(param1 + 16)))->timestamp;
        if (timestamp > 0)
        {
            LastSeverTimestamp = timestamp;
            LastSeverTimestampTicks = Environment.TickCount64;
        }
        return rawPacketReceiveHook.Original(param1, param2, param3, param4, param5, param6, param7, param8);
    }

    public void Dispose()
    {
        rawPacketReceiveHook.Dispose();
    }
}
