using System;
using System.Drawing;

namespace RainbowMage.OverlayPlugin {
    /// <summary>
    /// <see cref="RainbowMage.OverlayPlugin.IOverlay"/> の設定に必要なプロパティを定義します。
    /// </summary>
    /// <remarks>
    /// アドオンを作成する場合はこのインターフェイスを実装するのではなく、
    /// <see cref="RainbowMage.OverlayPlugin.OverlayConfigBase"/> 抽象クラスを継承してください。
    /// </remarks>
    public interface IOverlayConfig {
        string Name { get; set; }
        bool IsVisible { get; set; }
        bool HideOutOfCombat { get; set; }
        Size Size { get; set; }
        string Url { get; set; }

        // IOverlayConfig 実装型 → IOverlay 実装型の逆引き用
        Type OverlayType { get; }
    }
}
