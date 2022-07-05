using System.Diagnostics;
using Advanced_Combat_Tracker;
using DarkUI.Forms;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using IINACT.Properties;
using RainbowMage.OverlayPlugin;

namespace IINACT {
    public partial class SettingsForm : DarkForm {
        public SettingsForm() {
            InitializeComponent();
            comboBoxLang.DataSource = Enum.GetValues(typeof(Language));
            comboBoxLang.SelectedIndex = Settings.Default.Language;
            comboBoxLang.SelectedIndexChanged += comboBoxLang_SelectedIndexChanged;
            comboBoxFilter.DataSource = Enum.GetValues(typeof(ParseFilterMode));
            comboBoxFilter.SelectedIndex = Settings.Default.ParseFilterMode;
            comboBoxFilter.SelectedIndexChanged += comboBoxFilter_SelectedIndexChanged;
            rpcapCheckBox.Checked = Settings.Default.RPcap;
            rpcapSectionPanel.Height = Settings.Default.RPcap ? 200 : 0;


            //create window handle
            Opacity = 0;
            Show();
            Hide();
            Opacity = 1;
#if DEBUG
            Show();
#endif
            Task.Run(() => {
                var ffxivActPlugin = new FfxivActPluginWrapper();
                Invoke(new MethodInvoker(InitOverlayPlugin));
                while (ffxivActPlugin.ProcessManager.Verify())
                    Thread.Sleep(2000);
                Invoke(new MethodInvoker(Application.Exit));
            });
        }

        private void InitOverlayPlugin() {
            var container = new TinyIoCContainer();
            var logger = new Logger();
            container.Register(logger);
            container.Register<ILogger>(logger);

            var pluginMain = new PluginMain(Path.Combine(ActGlobals.oFormActMain.AppDataFolder.FullName, "OverlayPlugin"), logger, container);
            container.Register(pluginMain);
            ActGlobals.oFormActMain.OverlayPluginContainer = container;

            pluginMain.InitPlugin(opPanel, opLabel);
        }

        protected override void OnHandleCreated(EventArgs e) {
            Trace.Listeners.Add(new TextBoxTraceListener(debugBox));
        }

        private void comboBoxLang_SelectedIndexChanged(object? sender, EventArgs e) {
            Settings.Default.Language = comboBoxLang.SelectedIndex;
            Settings.Default.Save();
        }

        private void comboBoxFilter_SelectedIndexChanged(object? sender, EventArgs e) {
            Settings.Default.ParseFilterMode = comboBoxFilter.SelectedIndex;
            Settings.Default.Save();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            Hide();
        }

        private void RpcapCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.RPcap = rpcapCheckBox.Checked;
            rpcapSectionPanel.Height = Settings.Default.RPcap ? 200 : 0;
        }
    }
}