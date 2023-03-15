using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Advanced_Combat_Tracker;
using Fleck;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.Overlays;

namespace RainbowMage.OverlayPlugin
{
    public class WSServer
    {
        TinyIoCContainer _container;
        ILogger _logger;
        WebSocketServer _server;
        IPluginConfig _cfg;
        PluginMain _plugin;
        List<IWSConnection> _connections = new List<IWSConnection>();
        bool _failed = false;

        public EventHandler<StateChangedArgs> OnStateChanged;

        interface IWSConnection : IEventReceiver
        {
            void Close();
        }

        public void Stop()
        {
            try
            {
                if (_server != null)
                {
                    _server.Dispose();
                    _server = null;

                    foreach (var conn in _connections)
                    {
                        conn.Close();
                    }
                    _connections.Clear();
                }
            }
            catch (Exception e)
            {
                Log(LogLevel.Error, Resources.WSShutdownError, e);
            }
            _failed = false;

            OnStateChanged?.Invoke(null, new StateChangedArgs(false, false));
        }

        public bool IsRunning()
        {
            return _server != null;
        }

        public bool IsFailed()
        {
            return _failed;
        }

        public bool IsSSLPossible()
        {
            return File.Exists(GetCertPath());
        }

        public WSServer(TinyIoCContainer container)
        {
            _container = container;
            _logger = container.Resolve<ILogger>();
            _cfg = container.Resolve<IPluginConfig>();
            _plugin = container.Resolve<PluginMain>();
        }

        public void Start()
        {
            _failed = false;

            FleckLog.LogAction = (level, message, ex) =>
            {
#if !DEBUG
                if (level < Fleck.LogLevel.Warn)
                    return;
#endif
                LogLevel ourLevel = LogLevel.Info;
                switch (level)
                {
                    case Fleck.LogLevel.Error:
                        ourLevel = LogLevel.Error;
                        break;
                    case Fleck.LogLevel.Warn:
                        ourLevel = LogLevel.Warning;
                        break;
                    case Fleck.LogLevel.Info:
                        ourLevel = LogLevel.Info;
                        break;
                    case Fleck.LogLevel.Debug:
                        //ourLevel = LogLevel.Debug;

                        // prevent log spam
                        return;
                }

                _logger.Log(ourLevel, $"WSServer: {message} {ex}");
            };

            try
            {
                var sslPath = GetCertPath();
                var secure = _cfg.WSServerSSL && File.Exists(sslPath);

                if (_cfg.WSServerIP == "*")
                {
                    _server = new WebSocketServer((secure ? "wss://" : "ws://") + "0.0.0.0:" + _cfg.WSServerPort);
                }
                else
                {
                    _server = new WebSocketServer((secure ? "wss://" : "ws://") + _cfg.WSServerIP + ":" + _cfg.WSServerPort);
                }

                if (secure)
                {
                    Log(LogLevel.Info, Resources.WSLoadingCert, sslPath);

                    // IMPORTANT: Do *not* change the password here. This is the default password that mkcert uses.
                    // If you use a different password here, you'd have to pass it to mkcert and update the text on the WSServer tab to match.
                    _server.Certificate = new X509Certificate2(sslPath, "changeit");
                    if ((_server.EnabledSslProtocols & System.Security.Authentication.SslProtocols.Tls12) == 0)
                    {
                        _server.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;
                    }
                }

                _server.RestartAfterListenError = true;
                _server.ListenerSocket.NoDelay = true;

                _server.Start((conn) =>
                {
                    Log(LogLevel.Debug, $"Got request on WSServer at {conn.ConnectionInfo.Path}");

                    switch (conn.ConnectionInfo.Path)
                    {
                        case "/ws":
                            _connections.Add(new SocketHandler(_container, conn, this));
                            break;
                        case "/MiniParse":
                            _connections.Add(new LegacyHandler(_container, conn, this));
                            break;
                        case "/BeforeLogLineRead":
                            _connections.Add(new LegacyHandler(_container, conn, this));
                            break;
                    }
                });

                OnStateChanged?.Invoke(this, new StateChangedArgs(true, false));
            }
            catch (Exception e)
            {
                _failed = true;
                _server = null;
                Log(LogLevel.Error, Resources.WSStartFailed, e);
                OnStateChanged?.Invoke(this, new StateChangedArgs(false, true));
            }
        }

