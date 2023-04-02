#nullable enable
using System;
using Newtonsoft.Json.Linq;

namespace RainbowMage.OverlayPlugin.Handlers;

internal interface IHandler : IDisposable
{
    public void DataReceived(JObject data);
}
    
