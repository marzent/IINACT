using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using RainbowMage.OverlayPlugin.NetworkProcessors;

namespace RainbowMage.OverlayPlugin
{
    internal class OverlayHider
    {
        private bool gameActive = true;
        private bool inCutscene = false;
        private bool inCombat = false;
        private IPluginConfig config;
        private ILogger logger;
        private PluginMain main;
        private FFXIVRepository repository;
        private int ffxivPid = -1;
        private Timer focusTimer;

        public OverlayHider(TinyIoCContainer container)
        {
            this.config = container.Resolve<IPluginConfig>();
            this.logger = container.Resolve<ILogger>();
            this.main = container.Resolve<PluginMain>();
            this.repository = container.Resolve<FFXIVRepository>();

            container.Resolve<NativeMethods>().ActiveWindowChanged += ActiveWindowChangedHandler;
            container.Resolve<NetworkParser>().OnOnlineStatusChanged += OnlineStatusChanged;
            container.Resolve<EventSources.EnmityEventSource>().CombatStatusChanged += CombatStatusChanged;

            try
            {
                repository.RegisterProcessChangedHandler(UpdateFFXIVProcess);
            } catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "Failed to register process watcher for FFXIV; this is only an issue if you're playing FFXIV. As a consequence, OverlayPlugin won't be able to hide overlays if you're not in-game.");
                logger.Log(LogLevel.Error, "Details: " + ex.ToString());
            }

            focusTimer = new Timer();
            focusTimer.Tick += (o, e) => ActiveWindowChangedHandler(this, IntPtr.Zero);
            focusTimer.Interval = 10000;  // 10 seconds
            focusTimer.Start();
        }

        private void UpdateFFXIVProcess(Process p)
        {
            if (p != null)
            {
                ffxivPid = p.Id;
            } else
            {
                ffxivPid = -1;
            }
        }

        public void UpdateOverlays()
        {
            if (!config.HideOverlaysWhenNotActive)
                gameActive = true;

            if (!config.HideOverlayDuringCutscene)
                inCutscene = false;

            try
            {
                foreach (var overlay in main.Overlays)
                {
                    if (overlay.Config.IsVisible)
                    {
                        overlay.Visible = gameActive && !inCutscene && (!overlay.Config.HideOutOfCombat || inCombat);
                    }
                }
            } catch (Exception ex)
            {
                logger.Log(LogLevel.Error, $"OverlayHider: Failed to update overlays: {ex}");
            }
        }

        private void ActiveWindowChangedHandler(object sender, IntPtr changedWindow)
        {
            if (!config.HideOverlaysWhenNotActive) return;
            try
            {
                try
                {
                    NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(), out var pid);

                    if (pid == 0)
                        return;

                    if (ffxivPid != -1)
                    {
                        gameActive = pid == ffxivPid || pid == Process.GetCurrentProcess().Id;
                    } else
                    {
                        var exePath = Process.GetProcessById((int)pid).MainModule.FileName;
                        var fileName = Path.GetFileName(exePath.ToString());
                        gameActive = (fileName == "ffxiv.exe" || fileName == "ffxiv_dx11.exe" ||
                                        exePath.ToString() == Process.GetCurrentProcess().MainModule.FileName);
                    }
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // Ignore access denied errors. Those usually happen if the foreground window is running with
                    // admin permissions but we are not.
                    if (ex.ErrorCode == -2147467259)  // 0x80004005
                    {
                        gameActive = false;
                    }
                    else
                    {
                        logger.Log(LogLevel.Error, "XivWindowWatcher: {0}", ex.ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Log(LogLevel.Error, "XivWindowWatcher: {0}", ex.ToString());
            }

            UpdateOverlays();
        }

        private void OnlineStatusChanged(object sender, OnlineStatusChangedArgs e)
        {
            if (!config.HideOverlayDuringCutscene || e.Target != repository.GetPlayerID()) return;

            inCutscene = e.Status == 15;
            UpdateOverlays();
        }

        private void CombatStatusChanged(object sender, EventSources.CombatStatusChangedArgs e)
        {
            inCombat = e.InCombat;
            UpdateOverlays();
        }
    }
}
