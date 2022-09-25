using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin {
    public abstract class OverlayBase<TConfig> : IOverlay, IEventReceiver, IApiBase
        where TConfig : OverlayConfigBase {
        private bool disableLog = false;
        private List<Action> hotKeyCallbacks = new List<Action>();
        protected readonly TinyIoCContainer container;
        protected readonly ILogger logger;
        private readonly EventDispatcher dispatcher;

        protected System.Timers.Timer timer;

        /// <summary>
        /// オーバーレイがログを出力したときに発生します。
        /// </summary>
        private event EventHandler<LogEventArgs> OnLog;

        /// <summary>
        /// オーバーレイがログを出力したときに発生します。
        /// </summary>
        event EventHandler<LogEventArgs> IOverlay.OnLog {
            add => this.OnLog += value;
            remove => this.OnLog -= value;
        }

        /// <summary>
        /// ユーザーが設定したオーバーレイの名前を取得します。
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// ユーザーが設定したオーバーレイの名前を取得します。
        /// </summary>
        string IEventReceiver.Name => Name;

        /// <summary>
        /// ユーザーが設定したオーバーレイの名前を取得します。
        /// </summary>
        string IOverlay.Name => Name;

        /// <summary>
        /// オーバーレイの設定を取得します。
        /// </summary>
        public TConfig Config { get; private set; }

        /// <summary>
        /// プラグインの設定を取得します。
        /// </summary>
        public IPluginConfig PluginConfig { get; private set; }
        IOverlayConfig IOverlay.Config { get => Config; set => Config = (TConfig)value; }
        IntPtr IOverlay.Handle => IntPtr.Zero;

        bool IOverlay.Visible {
            get => false;
            set {
            }
        }

        protected OverlayBase(TConfig config, string name, TinyIoCContainer container) {
            this.container = container;
            this.logger = container.Resolve<ILogger>();
            this.dispatcher = container.Resolve<EventDispatcher>();
            this.PluginConfig = container.Resolve<IPluginConfig>();
            this.Config = config;
            this.Name = name;

            if (this.Config == null) {
                var construct = typeof(TConfig).GetConstructor(new Type[] { typeof(TinyIoCContainer), typeof(string) });
                if (construct == null) {
                    construct = typeof(TConfig).GetConstructor(new Type[] { typeof(string) });
                    if (construct == null) {
                        throw new Exception("No usable constructor for config type found (" + typeof(TConfig).ToString() + ")!");
                    }

                    this.Config = (TConfig)construct.Invoke(new object[] { name });
                } else {
                    this.Config = (TConfig)construct.Invoke(new object[] { container, name });
                }
            }

            InitializeOverlay();
            InitializeTimer();
            InitializeConfigHandlers();
            UpdateHotKey();
        }

        /// <summary>
        /// オーバーレイの更新を開始します。
        /// </summary>
        public virtual void Start() {
            if (Config == null) throw new InvalidOperationException("Configuration is missing!");

            timer.Start();
        }

        /// <summary>
        /// オーバーレイの更新を停止します。
        /// </summary>
        public virtual void Stop() {
            timer.Stop();
        }

        /// <summary>
        /// オーバーレイを初期化します。
        /// </summary>
        protected virtual void InitializeOverlay() {
            try {

            }
            catch (Exception ex) {
                Log(LogLevel.Error, "InitializeOverlay: {0} {1}", this.Name, ex);
            }
        }

        private ModifierKeys GetModifierKey(Keys modifier) {
            var modifiers = new ModifierKeys();
            if ((modifier & Keys.Shift) == Keys.Shift) {
                modifiers |= ModifierKeys.Shift;
            }
            if ((modifier & Keys.Control) == Keys.Control) {
                modifiers |= ModifierKeys.Control;
            }
            if ((modifier & Keys.Alt) == Keys.Alt) {
                modifiers |= ModifierKeys.Alt;
            }
            if ((modifier & Keys.LWin) == Keys.LWin || (modifier & Keys.RWin) == Keys.RWin) {
                modifiers |= ModifierKeys.Win;
            }
            return modifiers;
        }

        private void UpdateHotKey() {
            var hook = container.Resolve<KeyboardHook>();

            // Clear the old hotkeys
            foreach (var cb in hotKeyCallbacks) {
                hook.UnregisterHotKey(cb);
            }
            hotKeyCallbacks.Clear();

            foreach (var entry in Config.GlobalHotkeys) {
                if (entry.Enabled && entry.Key != Keys.None) {
                    var modifierKeys = GetModifierKey(entry.Modifiers);
                    Action cb = null;

                    switch (entry.Type) {
                        case GlobalHotkeyType.ToggleVisible:
                            cb = () => this.Config.IsVisible = !this.Config.IsVisible;
                            break;
                        case GlobalHotkeyType.ToggleClickthru:
                            cb = () => this.Config.IsClickThru = !this.Config.IsClickThru;
                            break;
                        case GlobalHotkeyType.ToggleLock:
                            cb = () => this.Config.IsLocked = !this.Config.IsLocked;
                            break;
                        case GlobalHotkeyType.ToogleEnabled:
                            cb = () => this.Config.Disabled = !this.Config.Disabled;
                            break;
                        default:
                            cb = () => this.Config.IsVisible = !this.Config.IsVisible;
                            break;
                    }

                    hotKeyCallbacks.Add(cb);
                    try {
                        hook.RegisterHotKey(modifierKeys, entry.Key, cb);
                    }
                    catch (Exception e) {
                        Log(LogLevel.Error, Resources.OverlayBaseRegisterHotkeyError, e.Message);
                        hotKeyCallbacks.Remove(cb);
                    }
                }
            }
        }

        /// <summary>
        /// タイマーを初期化します。
        /// </summary>
        protected virtual void InitializeTimer() {
            timer = new System.Timers.Timer();
            timer.Interval = 1000;
            timer.Elapsed += (o, e) => {
                try {
                    Update();
                }
                catch (Exception ex) {
                    Log(LogLevel.Error, "Update: {0}", ex.ToString());
                }
            };
        }

        /// <summary>
        /// 設定クラスのイベントハンドラを設定します。
        /// </summary>
        protected virtual void InitializeConfigHandlers() {
        }

        /// <summary>
        /// オーバーレイを更新します。
        /// </summary>
        protected abstract void Update();

        /// <summary>
        /// オーバーレイのインスタンスを破棄します。
        /// </summary>
        public virtual void Dispose() {
            try {
            }
            catch (Exception ex) {
                Log(LogLevel.Error, "Dispose: {0}", ex);
            }
        }

        public virtual void Navigate(string url) {
        }

        public virtual void Reload() {
        }

        protected void Log(LogLevel level, string message) {
            if (logger != null && !disableLog) {
                if (message.Contains("Xilium.CefGlue")) {
                    Log(LogLevel.Error, string.Format(Resources.IncompatibleAddon, this));
                    Stop();
                    disableLog = true;
                }

                logger.Log(level, "{0}: {1}", this.Name, message);
            }
        }

        protected void Log(LogLevel level, string format, params object[] args) {
            Log(level, string.Format(format, args));
        }


        void IOverlay.SavePositionAndSize() {
        }

        void IOverlay.ExecuteScript(string script) {
        }

        private void NotifyOverlayState() {
            ((IOverlay)this).ExecuteScript(string.Format(
                "document.dispatchEvent(new CustomEvent('onOverlayStateUpdate', {{ detail: {{ isLocked: {0} }} }}));",
                this.Config.IsLocked ? "true" : "false"));
        }

        void IOverlay.SendMessage(string message) {
            ((IOverlay)this).ExecuteScript(string.Format(
                "document.dispatchEvent(new CustomEvent('onBroadcastMessageReceive', {{ detail: {{ message: \"{0}\" }} }}));",
                Util.CreateJsonSafeString(message)));
        }

        public virtual void OverlayMessage(string message) {
        }



        // Event Source stuff

        public virtual void HandleEvent(JObject e) {
            ((IOverlay)this).ExecuteScript("if(window.__OverlayCallback) __OverlayCallback(" + e.ToString(Formatting.None) + ");");
        }

        public void Subscribe(string eventType) {
            dispatcher.Subscribe(eventType, this);
        }

        public void Unsubscribe(string eventType) {
            dispatcher.Unsubscribe(eventType, this);
        }

        public void UnsubscribeAll() {
            dispatcher.UnsubscribeAll(this);
        }

        public virtual void InitModernAPI() {

        }

    }
}
