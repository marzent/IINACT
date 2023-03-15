using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin
{
    public interface IOverlayPreset
    {
        string Name { get; }
        string Url { get; }
        string HttpUrl { get; }
        public string Options { get; }
        bool Modern { get; }
    }
}
