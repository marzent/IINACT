using System.IO.Compression;

namespace FetchDependencies {
    public class FetchDependencies {
        private readonly HttpClient HttpClient;

        public string DependenciesDir { get; }

        public FetchDependencies(string assemblyDir, HttpClient httpClient) {
            DependenciesDir = assemblyDir;
            HttpClient = httpClient;
        }

        public void GetFfxivPlugin() {
            var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
            var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

            if (!NeedsUpdate(pluginPath))
                return;

            if (!File.Exists(pluginZipPath))
                DownloadPlugin(pluginZipPath);

            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, overwriteFiles: true);
            File.Delete(pluginZipPath);

            var patcher = new Patcher(DependenciesDir);
            patcher.MainPlugin();
            patcher.LogFilePlugin();
            patcher.MemoryPlugin();
        }

        private bool NeedsUpdate(string dllPath) {
            if (!File.Exists(dllPath)) return true;
            try {
                using var plugin = new TargetAssembly(dllPath);
                using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var remoteVersionString = HttpClient.GetStringAsync("https://www.iinact.com/updater/version", cancelAfterDelay.Token).Result;
                var remoteVersion = new Version(remoteVersionString);
                return remoteVersion > plugin.Version;
            }
            catch {
                return false;
            }
        }

        private void DownloadPlugin(string path) {
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var downloadStream = HttpClient.GetStreamAsync("https://www.iinact.com/updater/download", cancelAfterDelay.Token).Result;
            using var zipFileStream = new FileStream(path, FileMode.Create);
            downloadStream.CopyTo(zipFileStream);
            zipFileStream.Close();
        }
    }
}
