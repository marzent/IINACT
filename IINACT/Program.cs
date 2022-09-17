using System.ComponentModel;
using System.Reflection;
using Advanced_Combat_Tracker;

namespace IINACT {
    internal static class Program {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main() {
            FetchDependencies.FetchDependencies.GetFfxivPlugin().Wait();
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            ApplicationConfiguration.Initialize();
            ActGlobals.oFormActMain = new FormActMain();
            Application.Run(new Daemon());
        }

        private static Assembly? CurrentDomain_AssemblyResolve(object? sender, ResolveEventArgs args) {
            if (args.Name.Contains(".resources"))
                return null;

            var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.FullName == args.Name);
            if (assembly != null)
                return assembly;

            var filename = args.Name.Split(',')[0] + ".dll".ToLower();
            var asmFile = Path.Combine(@".\", "external_dependencies", filename);

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
            Application.ApplicationExit += CleanupTray;
            _settingsForm = new SettingsForm();
        }

        private void Settings(object? sender, EventArgs e) {
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