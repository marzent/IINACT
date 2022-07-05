using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin {
    public interface IOverlayPreset {
        string Name { get; }
        string Type { get; }
        string Url { get; }
        int[] Size { get; }
        bool Locked { get; }
        List<string> Supports { get; }
    }
}

