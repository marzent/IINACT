using System.IO.Compression;

namespace FetchDependencies {
    public static class FetchDependencies {
        public static async Task GetFfxivPlugin() {
            var dependenciesDir = Directory.GetCurrentDirectory();
            var pluginZipPath = Path.Combine(dependenciesDir, "FFXIV_ACT_Plugin.zip");
            var pluginPath = Path.Combine(dependenciesDir, "FFXIV_ACT_Plugin.dll");

            if (!await NeedsUpdate(pluginPath))
                return;

            if (!File.Exists(pluginZipPath))
                await DownloadPlugin(pluginZipPath);

            ZipFile.ExtractToDirectory(pluginZipPath, dependenciesDir, overwriteFiles: true);
            File.Delete(pluginZipPath);

            var patcher = new Patcher(dependenciesDir);
            patcher.MainPlugin();
            patcher.LogFilePlugin();
            patcher.MemoryPlugin();
        }

        private static async Task<bool> NeedsUpdate(string dllPath) {
            return true;
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
