using DarkUI.Docking;
using System;
using System.Web;

namespace RainbowMage.OverlayPlugin {
    public partial class WSConfigPanel : DarkDockContent {
        private const string MKCERT_DOWNLOAD = "https://github.com/FiloSottile/mkcert/releases/download/v1.4.3/mkcert-v1.4.3-windows-amd64.exe";
        private const string NGROK_DOWNLOAD_IDX = "https://ngrok.com/download";

        private IPluginConfig _config;
        private WSServer _server;
        private PluginMain _plugin;
        private Registry _registry;
        private string _ngrokPrefix = null;

        private enum TunnelStatus {
            Unknown,
            Inactive,
            Downloading,
            Launching,
            Active,
            Error
        };

        public WSConfigPanel(TinyIoCContainer container) {
            InitializeComponent();

            _config = container.Resolve<IPluginConfig>();
            _server = container.Resolve<WSServer>();
            _plugin = container.Resolve<PluginMain>();
            _registry = container.Resolve<Registry>();

        }

        public void Stop() {
        }


        private void startBtn_Click(object sender, EventArgs e) {
            _config.WSServerRunning = true;
            _server.Start();
        }

        private void stopBtn_Click(object sender, EventArgs e) {
            _config.WSServerRunning = false;
            _server.Stop();
        }

        public class ComboboxItemOverlay {
            public string label { get; set; }
            public IOverlayPreset preset { get; set; }

            public override string ToString() {
                return label;
            }
        }


        public void RebuildOverlayOptions() {
            cbOverlay1.Items.Clear();

            foreach (var preset in _registry.OverlayPresets) {
                cbOverlay1.Items.Add(new ComboboxItemOverlay {
                    label = preset.Name,
                    preset = preset
                });
            }
        }

        private void cbOverlay1_SelectedIndexChanged(object sender, EventArgs e) {
            if (cbOverlay1.SelectedIndex == -1) return;

            var item = cbOverlay1.Items[cbOverlay1.SelectedIndex];
            var preset = (IOverlayPreset)item.GetType().GetProperty("preset").GetValue(item);
            if (preset == null) return;

            var hostUrl = "";
            if (_ngrokPrefix != null) {
                hostUrl += _ngrokPrefix;
            } else {
                if (_config.WSServerSSL) {
                    hostUrl += "wss://";
                } else {
                    hostUrl += "ws://";
                }

                if (_config.WSServerIP == "0.0.0.0") {
                    hostUrl += "127.0.0.1";
                } else {
                    hostUrl += _config.WSServerIP;
                }
                hostUrl += ":" + _config.WSServerPort;
            }

#if DEBUG
            var resourcesPath = "file:///" + _plugin.PluginDirectory.Replace('\\', '/') + "/libs/resources";
#else
            var resourcesPath = "file:///" + _plugin.PluginDirectory.Replace('\\', '/') + "/resources";
#endif

            var url = preset.HttpUrl.Replace("\\", "/").Replace("%%", resourcesPath);
            var uri = new UriBuilder(url);
            var query_params = HttpUtility.ParseQueryString(uri.Query);

            if (preset.Modern) {
                query_params.Add("OVERLAY_WS", hostUrl + "/ws");
            } else {
                query_params.Add("HOST_PORT", hostUrl + "/");
            }

            uri.Query = HttpUtility.UrlDecode(query_params.ToString());

            if ((uri.Port == 443 && uri.Scheme == "https") || (uri.Port == 80 && uri.Scheme == "http")) {
                uri.Port = -1;
            }

            txtOverlayUrl1.Text = (url != "") ? uri.ToString() : url;
        }

        private void txtOverlayUrl1_Click(object sender, EventArgs e) {
            txtOverlayUrl1.SelectAll();
            txtOverlayUrl1.Copy();
        }

        private class ShowLineArgs : EventArgs {
            public string Data { get; private set; }
            public ShowLineArgs(string d) {
                Data = d;
            }
        }
    }
}