        public (bool, string) GetUrl(MiniParseOverlay overlay)
        {
            string argName = "HOST_PORT";

            if (overlay.ModernApi)
            {
                argName = "OVERLAY_WS";
            }

            var url = Regex.Replace(overlay.Config.Url, @"[?&](?:HOST_PORT|OVERLAY_WS)=[^&]*", "");
            if (url.Contains("?"))
            {
                url += "&";
            }
            else
            {
                url += "?";
            }

            url += argName + "=ws";
            if (_cfg.WSServerSSL) url += "s";
            url += "://";
            if (_cfg.WSServerIP == "*" || _cfg.WSServerIP == "0.0.0.0")
                url += "127.0.0.1";
            else
                url += _cfg.WSServerIP;

            url += ":" + _cfg.WSServerPort + "/";

            if (argName == "OVERLAY_WS") url += "ws";

            return (argName != "HOST_PORT" || overlay.Config.ActwsCompatibility, url);
        }

        public string GetModernUrl(string url)
        {
            if (url.Contains("?"))
            {
                url += "&";
            }
            else
            {
                url += "?";
            }

            url += "OVERLAY_WS=ws";
            if (_cfg.WSServerSSL) url += "s";
            url += "://";
            if (_cfg.WSServerIP == "*" || _cfg.WSServerIP == "0.0.0.0")
                url += "127.0.0.1";
            else
                url += _cfg.WSServerIP;

            url += ":" + _cfg.WSServerPort + "/ws";
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

        private void Log(LogLevel level, string msg, params object[] args)
        {
            _logger.Log(level, msg, args);
        }

        public class SocketHandler : IWSConnection
        {
            public string Name => "WSHandler";
            private ILogger _logger;
            private EventDispatcher _dispatcher;
            private IWebSocketConnection _conn;

            public SocketHandler(TinyIoCContainer container, IWebSocketConnection conn, WSServer server)
            {
                _logger = container.Resolve<ILogger>();
                _dispatcher = container.Resolve<EventDispatcher>();
                _conn = conn;

                var open = true;

                conn.OnMessage = OnMessage;
                conn.OnClose = () =>
                {
                    if (!open) return;
                    open = false;

                    try
                    {
                        _dispatcher.UnsubscribeAll(this);
                        server._connections.Remove(this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, $"Failed to unsubscribe WebSocket connection: {ex}");
                    }
                };
                conn.OnError = (ex) =>
                {
                    // Fleck will close the connection; make sure we always clean up even if Fleck doesn't call OnClose().
                    conn.OnClose();

                    _logger.Log(LogLevel.Info, $"WebSocket connection was closed with error: {ex}");
                };
            }

            public void Close()
            {
                _conn.Close();
            }

            public void HandleEvent(JObject e)
            {
                if (!_conn.IsAvailable)
                {
                    _logger.Log(LogLevel.Error, "A closed WebSocket connection wasn't cleaned up properly; fixing.");
                    _conn.OnClose();
                    return;
                }

                _conn.Send(e.ToString(Formatting.None));
            }

            public void OnMessage(string message)
            {
                JObject data = null;

                try
                {
                    data = JObject.Parse(message);
                }
                catch (JsonException ex)
                {
                    _logger.Log(LogLevel.Error, Resources.WSInvalidDataRecv, ex, message);
                    return;
                }

                if (!data.ContainsKey("call")) return;

                var msgType = data["call"].ToString();
                if (msgType == "subscribe")
                {
                    try
                    {
                        foreach (var item in data["events"].ToList())
                        {
                            _dispatcher.Subscribe(item.ToString(), this);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, Resources.WSNewSubFail, ex);
                    }

                    return;
                }
                else if (msgType == "unsubscribe")
                {
                    try
                    {
                        foreach (var item in data["events"].ToList())
                        {
                            _dispatcher.Unsubscribe(item.ToString(), this);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, Resources.WSUnsubFail, ex);
                    }
                    return;
                }

                Task.Run(() =>
                {
                    try
                    {
                        var response = _dispatcher.CallHandler(data);

                        if (response != null && response.Type != JTokenType.Object)
                        {
                            throw new Exception("Handler response must be an object or null");
                        }

                        if (response == null)
                        {
                            response = new JObject();
                            response["$isNull"] = true;
                        }

                        if (data.ContainsKey("rseq"))
                        {
                            response["rseq"] = data["rseq"];
                        }

                        _conn.Send(response.ToString(Formatting.None));
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, Resources.WSHandlerException, ex);
                    }
                });
            }
        }

