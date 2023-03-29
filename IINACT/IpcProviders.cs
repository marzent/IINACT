using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace IINACT;

internal class IpcProviders : IDisposable
{
    internal static Version IpcVersion => new(2, 0, 0);
    internal readonly ICallGateProvider<Version> GetVersion;
    internal readonly ICallGateProvider<Version> GetIpcVersion;

    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }
    internal readonly ICallGateProvider<bool> GetServerRunning;
    internal readonly ICallGateProvider<int> GetServerPort;
    internal readonly ICallGateProvider<string> GetServerIp;
    internal readonly ICallGateProvider<bool> GetServerSslEnabled;
    internal readonly ICallGateProvider<Uri?> GetServerUri;
    
    internal IpcProviders(DalamudPluginInterface pluginInterface)
    {
        GetVersion = pluginInterface.GetIpcProvider<Version>("IINACT.Version");
        GetIpcVersion = pluginInterface.GetIpcProvider<Version>("IINACT.IpcVersion");
        
        GetServerRunning = pluginInterface.GetIpcProvider<bool>("IINACT.Server.Listening");
        GetServerPort = pluginInterface.GetIpcProvider<int>("IINACT.Server.Port");
        GetServerIp = pluginInterface.GetIpcProvider<string>("IINACT.Server.Ip");
        GetServerSslEnabled = pluginInterface.GetIpcProvider<bool>("IINACT.Server.SslEnabled");
        GetServerUri = pluginInterface.GetIpcProvider<Uri?>("IINACT.Server.Uri");
        
        Register();
    }

    private void Register()
    {
        GetVersion.RegisterFunc(() => Assembly.GetExecutingAssembly().GetName().Version!);
        GetIpcVersion.RegisterFunc(() => IpcVersion);
        
        GetServerRunning.RegisterFunc(() => Server?.Running ?? false);
        GetServerPort.RegisterFunc(() => Server?.Port ?? 0);
        GetServerIp.RegisterFunc(() => Server?.Address ?? "");
        GetServerSslEnabled.RegisterFunc(() => Server?.Secure ?? false);
        GetServerUri.RegisterFunc(() => Server?.Uri);
    }

    public void Dispose()
    {
        GetVersion.UnregisterFunc();
        GetIpcVersion.UnregisterFunc();
        
        GetServerRunning.UnregisterFunc();
        GetServerPort.UnregisterFunc();
        GetServerIp.UnregisterFunc();
        GetServerSslEnabled.UnregisterFunc();
        GetServerUri.UnregisterFunc();
    }
}
