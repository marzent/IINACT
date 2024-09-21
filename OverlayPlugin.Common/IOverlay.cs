using System;

namespace RainbowMage.OverlayPlugin
{
    /// <summary>
    /// オーバーレイに必要な機能を定義します。
    /// </summary>
    /// <remarks>
    /// アドオンを作成する場合はこのインターフェイスを実装するのではなく、
    /// </remarks>
    public interface IOverlay : IDisposable
    {
        /// <summary>
        /// ユーザーが設定したオーバーレイの名前を取得します。
        /// </summary>
        string Name { get; }

        IOverlayConfig Config { get; set; }

        bool Visible { get; set; }

        IntPtr Handle { get; }

        /// <summary>
        /// オーバーレイの更新を開始します。
        /// </summary>
        void Start();

        /// <summary>
        /// オーバーレイの更新を停止します。
        /// </summary>
        void Stop();

        void Reload();

        /// <summary>
        /// 指定した URL を表示します。
        /// </summary>
        /// <param name="url">表示する URL。</param>
        void Navigate(string url);

        /// <summary>
        /// オーバーレイの位置と大きさを保存します。
        /// </summary>
        void SavePositionAndSize();

        void ExecuteScript(string script);

        /// <summary>
        /// オーバーレイにメッセージを送信します。
        /// </summary>
        /// <param name="message">メッセージの内容。</param>
        void SendMessage(string message);

        /// <summary>
        /// A message from javascript for the overlay plugin to consume.
        /// </summary>
        /// <param name="message">A string message created by the plugin javascript.</param>
        void OverlayMessage(string message);
    }
}
