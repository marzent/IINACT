using System.IO.Compression;
using Dalamud.Plugin.Services;

namespace FetchDependencies;

public class FetchDependencies
{
    private const string VersionUrlGlobal = "https://www.iinact.com/updater/version";
    private const string VersionUrlChinese = "https://cninact.diemoe.net/CN解析/版本.txt";
    private const string PluginUrlGlobal = "https://www.iinact.com/updater/download";
    private const string PluginUrlChinese = "https://cninact.diemoe.net/CN解析/FFXIV_ACT_Plugin.dll";

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private bool IsChinese { get; }
    private HttpClient HttpClient { get; }
    private IPluginLog PluginLog { get; }

    public FetchDependencies(Version version, string assemblyDir, bool isChinese, HttpClient httpClient, IPluginLog pluginLog)
    {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        IsChinese = isChinese;
        HttpClient = httpClient;
        PluginLog = pluginLog;
    }

    public void GetFfxivPlugin()
    {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");
        var deucalionPath = Path.Combine(DependenciesDir, "deucalion-1.1.0.distrib.dll");
        
        if (!NeedsUpdate(pluginPath))
            return;

        if (IsChinese)
            DownloadFile(PluginUrlChinese, pluginPath);
        else
        {
            if (!File.Exists(pluginZipPath))
                DownloadFile(PluginUrlGlobal, pluginZipPath);
            try
            {
                ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
            }
            catch (InvalidDataException)
            {
                File.Delete(pluginZipPath);
                DownloadFile(PluginUrlGlobal, pluginZipPath);
                ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
            }
            File.Delete(pluginZipPath);

            foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
                File.Delete(deucalionDll);
        }

        var patcher = new Patcher(PluginVersion, DependenciesDir, PluginLog);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
        patcher.MachinaFFXIV();
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
                                      .GetStringAsync(IsChinese ? VersionUrlChinese : VersionUrlGlobal,
                                                      cancelAfterDelay.Token).Result;
            var remoteVersion = new Version(remoteVersionString);
            return remoteVersion > plugin.Version;
        }
        catch
        {
            return false;
        }
    }

    private void DownloadFile(string url, string path)
    {
        using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        using var downloadStream = HttpClient
                                   .GetStreamAsync(url,
                                                   cancelAfterDelay.Token).Result;
        using var zipFileStream = new FileStream(path, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
    }
}