        private class LegacyHandler : IWSConnection
        {
            public string Name => "WSLegacyHandler";
            private ILogger _logger;
            private EventDispatcher _dispatcher;
            private FFXIVRepository _repository;
            private IWebSocketConnection _conn;

            public LegacyHandler(TinyIoCContainer container, IWebSocketConnection conn, WSServer server) : base()
            {
                _logger = container.Resolve<ILogger>();
                _dispatcher = container.Resolve<EventDispatcher>();
                _repository = container.Resolve<FFXIVRepository>();
                _conn = conn;

                var open = true;

                conn.OnOpen = OnOpen;
                conn.OnMessage = OnMessage;
                conn.OnClose = () =>
                {
                    if (!open) return;
                    open = false;

                    try
                    {
                        _dispatcher.UnsubscribeAll(this);
                        server._connections.Remove(this);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, $"Failed to unsubscribe WebSocket connection: {ex}");
                    }
                };
                conn.OnError = (ex) =>
                {
                    // Fleck will close the connection; make sure we always clean up even if Fleck doesn't call OnClose().
                    conn.OnClose();

                    _logger.Log(LogLevel.Info, $"WebSocket connection was closed with error: {ex}");
                };
            }

            public void Close()
            {
                _conn.Close();
            }

            public void HandleEvent(JObject e)
            {
                if (!_conn.IsAvailable)
                {
                    _logger.Log(LogLevel.Error, "A closed WebSocket connection wasn't cleaned up properly; fixing.");
                    _conn.OnClose();
                    return;
                }

                try
                {
                    switch (e["type"].ToString())
                    {
                        case "CombatData":
                            _conn.Send("{\"type\":\"broadcast\",\"msgtype\":\"CombatData\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                        case "LogLine":
                            _conn.Send("{\"type\":\"broadcast\",\"msgtype\":\"Chat\",\"msg\":" + JsonConvert.SerializeObject(e["rawLine"].ToString()) + "}");
                            break;
                        case "ChangeZone":
                            _conn.Send("{\"type\":\"broadcast\",\"msgtype\":\"ChangeZone\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                        case "ChangePrimaryPlayer":
                            _conn.Send("{\"type\":\"broadcast\",\"msgtype\":\"SendCharName\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                    }
                }
                catch (InvalidOperationException ex)
                {
                    _logger.Log(LogLevel.Error, "Failed to send legacy WS message: {0}", ex);
                    _conn.Close();
                }
            }

            protected void OnOpen()
            {
                _dispatcher.Subscribe("CombatData", this);
                _dispatcher.Subscribe("LogLine", this);
                _dispatcher.Subscribe("ChangeZone", this);
                _dispatcher.Subscribe("ChangePrimaryPlayer", this);

                _conn.Send(JsonConvert.SerializeObject(new
                {
                    type = "broadcast",
                    msgtype = "SendCharName",
                    msg = new
                    {
                        charName = _repository.GetPlayerName() ?? "YOU",
                        charID = _repository.GetPlayerID()
                    }
                }));
            }

            protected void OnMessage(string message)
            {
                JObject data = null;

                try
                {
                    data = JObject.Parse(message);
                }
                catch (JsonException ex)
                {
                    _logger.Log(LogLevel.Error, Resources.WSInvalidDataRecv, ex, message);
                    return;
                }

                if (!data.ContainsKey("type") || !data.ContainsKey("msgtype")) return;

                switch (data["msgtype"].ToString())
                {
                    case "Capture":
                        _logger.Log(LogLevel.Warning, "ACTWS Capture is not supported outside of overlays.");
                        break;
                    case "RequestEnd":
                        ActGlobals.oFormActMain.EndCombat(true);
                        break;
                }
            }
        }

        public class StateChangedArgs : EventArgs
        {
            public bool Running { get; private set; }
            public bool Failed { get; private set; }

            public StateChangedArgs(bool Running, bool Failed)
            {
                this.Running = Running;
                this.Failed = Failed;
            }
        }
    }
}
