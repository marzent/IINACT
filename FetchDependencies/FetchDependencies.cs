using System.IO.Compression;

namespace FetchDependencies;

public class FetchDependencies
{
    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(Version version, string assemblyDir, HttpClient httpClient)
    {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        HttpClient = httpClient;
    }

    public void GetFfxivPlugin()
    {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

        if (!NeedsUpdate(pluginPath))
            return;

        if (!File.Exists(pluginZipPath))
            DownloadPlugin(pluginZipPath);

        try
        {
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }
        catch (InvalidDataException) 
        {
            File.Delete(pluginZipPath);
            DownloadPlugin(pluginZipPath);
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }

        File.Delete(pluginZipPath);

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private bool NeedsUpdate(string dllPath)
    {
        if (!File.Exists(dllPath)) return true;
        try
        {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;
            
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            var remoteVersionString = HttpClient
                                      .GetStringAsync("https://www.iinact.com/updater/version",
                                                      cancelAfterDelay.Token).Result;
            var remoteVersion = new Version(remoteVersionString);
            return remoteVersion > plugin.Version;
        }
        catch
        {
            return false;
        }
    }

    private void DownloadPlugin(string path)
    {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
                                   .GetStreamAsync("https://www.iinact.com/updater/download",
                                                   cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
    }
}
