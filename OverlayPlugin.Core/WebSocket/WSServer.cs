using Advanced_Combat_Tracker;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RainbowMage.OverlayPlugin.Overlays;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace RainbowMage.OverlayPlugin {
    public class WSServer {
        private TinyIoCContainer _container;
        private ILogger _logger;
        private HttpServer _server;
        private IPluginConfig _cfg;
        private PluginMain _plugin;
        private bool _failed = false;

        public EventHandler<StateChangedArgs> OnStateChanged;

        public void Stop() {
            try {
                if (_server != null) {
                    _server.Stop();
                }
            }
            catch (Exception e) {
                Log(LogLevel.Error, Resources.WSShutdownError, e);
            }
            _failed = false;

            OnStateChanged?.Invoke(null, new StateChangedArgs(false, false));
        }

        public bool IsRunning() {
            return _server != null && _server.IsListening;
        }

        public bool IsFailed() {
            return _failed;
        }

        public bool IsSSLPossible() {
            return File.Exists(GetCertPath());
        }

        public WSServer(TinyIoCContainer container) {
            _container = container;
            _logger = container.Resolve<ILogger>();
            _cfg = container.Resolve<IPluginConfig>();
            _plugin = container.Resolve<PluginMain>();
        }

        public void Start() {
            _failed = false;

            try {
                var sslPath = GetCertPath();
                var secure = _cfg.WSServerSSL && File.Exists(sslPath);

                if (_cfg.WSServerIP == "*") {
                    _server = new HttpServer(_cfg.WSServerPort, secure);
                } else {
                    _server = new HttpServer(IPAddress.Parse(_cfg.WSServerIP), _cfg.WSServerPort, secure);
                }

                _server.ReuseAddress = true;
                _server.Log.Output += (LogData d, string msg) => {
                    Log(LogLevel.Info, "WS: {0}: {1} {2}", d.Level.ToString(), d.Message, msg);
                };
                _server.Log.Level = WebSocketSharp.LogLevel.Info;

                if (secure) {
                    Log(LogLevel.Debug, Resources.WSLoadingCert, sslPath);

                    // IMPORTANT: Do *not* change the password here. This is the default password that mkcert uses.
                    // If you use a different password here, you'd have to pass it to mkcert and update the text on the WSServer tab to match.
                    _server.SslConfiguration.ServerCertificate = new X509Certificate2(sslPath, "changeit");
                    _server.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;
                }

                _server.AddWebSocketService<SocketHandler>("/ws", () => new SocketHandler(_container));
                _server.AddWebSocketService<LegacyHandler>("/MiniParse", () => new LegacyHandler(_container));
                _server.AddWebSocketService<LegacyHandler>("/BeforeLogLineRead", () => new LegacyHandler(_container));

                _server.OnGet += (object sender, HttpRequestEventArgs e) => {
                    if (e.Request.RawUrl == "/") {
                        var builder = new StringBuilder();
                        builder.Append(@"<!DOCTYPE html>
<html>
    <head>
        <title>OverlayPlugin WSServer</title>
    </head>
    <body>
        " + Resources.WSIndexPage + @"
        <ul>");

                        foreach (var overlay in _plugin.Overlays) {
                            if (overlay.GetType() != typeof(MiniParseOverlay)) continue;

                            var (confident, url) = GetUrl((MiniParseOverlay)overlay);

                            url = url.Replace("&", "&amp;").Replace("\"", "&quot;");
                            var overlayName = overlay.Name.Replace("&", "&amp;").Replace("<", "&lt;");

                            if (url.StartsWith("file://")) {
                                builder.Append($"<li>Local: {overlayName}: {url}</li>");
                            } else {
                                builder.Append($"<li><a href=\"{url}\">{overlayName}</a>");
                            }

                            if (!confident) {
                                builder.Append(" " + Resources.WSNotConfidentLink);
                            }

                            builder.Append("</li>");
                        }

                        builder.Append("</ul></body></html>");

                        var res = e.Response;
                        res.StatusCode = 200;
                        res.ContentType = "text/html";
                        Ext.WriteContent(res, Encoding.UTF8.GetBytes(builder.ToString()));
                    }
                };

                _server.Start();
                OnStateChanged?.Invoke(this, new StateChangedArgs(true, false));
            }
            catch (Exception e) {
                _failed = true;
                Log(LogLevel.Error, Resources.WSStartFailed, e);
                OnStateChanged?.Invoke(this, new StateChangedArgs(false, true));
            }
        }

        public (bool, string) GetUrl(MiniParseOverlay overlay) {
            var argName = "HOST_PORT";

            if (overlay.ModernApi) {
                argName = "OVERLAY_WS";
            }

            var url = Regex.Replace(overlay.Config.Url, @"[?&](?:HOST_PORT|OVERLAY_WS)=[^&]*", "");
            if (url.Contains("?")) {
                url += "&";
            } else {
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

        public string GetModernUrl(string url) {
            if (url.Contains("?")) {
                url += "&";
            } else {
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

        public string GetCertPath() {
            var path = Path.Combine(
                ActGlobals.oFormActMain.AppDataFolder.FullName,
                "Config",
                "OverlayPluginSSL.p12");

            return path;
        }

        private void Log(LogLevel level, string msg, params object[] args) {
            _logger.Log(level, msg, args);
        }

        public class SocketHandler : WebSocketBehavior, IEventReceiver {
            public string Name => "WSHandler";
            private ILogger _logger;
            private EventDispatcher _dispatcher;

            public SocketHandler(TinyIoCContainer container) : base() {
                _logger = container.Resolve<ILogger>();
                _dispatcher = container.Resolve<EventDispatcher>();
            }


            public void HandleEvent(JObject e) {
                Task.Run(() => {
                    try {
                        Send(e.ToString(Formatting.None));
                    }
                    catch (Exception ex) {
                        _logger.Log(LogLevel.Error, Resources.WSMessageSendFailed, e);
                        _dispatcher.UnsubscribeAll(this);
                    }
                });
            }

            protected override void OnOpen() {
            }

            protected override void OnMessage(MessageEventArgs e) {
                JObject data = null;

                try {
                    data = JObject.Parse(e.Data);
                }
                catch (JsonException ex) {
                    _logger.Log(LogLevel.Error, Resources.WSInvalidDataRecv, ex, e.Data);
                    return;
                }

                if (!data.ContainsKey("call")) return;

                var msgType = data["call"].ToString();
                if (msgType == "subscribe") {
                    try {
                        foreach (var item in data["events"].ToList()) {
                            _dispatcher.Subscribe(item.ToString(), this);
                        }
                    }
                    catch (Exception ex) {
                        _logger.Log(LogLevel.Error, Resources.WSNewSubFail, ex);
                    }

                    return;
                } else if (msgType == "unsubscribe") {
                    try {
                        foreach (var item in data["events"].ToList()) {
                            _dispatcher.Unsubscribe(item.ToString(), this);
                        }
                    }
                    catch (Exception ex) {
                        _logger.Log(LogLevel.Error, Resources.WSUnsubFail, ex);
                    }
                    return;
                }

                Task.Run(() => {
                    try {
                        var response = _dispatcher.CallHandler(data);

                        if (response != null && response.Type != JTokenType.Object) {
                            throw new Exception("Handler response must be an object or null");
                        }

                        if (response == null) {
                            response = new JObject();
                            response["$isNull"] = true;
                        }

                        if (data.ContainsKey("rseq")) {
                            response["rseq"] = data["rseq"];
                        }

                        Send(response.ToString(Formatting.None));
                    }
                    catch (Exception ex) {
                        _logger.Log(LogLevel.Error, Resources.WSHandlerException, ex);
                    }
                });
            }

            protected override void OnClose(CloseEventArgs e) {
                _dispatcher.UnsubscribeAll(this);
            }
        }

        private class LegacyHandler : WebSocketBehavior, IEventReceiver {
            public string Name => "WSLegacyHandler";
            private ILogger _logger;
            private EventDispatcher _dispatcher;
            private FFXIVRepository _repository;

            public LegacyHandler(TinyIoCContainer container) : base() {
                _logger = container.Resolve<ILogger>();
                _dispatcher = container.Resolve<EventDispatcher>();
                _repository = container.Resolve<FFXIVRepository>();
            }

            protected override void OnOpen() {
                base.OnOpen();

                _dispatcher.Subscribe("CombatData", this);
                _dispatcher.Subscribe("LogLine", this);
                _dispatcher.Subscribe("ChangeZone", this);
                _dispatcher.Subscribe("ChangePrimaryPlayer", this);

                Send(JsonConvert.SerializeObject(new {
                    type = "broadcast",
                    msgtype = "SendCharName",
                    msg = new {
                        charName = _repository.GetPlayerName() ?? "YOU",
                        charID = _repository.GetPlayerID()
                    }
                }));
            }

            protected override void OnClose(CloseEventArgs e) {
                base.OnClose(e);

                _dispatcher.UnsubscribeAll(this);
            }


            public void HandleEvent(JObject e) {
                try {
                    switch (e["type"].ToString()) {
                        case "CombatData":
                            Send("{\"type\":\"broadcast\",\"msgtype\":\"CombatData\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                        case "LogLine":
                            Send("{\"type\":\"broadcast\",\"msgtype\":\"Chat\",\"msg\":" + JsonConvert.SerializeObject(e["rawLine"].ToString()) + "}");
                            break;
                        case "ChangeZone":
                            Send("{\"type\":\"broadcast\",\"msgtype\":\"ChangeZone\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                        case "ChangePrimaryPlayer":
                            Send("{\"type\":\"broadcast\",\"msgtype\":\"SendCharName\",\"msg\":" + e.ToString(Formatting.None) + "}");
                            break;
                    }
                }
                catch (InvalidOperationException ex) {
                    _logger.Log(LogLevel.Error, "Failed to send legacy WS message: {0}", ex);
                    _dispatcher.UnsubscribeAll(this);
                }
            }

            protected override void OnMessage(MessageEventArgs e) {
                JObject data = null;

                try {
                    data = JObject.Parse(e.Data);
                }
                catch (JsonException ex) {
                    _logger.Log(LogLevel.Error, Resources.WSInvalidDataRecv, ex, e.Data);
                    return;
                }

                if (!data.ContainsKey("type") || !data.ContainsKey("msgtype")) return;

                switch (data["msgtype"].ToString()) {
                    case "Capture":
                        _logger.Log(LogLevel.Warning, "ACTWS Capture is not supported outside of overlays.");
                        break;
                    case "RequestEnd":
                        ActGlobals.oFormActMain.EndCombat(true);
                        break;
                }
            }
        }

        public class StateChangedArgs : EventArgs {
            public bool Running { get; private set; }
            public bool Failed { get; private set; }

            public StateChangedArgs(bool Running, bool Failed) {
                this.Running = Running;
                this.Failed = Failed;
            }
        }
    }
}
