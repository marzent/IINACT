using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace RainbowMage.OverlayPlugin
{
    public class OverlayTemplateConfig
    {
        public int Version { get; set; }
        public List<OverlayTemplate> Overlays { get; set; }

    }

    public class OverlayTemplate : IOverlayTemplate
    {
        public string Name { get; set; }
        public string Uri { get; set; }

        [JsonProperty("plaintext_uri")]
        public string PlaintextUri { get; set; }
        [JsonProperty("suggested_width")]
        public int? SuggestedWidth { get; set; }
        [JsonProperty("suggested_height")]
        public int? SuggestedHeight { get; set; }
        public List<string> Features { get; set; } = new List<string>();

        public Uri ToOverlayUri(Uri webSocketServer)
        {
            Uri overlayUri;
            
            if (webSocketServer == null)
            {
                return null;
            }

            if (webSocketServer.Scheme == "wss")
            {
                overlayUri = new Uri(Uri);
            }
            else if (webSocketServer.Scheme == "ws")
            {
                overlayUri = new Uri(PlaintextUri ?? Uri);
            }
            else
            {
                return null;
            }

            if (Features.Contains("overlay_ws"))
            {
                overlayUri = setQueryStringParameter(overlayUri, "OVERLAY_WS", webSocketServer.ToString());
            }
            else if (Features.Contains("host_port"))
            {
                overlayUri = setQueryStringParameter(overlayUri, "HOST_PORT", webSocketServer.GetComponents(UriComponents.SchemeAndServer, UriFormat.SafeUnescaped));
            }

            return overlayUri;
        }

        private static Uri setQueryStringParameter(Uri source, string parameter, string value)
        {
            var qs = HttpUtility.ParseQueryString(source.Query);
            qs.Set(parameter, value);

            var uriBuilder = new UriBuilder(source);
            uriBuilder.Query = HttpUtility.UrlDecode(qs.ToString());
            return uriBuilder.Uri;
        }
    }
}
