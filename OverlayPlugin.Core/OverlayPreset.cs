using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin {
    [JsonObject(NamingStrategyType = typeof(Newtonsoft.Json.Serialization.SnakeCaseNamingStrategy))]
    internal class OverlayPreset : IOverlayPreset {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Url { get; set; }
        [JsonIgnore]
        public int[] Size { get; set; }
        public bool Locked { get; set; }
        public List<string> Supports { get; set; }

        [JsonExtensionData]
        [SuppressMessage("Style", "IDE0044:Add readonly modifier", Justification = "JsonExtensionData modifies this variable")]
        private IDictionary<string, JToken> _others;

        [OnDeserialized]
        public void ParseOthers(StreamingContext ctx) {
            var size = _others["size"];
            Size = new int[2];

            for (var i = 0; i < 2; i++) {
                switch (size[i].Type) {
                    case JTokenType.Integer:
                        Size[i] = size[i].ToObject<int>();
                        break;
                    case JTokenType.String:
                        var part = size[i].ToString();
                        if (part.EndsWith("%")) {
                            var percent = float.Parse(part[..^1]) / 100;
                            var screenSize = Screen.PrimaryScreen.WorkingArea;

                            Size[i] = (int)Math.Round(percent * (i == 0 ? screenSize.Width : screenSize.Height));
                        } else {
                            Size[i] = int.Parse(part);
                        }
                        break;
                    default:
                        Size[i] = 300;
                        break;
                }
            }
        }

        public override string ToString() {
            return Name;
        }
    }
}
