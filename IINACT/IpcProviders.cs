using System.Reflection;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;

namespace IINACT;

internal class IpcProviders : IDisposable
{
    private readonly DalamudPluginInterface dalamudPluginInterface;

    internal static Version IpcVersion => new(2, 0, 0);
    internal readonly ICallGateProvider<Version> GetVersion;
    internal readonly ICallGateProvider<Version> GetIpcVersion;

    private event EventHandler<string> OnSendMessageOverIpcEvent;
    private readonly List<string> ipcMessageEndpoints = new ();

    public RainbowMage.OverlayPlugin.WebSocket.ServerController? Server { get; set; }
    internal readonly ICallGateProvider<string, bool> SubscribeToCombatEvents;
    internal readonly ICallGateProvider<string, bool> UnsubscribeFromCombatEvents;
    internal readonly ICallGateProvider<bool> GetServerRunning;
    internal readonly ICallGateProvider<int> GetServerPort;
    internal readonly ICallGateProvider<string> GetServerIp;
    internal readonly ICallGateProvider<bool> GetServerSslEnabled;
    internal readonly ICallGateProvider<Uri?> GetServerUri;
    
    internal IpcProviders(DalamudPluginInterface pluginInterface)
    {
        dalamudPluginInterface = pluginInterface;
        OnSendMessageOverIpcEvent += SendMessageOverIpc;

        GetVersion = pluginInterface.GetIpcProvider<Version>("IINACT.Version");
        GetIpcVersion = pluginInterface.GetIpcProvider<Version>("IINACT.IpcVersion");
        
        SubscribeToCombatEvents = pluginInterface.GetIpcProvider<string, bool>("IINACT.Server.SubscribeToCombatEvents");
        UnsubscribeFromCombatEvents = pluginInterface.GetIpcProvider<string, bool>("IINACT.Server.UnsubscribeFromCombatEvents");
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

        SubscribeToCombatEvents.RegisterFunc(
            specifiedEndpoint =>
            {
                if (Server == null) return false;
                if (ipcMessageEndpoints.Count < 1 && Server.SubscribeToIpcSession(OnSendMessageOverIpcEvent))
                {
                    ipcMessageEndpoints.Add(specifiedEndpoint);
                    return true;
                }

                return false;
            }
        );
        UnsubscribeFromCombatEvents.RegisterFunc(
            specifiedEndpoint =>
            {
                var success = ipcMessageEndpoints.Remove(specifiedEndpoint);
                if (ipcMessageEndpoints.Count < 1)
                {
                    Server?.UnsubscribeFromIpcSession();
                }

                return success;
            }
        );

        GetServerRunning.RegisterFunc(() => Server?.Running ?? false);
        GetServerPort.RegisterFunc(() => Server?.Port ?? 0);
        GetServerIp.RegisterFunc(() => Server?.Address ?? "");
        GetServerSslEnabled.RegisterFunc(() => Server?.Secure ?? false);
        GetServerUri.RegisterFunc(() => Server?.Uri);
    }

    private void SendMessageOverIpc(object? o, string message)
    {
        foreach (var endpoint in ipcMessageEndpoints)
        {
            dalamudPluginInterface.GetIpcSubscriber<string, bool>(endpoint).InvokeFunc(message);
        }
    }

    public void Dispose()
    {
        GetVersion.UnregisterFunc();
        GetIpcVersion.UnregisterFunc();

        Server?.UnsubscribeFromIpcSession();
        SubscribeToCombatEvents.UnregisterFunc();
        UnsubscribeFromCombatEvents.UnregisterFunc();

        GetServerRunning.UnregisterFunc();
        GetServerPort.UnregisterFunc();
        GetServerIp.UnregisterFunc();
        GetServerSslEnabled.UnregisterFunc();
        GetServerUri.UnregisterFunc();
    }
}
