using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin {
    internal class TriggIntegration {
        private PluginMain _plugin;
        public delegate void CustomCallbackDelegate(object o, string param);

        private object GetPluginData() {
            return null;
        }

        public TriggIntegration(TinyIoCContainer container) {
            var logger = container.Resolve<ILogger>();
            _plugin = container.Resolve<PluginMain>();
        }

        public void SendOverlayMessage(object _, string msg) {
            var pos = msg.IndexOf('|');
            if (pos < 1) return;

            var overlayName = msg[..pos];
            msg = msg[(pos + 1)..];

            foreach (var overlay in _plugin.Overlays) {
                if (overlay.Name == overlayName) {
                    ((IEventReceiver)overlay).HandleEvent(JObject.FromObject(new {
                        type = "Triggernometry",
                        message = msg
                    }));
                    break;
                }
            }
        }

        public void HideOverlay(object _, string msg) {
            foreach (var overlay in _plugin.Overlays) {
                if (overlay.Name == msg) {
                    overlay.Config.IsVisible = false;
                    break;
                }
            }
        }

        public void ShowOverlay(object _, string msg) {
            foreach (var overlay in _plugin.Overlays) {
                if (overlay.Name == msg) {
                    overlay.Config.IsVisible = true;
                    break;
                }
            }
        }
    }
}
