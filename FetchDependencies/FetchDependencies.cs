using System.IO.Compression;
using System.Text.Json.Nodes;

namespace FetchDependencies;

public class FetchDependencies
{
    private const string VersionUrlGlobal = "https://www.iinact.com/updater/version";
    private const string PluginUrlGlobal = "https://www.iinact.com/updater/download";
    private const string FallbackReleaseUrlGlobal = "https://api.github.com/repos/ravahn/FFXIV_ACT_Plugin/releases/latest";

    // The CN client needs a CN build of FFXIV_ACT_Plugin. Its releases share the same plugin assembly version
    // across client patches (e.g. 3.0.3.0-CN7.57 and 3.0.3.0-CN7.58), so updates are tracked by release tag.
    private const string ReleaseUrlChinese = "https://api.github.com/repos/NewMoe-Technology/FFXIV_ACT_Plugin_CN/releases/latest";
    private const string PluginAssetNameChinese = "FFXIV_ACT_Plugin.zip";
    private const string ReleaseTagFileChinese = "FFXIV_ACT_Plugin.release";

    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private bool IsChinese { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(Version version, string assemblyDir, bool isChinese, HttpClient httpClient)
    {
        PluginVersion = version;
        DependenciesDir = assemblyDir;
        IsChinese = isChinese;
        HttpClient = httpClient;
    }

    private string ReleaseTagPathChinese => Path.Combine(DependenciesDir, ReleaseTagFileChinese);

    public void GetFfxivPlugin()
    {
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");
        
        if (!NeedsUpdate(pluginPath, out var release))
            return;
        
        if (!File.Exists(pluginZipPath))
        {
            release = DownloadPlugin(pluginZipPath, release);
        }

        try
        {
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }
        catch (InvalidDataException)
        {
            File.Delete(pluginZipPath);
            release = DownloadPlugin(pluginZipPath, release);
            ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        }
        File.Delete(pluginZipPath);

        foreach (var deucalionDll in Directory.GetFiles(DependenciesDir, "deucalion*.dll"))
            File.Delete(deucalionDll);

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();

        if (IsChinese)
        {
            if (release != null)
                File.WriteAllText(ReleaseTagPathChinese, release.Tag);
            else
                File.Delete(ReleaseTagPathChinese);
        }
    }

    private bool NeedsUpdate(string dllPath, out ReleaseInfo? release)
    {
        release = null;
        if (!File.Exists(dllPath)) return true;
        try
        {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;
            
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));

            if (IsChinese)
            {
                release = GetLatestRelease(ReleaseUrlChinese, PluginAssetNameChinese, cancelAfterDelay.Token);
                var installedTag = File.Exists(ReleaseTagPathChinese)
                                       ? File.ReadAllText(ReleaseTagPathChinese).Trim()
                                       : null;
                return installedTag != release.Tag;
            }

            var remoteVersionString = HttpClient
                                      .GetStringAsync(VersionUrlGlobal, cancelAfterDelay.Token).Result;
            var remoteVersion = new Version(remoteVersionString);
            return remoteVersion > plugin.Version;
        }
        catch
        {
            return false;
        }
    }

    private ReleaseInfo? DownloadPlugin(string pluginZipPath, ReleaseInfo? release)
    {
        if (IsChinese)
        {
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            release ??= GetLatestRelease(ReleaseUrlChinese, PluginAssetNameChinese, cancelAfterDelay.Token);
            DownloadFile(release.DownloadUrl, pluginZipPath);
            return release;
        }

        try
        {
            DownloadFile(PluginUrlGlobal, pluginZipPath);
        }
        catch
        {
            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var fallback = GetLatestRelease(FallbackReleaseUrlGlobal, null, cancelAfterDelay.Token);
            DownloadFile(fallback.DownloadUrl, pluginZipPath);
        }

        return null;
    }

    private ReleaseInfo GetLatestRelease(string releaseUrl, string? assetName, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, releaseUrl);
        request.Headers.UserAgent.ParseAdd("IINACT/1.0");
        using var response = HttpClient.Send(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = response.Content.ReadAsStream(cancellationToken);
        var json = JsonNode.Parse(stream);
        var tag = json?["tag_name"]?.ToString();
        var asset = json?["assets"]?.AsArray()
                        .FirstOrDefault(a => assetName == null || a?["name"]?.ToString() == assetName);
        var downloadUrl = asset?["browser_download_url"]?.ToString();

        if (string.IsNullOrEmpty(tag) || string.IsNullOrEmpty(downloadUrl))
            throw new Exception($"Could not find a plugin download in {releaseUrl}.");

        return new ReleaseInfo(tag, downloadUrl);
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

    private sealed record ReleaseInfo(string Tag, string DownloadUrl);
}
