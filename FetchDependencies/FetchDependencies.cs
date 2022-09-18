using System.IO.Compression;
using System.Reflection;

namespace FetchDependencies {
    public class FetchDependencies {
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
            return !File.Exists(dllPath);
        }

        private static async Task DownloadPlugin(string path) {
            var httpClient = new HttpClient();
            await using var downloadStream = await httpClient.GetStreamAsync("https://advancedcombattracker.com/download.php?id=73");
            await using var zipFileStream = new FileStream(path, FileMode.Create);
            await downloadStream.CopyToAsync(zipFileStream);
            zipFileStream.Close();
        }
    }
}
