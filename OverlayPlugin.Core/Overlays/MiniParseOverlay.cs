using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace RainbowMage.OverlayPlugin.Overlays
{
    public partial class MiniParseOverlay : OverlayBase<MiniParseOverlayConfig>
    {
        protected DateTime lastUrlChange;
        protected string lastLoadedUrl;
        protected System.Threading.Timer previewTimer;
        private readonly FFXIVRepository repository;

        public bool Preview = false;

        public bool ModernApi { get; protected set; }

        public MiniParseOverlay(MiniParseOverlayConfig config, string name, TinyIoCContainer container)
            : base(config, name, container) { }

        public override void Dispose()
        {
            base.Dispose();
            previewTimer?.Dispose();
        }

        public override void Navigate(string url)
        {
            if (Config.ActwsCompatibility)
            {
                if (!url.Contains("HOST_PORT=") && url != "about:blank")
                {
                    if (!url.EndsWith("?"))
                    {
                        if (url.Contains("?"))
                        {
                            url += "&";
                        }
                        else
                        {
                            url += "?";
                        }
                    }

                    url += "HOST_PORT=ws://127.0.0.1/fake/";
                }
            }
            else
            {
                var pos = url.IndexOf("HOST_PORT=");
                if (pos > -1 && url.Contains("/fake/"))
                {
                    url = url[..pos].Trim(new char[] { '?', '&' });
                }
            }

            // If this URL was just loaded (see PrepareWebsite), ignore this request since we're loading that URL already.
            if (url == lastLoadedUrl) return;

            lastUrlChange = DateTime.Now;
            base.Navigate(url);
        }

        public override void Reload()
        {
            // If the user changed the URL less than a second ago, ignore the reload since it would interrupt
            // the currently loading overlay and end up with an empty page.
            // The user probably just wanted to load the page so doing nothing here (in that case) is fine.

            if (DateTime.Now - lastUrlChange > new TimeSpan(0, 0, 1))
            {
                base.Reload();
            }
        }

        public override void Start() { }

        public override void Stop() { }

        public override void HandleEvent(JObject e)
        {
            if (Config.ActwsCompatibility)
            {
                // NOTE: Keep this in sync with WSServer's LegacyHandler.
                switch (e["type"].ToString())
                {
                    case "CombatData":
                        ((IOverlay)this).ExecuteScript(
                            "__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'CombatData', 'msg': " +
                            e.ToString(Formatting.None) + " });");
                        break;
                    case "LogLine":
                        ((IOverlay)this).ExecuteScript(
                            "__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'Chat', 'msg': " +
                            JsonConvert.SerializeObject(e["rawLine"].ToString()) + " });");
                        break;
                    case "ChangeZone":
                        ((IOverlay)this).ExecuteScript(
                            "__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'ChangeZone', 'msg': " +
                            e.ToString(Formatting.None) + " });");
                        break;
                    case "ChangePrimaryPlayer":
                        ((IOverlay)this).ExecuteScript(
                            "__OverlayPlugin_ws_faker({'type': 'broadcast', 'msgtype': 'SendCharName', 'msg': " +
                            e.ToString(Formatting.None) + " });");
                        break;
                }
            }
            else if (ModernApi)
            {
                base.HandleEvent(e);
            }
            else
            {
                // Old OverlayPlugin API
                switch (e["type"].ToString())
                {
                    case "CombatData":
                        ((IOverlay)this).ExecuteScript(
                            "document.dispatchEvent(new CustomEvent('onOverlayDataUpdate', { detail: " +
                            e.ToString(Formatting.None) + " }));");
                        break;
                    case "LogLine":
                        ((IOverlay)this).ExecuteScript(
                            "document.dispatchEvent(new CustomEvent('onLogLine', { detail: " +
                            JsonConvert.SerializeObject(e["line"].ToString(Formatting.None)) + " }));");
                        break;
                }
            }
        }

        public override void InitModernAPI()
        {
            if (!ModernApi)
            {
                // Clear the subscription set in PrepareWebsite().
                Unsubscribe("CombatData");
                Unsubscribe("LogLine");
                ModernApi = true;
            }
        }

        protected override void Update() { }
    }
}
