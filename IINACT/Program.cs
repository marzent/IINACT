
using System.ComponentModel;
using Advanced_Combat_Tracker;
using FFXIV_ACT_PluginPatcher;

namespace IINACT {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            PluginPatcher.DoPatching();
            ApplicationConfiguration.Initialize();
            ActGlobals.oFormActMain = new FormActMain();
            Application.Run(new Daemon());
        }
    }

    public class Daemon : ApplicationContext {
        private readonly NotifyIcon _trayIcon;
        private readonly SettingsForm _settingsForm;

        public Daemon() {
            var resources = new ComponentResourceManager(typeof(SettingsForm));
            _trayIcon = new NotifyIcon
            {
                Icon = (Icon)resources.GetObject("$this.Icon")!,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };

            _trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("Settings", null, Settings, "Settings"),
                new ToolStripMenuItem("Exit", null, Exit, "Exit")
            });
            AppDomain.CurrentDomain.ProcessExit += CleanupTray;
            _settingsForm = new SettingsForm();
        }

        private void Settings(object? sender, EventArgs e) {
            _settingsForm.Show();
        }

        private void CleanupTray(object? sender, EventArgs e) {
            _trayIcon.Visible = false;
        }

        private static void Exit(object? sender, EventArgs e) {
            Application.Exit();
        }
    }
}