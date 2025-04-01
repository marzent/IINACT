using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Plugin.Services;

namespace IINACT;

internal class DeucalionController : IDisposable
{
    private readonly int pid;
    private NamedPipeServerStream? pipeServer;
    private Hook<LoadLibraryWDelegate>? loadLibraryWHook;
    
    private delegate nint LoadLibraryWDelegate([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

    public DeucalionController(Process process, IGameInteropProvider hooks)
    {
        pid = process.Id;
        loadLibraryWHook = hooks.HookFromSymbol<LoadLibraryWDelegate>("Kernel32", "LoadLibraryW", LoadLibraryWDetour);
        loadLibraryWHook.Enable();
        SendExitAndLockPipe();
    }
    
    private nint LoadLibraryWDetour(string lpLibFileName)
    {
        Plugin.Log.Verbose($"LoadLibraryW called with {lpLibFileName}.");

        var fileName = Path.GetFileName(lpLibFileName);
        if (fileName.Contains("Deucalion", StringComparison.OrdinalIgnoreCase))
        {
            Plugin.Log.Warning($"Blocked loading of DLL: {lpLibFileName} (filename: {fileName})");
            return nint.Zero;
        }

        return loadLibraryWHook!.Original(lpLibFileName);
    }

    /// <summary>
    /// Sends an Exit operation to Deucalion via its named pipe and locks the pipe name afterward.
    /// </summary>
    private void SendExitAndLockPipe()
    {
        var pipeName = $@"deucalion-{pid}";
        if (SendExitOp(pipeName))
            LockPipeName(pipeName);
    }

    /// <summary>
    /// Connects to the Deucalion named pipe and sends the Exit operation payload.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe.</param>
    /// <returns>Returns true if Deucalion was running, otherwise false.</returns>
    private static bool SendExitOp(string pipeName)
    {
        using var pipeClient = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
        try
        {
            pipeClient.Connect(0);
        }
        catch (TimeoutException)
        {
            Plugin.Log.Debug("Deucalion is not running.");
            return false;
        }

        // Construct the 9-byte Exit OP payload:
        // - Bytes 0-3: Length (9)
        // - Byte 4: OP (2 for Exit)
        // - Bytes 5-8: Channel (0)
        var payload = new byte[9];
        BitConverter.GetBytes(9u).CopyTo(payload, 0);   // LENGTH
        payload[4] = 2;                                      // OP
        BitConverter.GetBytes(0u).CopyTo(payload, 5);   // CHANNEL
        
        pipeClient.Write(payload, 0, payload.Length);
        pipeClient.Flush();
        return true;
    }

    /// <summary>
    /// Locks the pipe name by creating a new named pipe server and holding it.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to lock.</param>
    private void LockPipeName(string pipeName)
    {
        const int timeoutSeconds = 5;
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutSeconds * 1000)
        {
            try
            {
                pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
                Plugin.Log.Debug($"Acquired pipe at {pipeName}.");
                return;
            }
            catch
            {
                // keep spinning to get the handle first
            }
        }
        
        Plugin.Log.Error($"Failed to lock pipe {pipeName} after {timeoutSeconds} seconds.");
    }

    public void Dispose()
    {
        pipeServer?.Dispose();
        pipeServer = null;
        loadLibraryWHook?.Disable();
        loadLibraryWHook?.Dispose();
        loadLibraryWHook = null;
    }
}
