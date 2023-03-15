using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin
{
    internal class OverlayPreset : IOverlayPreset
    {
        public string Name { get; set; }
        public string Url { get; set; }

        [JsonProperty("http_proxy")]
        public string HttpUrl { get; set; }

        public string Options { get; set; }
        public bool Modern { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }
}
