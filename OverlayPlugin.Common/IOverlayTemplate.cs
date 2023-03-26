using System;
using System.Collections.Generic;

namespace RainbowMage.OverlayPlugin
{
    public interface IOverlayTemplate
    {
        public string Name { get; }
        public string Uri { get; }
        public string PlaintextUri { get; }
        public int? SuggestedWidth { get; }
        public int? SuggestedHeight { get; }
        public List<string> Features { get; }

        public abstract Uri ToOverlayUri(Uri webSocketServer);
    }
}
