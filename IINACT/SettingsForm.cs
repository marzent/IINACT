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
            comboBoxLang.SelectedIndex = Settings.Default.Language - 1;
            comboBoxLang.SelectedIndexChanged += comboBoxLang_SelectedIndexChanged;
            comboBoxFilter.DataSource = Enum.GetValues(typeof(ParseFilterMode));
            comboBoxFilter.SelectedIndex = Settings.Default.ParseFilterMode;
            comboBoxFilter.SelectedIndexChanged += comboBoxFilter_SelectedIndexChanged;
            checkBoxShield.Checked = Settings.Default.DisableDamageShield;
            checkBoxPets.Checked = Settings.Default.DisableCombinePets;
            checkBoxDotCrit.Checked = Settings.Default.SimulateIndividualDoTCrits;
            checkBoxDotTick.Checked = Settings.Default.ShowRealDoTTicks;
            checkBoxDebug.Checked = Settings.Default.ShowDebug;
            checkBoxRpcap.Checked = Settings.Default.RPcap;
            textBoxHost.Text = Settings.Default.RPcapHost;
            textBoxPort.Text = $@"{Settings.Default.RPcapPort}";
            textBoxUsername.Text = Settings.Default.RPcapUsername;
            textBoxPassword.Text = Settings.Default.RPcapPassword;
            rpcapSectionPanel.Height = Settings.Default.RPcap ? 200 : 0;
            logFileButton.Click += logFileButton_Clicked;
            if (Directory.Exists(Settings.Default.LogFilePath))
                ActGlobals.oFormActMain.LogFilePath = Settings.Default.LogFilePath;
            logFileButton.Text = ActGlobals.oFormActMain.LogFilePath;

            //create window handle
            Opacity = 0;
            Show();
            Hide();
            Opacity = 1;
#if DEBUG
            Show();
#endif
            Task.Run(CheckForUpdate);
            Task.Run(() => {
                var ffxivActPlugin = new FfxivActPluginWrapper();
                Invoke(new MethodInvoker(InitOverlayPlugin));
                while (ffxivActPlugin.ProcessManager.Verify())
                    Thread.Sleep(2000);
                Invoke(new MethodInvoker(Application.Exit));
            });
        }

        private static void CheckForUpdate() {
            try {
                var currentVersion = new Version(Application.ProductVersion);
                var remoteVersionString =
                    new HttpClient().GetStringAsync("https://github.com/marzent/IINACT/raw/main/version").Result;
                var remoteVersion = new Version(remoteVersionString);
                if (remoteVersion < currentVersion) return;
                if (MessageBox.Show("An newer version of IINACT is available. Would you like to download it?",
                        "Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.Yes) {
                    Process.Start("explorer", "https://github.com/marzent/IINACT/releases/latest");
                }
            }
            catch {
                MessageBox.Show("Failed to check for updates.");
            }
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
            Settings.Default.Language = comboBoxLang.SelectedIndex + 1;
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

        private void checkBoxShield_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.DisableDamageShield = checkBoxShield.Checked;
            Settings.Default.Save();
        }

        private void checkBoxPets_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.DisableCombinePets = checkBoxPets.Checked;
            Settings.Default.Save();
        }

        private void checkBoxDotCrit_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.SimulateIndividualDoTCrits = checkBoxDotCrit.Checked;
            Settings.Default.Save();
        }

        private void checkBoxDotTick_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.ShowRealDoTTicks = checkBoxDotTick.Checked;
            Settings.Default.Save();
        }

        private void checkBoxDebug_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.ShowDebug = checkBoxDebug.Checked;
            Settings.Default.Save();
        }

        private void RpcapCheckBox_CheckedChanged(object sender, EventArgs e) {
            Settings.Default.RPcap = checkBoxRpcap.Checked;
            Settings.Default.Save();
            rpcapSectionPanel.Height = Settings.Default.RPcap ? 200 : 0;
        }

        private void TextBoxHost_TextChanged(object sender, EventArgs e) {
            Settings.Default.RPcapHost = textBoxHost.Text;
            Settings.Default.Save();
        }

        private void TextBoxPort_TextChanged(object sender, EventArgs e) {
            if (!int.TryParse(textBoxPort.Text, out var port)) return;
            Settings.Default.RPcapPort = port;
            Settings.Default.Save();
        }

        private void TextBoxUsername_TextChanged(object sender, EventArgs e) {
            Settings.Default.RPcapUsername = textBoxUsername.Text;
            Settings.Default.Save();
        }

        private void TextBoxPassword_TextChanged(object sender, EventArgs e) {
            Settings.Default.RPcapPassword = textBoxPassword.Text;
            Settings.Default.Save();
        }

        private void logFileButton_Clicked(object? sender, EventArgs e) {
            // Show the FolderBrowserDialog.
            var result = logFolderBrowserDialog.ShowDialog();
            if (result != DialogResult.OK)
                return;
            var newPath = logFolderBrowserDialog.SelectedPath ?? "";
            if (!Directory.Exists(newPath))
                return;
            ActGlobals.oFormActMain.LogFilePath = newPath;
            Settings.Default.LogFilePath = newPath;
            Settings.Default.Save();
        }

    }
}