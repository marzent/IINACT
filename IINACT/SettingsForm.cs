using System.Diagnostics;
using Advanced_Combat_Tracker;
using DarkUI.Forms;
using FFXIV_ACT_Plugin.Common;
using FFXIV_ACT_Plugin.Config;
using RainbowMage.OverlayPlugin;

namespace IINACT {
    public partial class SettingsForm : DarkForm
    {
        private Configuration _configuration;
        
        public SettingsForm(int targetPid, Configuration configuration) {
            InitializeComponent();
            _configuration = configuration;
            comboBoxLang.DataSource = Enum.GetValues(typeof(Language));
            comboBoxLang.SelectedIndex = _configuration.Language - 1;
            comboBoxLang.SelectedIndexChanged += comboBoxLang_SelectedIndexChanged;
            comboBoxFilter.DataSource = Enum.GetValues(typeof(ParseFilterMode));
            comboBoxFilter.SelectedIndex = _configuration.ParseFilterMode;
            comboBoxFilter.SelectedIndexChanged += comboBoxFilter_SelectedIndexChanged;
            checkBoxShield.Checked = _configuration.DisableDamageShield;
            checkBoxPets.Checked = _configuration.DisableCombinePets;
            checkBoxDotCrit.Checked = _configuration.SimulateIndividualDoTCrits;
            checkBoxDotTick.Checked = _configuration.ShowRealDoTTicks;
            checkBoxDebug.Checked = _configuration.ShowDebug;
            logFileButton.Click += logFileButton_Clicked;
            if (Directory.Exists(_configuration.LogFilePath))
                ActGlobals.oFormActMain.LogFilePath = _configuration.LogFilePath;
            logFileButton.Text = ActGlobals.oFormActMain.LogFilePath;

            //create window handle
            //Opacity = 0;
            Show();
            //Hide();
            Opacity = 1;
#if DEBUG
            Show();
#endif
            Task.Run(CheckForUpdate);
            Task.Run(() => {
                var ffxivActPlugin = new FfxivActPluginWrapper(targetPid, _configuration);
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
                if (remoteVersion <= currentVersion) return;
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
            _configuration.Language = comboBoxLang.SelectedIndex + 1;
            _configuration.Save();
        }

        private void comboBoxFilter_SelectedIndexChanged(object? sender, EventArgs e) {
            _configuration.ParseFilterMode = comboBoxFilter.SelectedIndex;
            _configuration.Save();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (e.CloseReason != CloseReason.UserClosing) return;
            e.Cancel = true;
            Hide();
        }

        private void checkBoxShield_CheckedChanged(object sender, EventArgs e) {
            _configuration.DisableDamageShield = checkBoxShield.Checked;
            _configuration.Save();
        }

        private void checkBoxPets_CheckedChanged(object sender, EventArgs e) {
            _configuration.DisableCombinePets = checkBoxPets.Checked;
            _configuration.Save();
        }

        private void checkBoxDotCrit_CheckedChanged(object sender, EventArgs e) {
            _configuration.SimulateIndividualDoTCrits = checkBoxDotCrit.Checked;
            _configuration.Save();
        }

        private void checkBoxDotTick_CheckedChanged(object sender, EventArgs e) {
            _configuration.ShowRealDoTTicks = checkBoxDotTick.Checked;
            _configuration.Save();
        }

        private void checkBoxDebug_CheckedChanged(object sender, EventArgs e) {
            _configuration.ShowDebug = checkBoxDebug.Checked;
            _configuration.Save();
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
            _configuration.LogFilePath = newPath;
            _configuration.Save();
        }

    }
}