using System.Diagnostics;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using Dalamud.Hooking;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;

namespace IINACT;

internal class DeucalionController : IDisposable
{
    private readonly int pid;
    private readonly INotificationManager notificationManager;
    private Hook<LoadLibraryWDelegate>? loadLibraryWHook;
    private bool allowLoads;
    
    private delegate nint LoadLibraryWDelegate([MarshalAs(UnmanagedType.LPWStr)] string lpLibFileName);

    public DeucalionController(Process process, IGameInteropProvider hooks, INotificationManager notificationManager)
    {
        this.notificationManager = notificationManager;
        pid = process.Id;
        loadLibraryWHook = hooks.HookFromSymbol<LoadLibraryWDelegate>("Kernel32", "LoadLibraryW", LoadLibraryWDetour);
        loadLibraryWHook.Enable();
        SendExit();
    }
    
    private nint LoadLibraryWDetour(string lpLibFileName)
    {
        Plugin.Log.Verbose($"LoadLibraryW called with {lpLibFileName}.");
        return ShouldLoadLibrary(lpLibFileName) ? loadLibraryWHook!.Original(lpLibFileName) : nint.Zero;
    }

    private bool ShouldLoadLibrary(string lpLibFileName)
    {
        try
        {
            var fileName = Path.GetFileName(lpLibFileName);

            if (fileName.Contains("Deucalion", StringComparison.OrdinalIgnoreCase))
            {
                var versionInfo = FileVersionInfo.GetVersionInfo(lpLibFileName);
                var minimumVersion = new Version(1, 2, 1);
                var deucalionVersion = new Version(versionInfo.FileVersion ?? "0.0.0");

                if (deucalionVersion >= minimumVersion && allowLoads)
                {
                    Plugin.Log.Debug($"Allowed Deucalion version [{deucalionVersion}] to load.");
                    return true;
                }

                notificationManager.AddNotification(new Notification
                {
                    Content = "Blocked loading of Deucalion to prevent crashing.",
                    Title = "Warning",
                });
                Plugin.Log.Warning($"Blocked loading of DLL: {lpLibFileName} (filename: {fileName})");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Plugin.Log.Error(ex, "Exception in LoadLibraryW hook");
            return true;
        }
    }

    /// <summary>
    /// Sends an Exit operation to Deucalion via its named pipe and waits for its unload afterward.
    /// </summary>
    private void SendExit()
    {
        var pipeName = $@"deucalion-{pid}";
        if (SendExitOp(pipeName))
            WaitForDeucalionExit(pipeName);
    }

    /// <summary>
    /// Connects to the Deucalion named pipe and sends the Exit operation payload.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe.</param>
    /// <returns>Returns true if Deucalion was running, otherwise false.</returns>
    private bool SendExitOp(string pipeName)
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

        notificationManager.AddNotification(new Notification
        {
            Content = "Unloading Deucalion to safely start plugin for now. " + 
                      "You will have to restart your Deucalion client (FFXIV_ACT_Plugin, Teamcraft, etc.) " +
                      "after this in order to receive network data.",
            Title = "Warning",
        });

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
    /// Detects Deucalion shutdown by trying to acquire the named pipe it uses.
    /// </summary>
    /// <param name="pipeName">The name of the named pipe to acquire.</param>
    private static void WaitForDeucalionExit(string pipeName)
    {
        const int timeoutSeconds = 5;
        var stopwatch = Stopwatch.StartNew();
        
        while (stopwatch.ElapsedMilliseconds < timeoutSeconds * 1000)
        {
            try
            {
                using var pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1);
                Plugin.Log.Debug($"Acquired pipe at {pipeName}.");
                return;
            }
            catch
            {
                Thread.Sleep(10);
            }
        }
        
        Plugin.Log.Error($"Pipe {pipeName} is still used after after {timeoutSeconds} seconds.");
    }

    internal void AllowLoads() => allowLoads = true;

    public void Dispose()
    {
        loadLibraryWHook?.Disable();
        loadLibraryWHook?.Dispose();
        loadLibraryWHook = null;
    }
}
