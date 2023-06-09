
using Dalamud;
using System.IO.Compression;

namespace FetchDependencies;

public class FetchDependencies
{
    private Version PluginVersion { get; }
    private string DependenciesDir { get; }
    private HttpClient HttpClient { get; }
    public bool InChina { get; set; }
    public FetchDependencies(string assemblyDir, HttpClient httpClient, ClientLanguage dalamudClientLanguage)
    {
        //DependenciesDir = Path.Combine(assemblyDir, "external_dependencies");
        DependenciesDir = assemblyDir;
        HttpClient = httpClient;
        switch (dalamudClientLanguage)
        {
            case Dalamud.ClientLanguage.Japanese:
            case Dalamud.ClientLanguage.English:
            case Dalamud.ClientLanguage.German:
            case Dalamud.ClientLanguage.French:
                InChina = false; ;
                break;
            default:
                InChina=true;
                break;
        }
    }

    public void GetFfxivPlugin()
    {
        Directory.CreateDirectory(DependenciesDir);
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

        if (!NeedsUpdate(pluginPath))
            return;

        if (File.Exists(pluginPath))
        {
            File.Delete(pluginPath);
        }
        DownloadPlugin(DependenciesDir);

        //try
        //{
        //    ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        //}
        //catch (InvalidDataException)
        //{
        //    File.Delete(pluginZipPath);
        //    DownloadPlugin(DependenciesDir);
        //    ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        //}

        //File.Delete(pluginZipPath);

        var patcher = new Patcher(PluginVersion, DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    //private bool NeedsUpdate(string dllPath)
    //{
    //    var txtPath = Path.Combine(dllPath, "版本.txt");
    //    if (!File.Exists(txtPath)) return true;
    //    try
    //    {
    //        if (File.Exists(txtPath))
    //        {
    //            using var txt = new StreamReader(txtPath);
    //            var nowVerson = new Version(txt.ReadToEnd());
    //            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
    //            var textStream = HttpClient.GetStringAsync("https://cninact.diemoe.net/CN解析/版本.txt").Result;
    //            var remoteVersion = new Version(textStream);
    //            return remoteVersion > nowVerson;
    //        }
    //        else
    //        {
    //            DownloadPlugin(dllPath);
    //            return true;
    //        };

    //    }
    //    catch
    //    {
    //        return false;
    //    }
    //}
    private bool NeedsUpdate(string dllPath)
    {
        if (!File.Exists(dllPath)) return true;
        try
        {
            using var plugin = new TargetAssembly(dllPath);

            if (!plugin.ApiVersionMatches())
                return true;

            using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            string remoteVersionString="";
            if (InChina)
            {
                https://dev.ff14.cloud/index.php?user/publicLink&fid=cd5eTDD_9a3yJy9zxUpJQgZehqyT49CAJtzko9f2jjr2leefQ-AAP9duHXg92cAJ5zBlpOpdiE5W6IGlHrrIXDbEHnG5gyDt-w&file_name=/%E7%89%88%E6%9C%AC.txt
                remoteVersionString = HttpClient
                          .GetStringAsync("https://cninact.diemoe.net/CN解析/版本.txt"
                                          ).Result;
            }
            else
            {
                remoteVersionString = HttpClient
                                         .GetStringAsync("https://cninact.diemoe.net/global/版本.txt",
                                                         cancelAfterDelay.Token).Result;
            }

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
        //using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        //using var downloadStream = HttpClient
        //                           .GetStreamAsync("https://www.iinact.com/updater/download",
        //                                           cancelAfterDelay.Token).Result;
        //using var zipFileStream = new FileStream(path, FileMode.Create);
        //downloadStream.CopyTo(zipFileStream);
        //zipFileStream.Close();
        var pluginPath = Path.Combine(path, "FFXIV_ACT_Plugin.dll");
        if (InChina)
        {
            using var downloadStream = HttpClient.GetStreamAsync("https://cninact.diemoe.net/CN解析/FFXIV_ACT_Plugin.dll").Result;
            using var zipFileStream = new FileStream(pluginPath, FileMode.Create);
            downloadStream.CopyTo(zipFileStream);
            zipFileStream.Close();
        }
        else
        {
            using var downloadStream = HttpClient.GetStreamAsync("https://cninact.diemoe.net/global/FFXIV_ACT_Plugin.dll").Result;
            using var zipFileStream = new FileStream(pluginPath, FileMode.Create);
            downloadStream.CopyTo(zipFileStream);
            zipFileStream.Close();
        }
       
    }
}
