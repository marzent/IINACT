using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RainbowMage.OverlayPlugin {
    public partial class GeneralConfigTab : UserControl {
        private readonly TinyIoCContainer container;
        private readonly string pluginDirectory;
        private readonly PluginConfig config;
        private readonly ILogger logger;

        private DateTime lastClick;

        public GeneralConfigTab(TinyIoCContainer container) {
            InitializeComponent();
            Dock = DockStyle.Fill;

            this.container = container;
            pluginDirectory = container.Resolve<PluginMain>().PluginDirectory;
            config = container.Resolve<PluginConfig>();
            logger = container.Resolve<ILogger>();

            cbErrorReports.Checked = config.ErrorReports;
            cbUpdateCheck.Checked = config.UpdateCheck;
            cbHideOverlaysWhenNotActive.Checked = config.HideOverlaysWhenNotActive;
            cbHideOverlaysDuringCutscene.Checked = config.HideOverlayDuringCutscene;
        }

        public void SetReadmeVisible(bool visible) {
            lblReadMe.Visible = visible;
            lblNewUserWelcome.Visible = visible;
        }

        private void btnUpdateCheck_MouseClick(object sender, MouseEventArgs e) {
            // Shitty double-click detection. I'd love to have a proper double click event on buttons in WinForms. =/
            double timePassed = 1000;
            var now = DateTime.Now;

            if (lastClick != null) {
                timePassed = now.Subtract(lastClick).TotalMilliseconds;
            }

            lastClick = now;

            Task.Run(() => {
                Thread.Sleep(500);

                if (lastClick != now) return;
            });
        }

        private void lnkGithubRepo_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e) {
            System.Diagnostics.Process.Start(lnkGithubRepo.Text);
        }

        private void newUserWelcome_Click(object sender, EventArgs e) {

        }

        private void btnCactbotUpdate_Click(object sender, EventArgs e) {
            try {
                var asm = Assembly.Load("CactbotEventSource");
                var checkerType = asm.GetType("Cactbot.VersionChecker");
                var loggerType = asm.GetType("Cactbot.ILogger");
                var configType = asm.GetType("Cactbot.CactbotEventSourceConfig");

                var esList = container.Resolve<Registry>().EventSources;
                IEventSource cactbotEs = null;

                foreach (var es in esList) {
                    if (es.Name == "Cactbot Config" || es.Name == "Cactbot") {
                        cactbotEs = es;
                        break;
                    }
                }

                if (cactbotEs == null) {
                    MessageBox.Show("Cactbot is loaded but it never registered with OverlayPlugin!", "Error");
                    return;
                }

                var cactbotConfig = cactbotEs.GetType().GetProperty("Config").GetValue(cactbotEs);
                configType.GetField("LastUpdateCheck").SetValue(cactbotConfig, DateTime.MinValue);

                var checker = checkerType.GetConstructor(new Type[] { loggerType }).Invoke(new object[] { cactbotEs });
                checkerType.GetMethod("DoUpdateCheck", new Type[] { configType }).Invoke(checker, new object[] { cactbotConfig });
            }
            catch (FileNotFoundException) {
                MessageBox.Show("Could not find Cactbot!", "Error");
            }
            catch (Exception ex) {
                MessageBox.Show("Failed: " + ex.ToString(), "Error");
            }
        }
    }
}
