using System;
using System.IO;
using System.Xml.Serialization;

namespace RainbowMage.OverlayPlugin.Overlays
{
    [Serializable]
    public class LabelOverlayConfig : OverlayConfigBase
    {
        private string text;

        [XmlElement("Text")]
        public string Text
        {
            get => this.text;
            set
            {
                if (this.text != value)
                {
                    this.text = value;
                    if (TextChanged != null)
                    {
                        TextChanged(this, new TextChangedEventArgs(this.text));
                    }
                }
            }
        }

        private bool htmlModeEnabled;

        [XmlElement("HTMLModeEnabled")]
        public bool HtmlModeEnabled
        {
            get => this.htmlModeEnabled;
            set
            {
                if (this.htmlModeEnabled != value)
                {
                    this.htmlModeEnabled = value;
                    if (HTMLModeChanged != null)
                    {
                        HTMLModeChanged(this, new StateChangedEventArgs<bool>(this.htmlModeEnabled));
                    }
                }
            }
        }

        public event EventHandler<TextChangedEventArgs> TextChanged;
        public event EventHandler<StateChangedEventArgs<bool>> HTMLModeChanged;

        public LabelOverlayConfig(TinyIoCContainer container, string name)
            : base(name)
        {
            var pluginPath = container.Resolve<PluginMain>().PluginDirectory;

            this.Text = "";
            this.HtmlModeEnabled = false;
#if DEBUG
            this.Url = "file:///" + Path.Combine(pluginPath, "libs", "resources", "label.html").Replace("\\", "/");
#else
            this.Url = "file:///" + Path.Combine(pluginPath, "resources", "label.html").Replace("\\", "/");
#endif
        }

        // XmlSerializer用
        private LabelOverlayConfig()
            : base(null) { }

        public override Type OverlayType => typeof(LabelOverlay);
    }

    public class TextChangedEventArgs : EventArgs
    {
        public string Text { get; private set; }

        public TextChangedEventArgs(string text)
        {
            this.Text = text;
        }
    }
}
