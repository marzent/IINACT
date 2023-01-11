using System.ComponentModel;
using System.Reflection;
using Advanced_Combat_Tracker;
using IINACT.Properties;

namespace IINACT {
    internal static class Program
    {
        private static string _dependenciesDir = null!;
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            using var singletoneMutex = new Mutex(true, @"Global\IINACT", out bool createdNew);

            if (createdNew)
            {
                var fetchDeps = new FetchDependencies.FetchDependencies();
                fetchDeps.GetFfxivPlugin().Wait();
                _dependenciesDir = fetchDeps.DependenciesDir;
                AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
                ApplicationConfiguration.Initialize();
                ActGlobals.oFormActMain = new FormActMain();
                Application.Run(new Daemon());
            }
            else
            {
                MessageBox.Show("IINACT is already running.");
            }
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args) {
            if (args.Name.Contains(".resources"))
                return null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            var filename = args.Name.Split(',')[0] + ".dll".ToLower();
            var asmFile = Path.Combine(_dependenciesDir, filename);

            try {
                return Assembly.LoadFrom(asmFile);
            }
            catch (Exception) {
                return null;
            }
        }
    }

    public class Daemon : ApplicationContext {
        private readonly NotifyIcon _trayIcon;
        private readonly SettingsForm _settingsForm;

        public Daemon() {
            UpgradeSettings();
            var resources = new ComponentResourceManager(typeof(SettingsForm));
            _trayIcon = new NotifyIcon
            {
                Icon = (Icon)resources.GetObject("$this.Icon")!,
                ContextMenuStrip = new ContextMenuStrip(),
                Visible = true
            };

            _trayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[] {
                new ToolStripMenuItem("Settings", null, ShowSettings, "Settings"),
                new ToolStripMenuItem("Exit", null, Exit, "Exit")
            });
            Application.ApplicationExit += CleanupTray;
            _settingsForm = new SettingsForm();
        }

        private static void UpgradeSettings() {
            if (!Settings.Default.IsSettingsUpgradeRequired) return;
            Settings.Default.Upgrade();
            Settings.Default.IsSettingsUpgradeRequired = false;
            Settings.Default.Save();
        }

        private void ShowSettings(object? sender, EventArgs e) {
            _settingsForm.Show();
        }

        private void CleanupTray(object? sender, EventArgs e) {
            _trayIcon.Visible = false;
            _trayIcon.Icon.Dispose();
            _trayIcon.Dispose();
        }

        private static void Exit(object? sender, EventArgs e) {
            Application.Exit();
        }
    }
}