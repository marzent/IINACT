using System.IO.Compression;

namespace FetchDependencies {
    public class FetchDependencies {
        private const string UserAgent =
            "IINACT";

        public string DependenciesDir { get; }

        public FetchDependencies() {
            var assemblyDir = AppDomain.CurrentDomain.BaseDirectory;
            DependenciesDir = Path.Combine(assemblyDir, "external_dependencies");
        }

        public async Task GetFfxivPlugin() {
            Directory.CreateDirectory(DependenciesDir);
            var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
            var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

            if (!await NeedsUpdate(pluginPath))
                return;

            if (!File.Exists(pluginZipPath))
                await DownloadPlugin(pluginZipPath);

            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, overwriteFiles: true);
            File.Delete(pluginZipPath);

            var patcher = new Patcher(DependenciesDir);
            patcher.MainPlugin();
            patcher.LogFilePlugin();
            patcher.MemoryPlugin();
        }

        private static async Task<bool> NeedsUpdate(string dllPath) {
            if (!File.Exists(dllPath)) return true;
            try {
                using var plugin = new TargetAssembly(dllPath);
                var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(2);
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                var remoteVersionString =
                    await httpClient.GetStringAsync("https://www.iinact.com/updater/version");
                var remoteVersion = new Version(remoteVersionString);
                return remoteVersion > plugin.Version;
            }
            catch {
                return false;
            }
        }

        private static async Task DownloadPlugin(string path) {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
            await using var downloadStream = await httpClient.GetStreamAsync("https://www.iinact.com/updater/download");
            await using var zipFileStream = new FileStream(path, FileMode.Create);
            await downloadStream.CopyToAsync(zipFileStream);
            zipFileStream.Close();
        }
    }
}
