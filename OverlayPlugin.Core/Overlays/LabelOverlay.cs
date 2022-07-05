using System;
using Newtonsoft.Json;

namespace RainbowMage.OverlayPlugin.Overlays
{
    [Serializable]
    public class LabelOverlay : OverlayBase<LabelOverlayConfig>
    {
        public LabelOverlay(LabelOverlayConfig config, string name, TinyIoCContainer container)
            : base(config, name, container)
        {
            timer.Stop();

            Config.TextChanged += (o, e) =>
            {
                UpdateOverlayText();
            };
            Config.HTMLModeChanged += (o, e) =>
            {
                UpdateOverlayText();
            };
        }



        private void UpdateOverlayText()
        {
            try
            {
                ((IOverlay)this).ExecuteScript(CreateEventDispatcherScript());
            }
            catch (Exception ex)
            {
                Log(LogLevel.Error, "Update: {1}", this.Name, ex);
            }
        }

        private string CreateEventDispatcherScript()
        {
            return string.Format(
                "document.dispatchEvent(new CustomEvent('onOverlayDataUpdate', {{ detail: {0} }}));",
                CreateJson());
        }

        internal string CreateJson()
        {
            return string.Format(
                "{{ text: {0}, isHTML: {1} }}",
                JsonConvert.SerializeObject(this.Config.Text),
                this.Config.HtmlModeEnabled ? "true" : "false");
        }

        protected override void Update()
        {
            
        }
    }
}
