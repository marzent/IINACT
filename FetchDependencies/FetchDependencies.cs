using System.IO.Compression;

namespace FetchDependencies {
    public class FetchDependencies {
        private const string ActUserAgent =
            "ACT-Parser (v3.6.1     Release: 277 | .NET v4.8+ (533325) | OS Microsoft Windows NT 10.0.22000.0)";

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
                httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ActUserAgent);
                var source = new CancellationTokenSource();
                source.CancelAfter(1000);
                var remoteVersionString =
                    await httpClient.GetStringAsync("https://advancedcombattracker.com/versioncheck/pluginversion/73", source.Token);
                var remoteVersion = new Version(remoteVersionString);
                return remoteVersion > plugin.Version;
            }
            catch {
                return false;
            }
        }

        private static async Task DownloadPlugin(string path) {
            var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(ActUserAgent);
            await using var downloadStream = await httpClient.GetStreamAsync("https://advancedcombattracker.com/download.php?id=73");
            await using var zipFileStream = new FileStream(path, FileMode.Create);
            await downloadStream.CopyToAsync(zipFileStream);
            zipFileStream.Close();
        }
    }
}
