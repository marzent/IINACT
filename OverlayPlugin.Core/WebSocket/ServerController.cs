#nullable enable
using System;
using System.IO;
using System.Net;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin.WebSocket;

public class ServerController
{
    public EventHandler<StateChangedArgs>? OnStateChanged;

    public ServerController(TinyIoCContainer container)
    {
        Container = container;
        Logger = container.Resolve<ILogger>();
        Config = container.Resolve<IPluginConfig>();
    }

    private TinyIoCContainer Container { get; }
    private ILogger Logger { get; }
    private OverlayServer? Server { get; set; }
    private IPluginConfig Config { get; }
    public bool Failed { get; private set; }
    public Exception? LastException { get; private set; }
    public bool Running => Server?.IsAccepting ?? false;
    public string? Address => Server?.Address;
    public int? Port => Server?.Port;
    public bool Secure => false;
    public Uri Uri => new($"{(Secure ? "wss" : "ws")}://{Address}:{Port}");

    public void Stop()
    {
        try
        {
            Server?.Stop();
        }
        catch (Exception e)
        {
            LastException = e;
            Logger.Log(LogLevel.Error, Resources.WSShutdownError, e);
        }

        Failed = false;

        OnStateChanged?.Invoke(null, new StateChangedArgs(false, false));
    }

    public void Restart()
    {
        Stop();
        Start();
    }

    public bool IsSSLPossible()
    {
        return File.Exists(GetCertPath());
    }

    public void Start()
    {
        Failed = false;

        try
        {
            // TODO: add SSL support
            // var sslPath = GetCertPath();
            // var secure = _cfg.WSServerSSL && File.Exists(sslPath);

            var address = Config.WSServerIP == "*" ? IPAddress.Any : IPAddress.Parse(Config.WSServerIP);

            Server = new OverlayServer(address, Config.WSServerPort, Container);
            Server.OptionReuseAddress = true;
            
            Server.Start();

            OnStateChanged?.Invoke(this, new StateChangedArgs(true, false));
        }
        catch (Exception e)
        {
            Failed = true;
            LastException = e;
            Logger.Log(LogLevel.Error, Resources.WSStartFailed, e);
            OnStateChanged?.Invoke(this, new StateChangedArgs(false, true));
        }
    }

    public string GetModernUrl(string url)
    {
        if (url.Contains("?"))
            url += "&";
        else
            url += "?";

        url += "OVERLAY_WS=ws";
        if (Config.WSServerSSL) url += "s";
        url += "://";
        if (Config.WSServerIP == "*" || Config.WSServerIP == "0.0.0.0")
            url += "127.0.0.1";
        else
            url += Config.WSServerIP;

        url += ":" + Config.WSServerPort + "/ws";
        return url;
    }

    public string GetCertPath()
    {
        var path = Path.Combine(
            ActGlobals.oFormActMain.AppDataFolder.FullName,
            "Config",
            "OverlayPluginSSL.p12");

        return path;
    }
    
    public class StateChangedArgs : EventArgs
    {
        public StateChangedArgs(bool running, bool failed)
        {
            this.Running = running;
            this.Failed = failed;
        }

        public bool Running { get; private set; }
        public bool Failed { get; private set; }
    }
}
