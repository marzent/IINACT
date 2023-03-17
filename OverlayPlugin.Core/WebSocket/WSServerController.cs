#nullable enable
using System;
using System.IO;
using System.Net;
using Advanced_Combat_Tracker;

namespace RainbowMage.OverlayPlugin.WebSocket;

public class WSServerController
{
    public EventHandler<StateChangedArgs>? OnStateChanged;

    public WSServerController(TinyIoCContainer container)
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
    public bool Running => Server?.IsAccepting ?? false;

    public void Stop()
    {
        try
        {
            if (Server is not null)
            {
                Server.Stop();
                Server.Dispose();
                Server = null;
            }
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, Resources.WSShutdownError, e);
        }

        Failed = false;

        OnStateChanged?.Invoke(null, new StateChangedArgs(false, false));
    }

    public void Restart()
    {
        try
        {
            Server?.Restart();
        }
        catch (Exception e)
        {
            Logger.Log(LogLevel.Error, Resources.WSStartFailed, e);
        }

        Failed = false;

        OnStateChanged?.Invoke(null, new StateChangedArgs(true, false));
    }

    public bool IsRunning()
    {
        return Server?.IsAccepting ?? false;
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
            Server.Start();

            OnStateChanged?.Invoke(this, new StateChangedArgs(true, false));
        }
        catch (Exception e)
        {
            Failed = true;
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
        public StateChangedArgs(bool Running, bool Failed)
        {
            this.Running = Running;
            this.Failed = Failed;
        }

        public bool Running { get; private set; }
        public bool Failed { get; private set; }
    }
}
