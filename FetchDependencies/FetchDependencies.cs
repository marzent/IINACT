using System.IO.Compression;

namespace FetchDependencies;

public class FetchDependencies
{
    private string DependenciesDir { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(string assemblyDir, HttpClient httpClient)
    {
        //DependenciesDir = Path.Combine(assemblyDir, "external_dependencies");
        DependenciesDir = assemblyDir;
        HttpClient = httpClient;
    }

    public void GetFfxivPlugin()
    {
        Directory.CreateDirectory(DependenciesDir);
        var pluginZipPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.zip");
        var pluginPath = Path.Combine(DependenciesDir, "FFXIV_ACT_Plugin.dll");

        if (!NeedsUpdate(DependenciesDir))
            return;

        if (!File.Exists(pluginPath))
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

        var patcher = new Patcher(DependenciesDir);
        patcher.MainPlugin();
        patcher.LogFilePlugin();
        patcher.MemoryPlugin();
    }

    private bool NeedsUpdate(string dllPath)
    {
        var txtPath = Path.Combine(dllPath, "版本.txt");
        if (!File.Exists(txtPath)) return true;
        try
        {
            if (File.Exists(txtPath))
            {
                using var txt = new StreamReader(txtPath);
                var nowVerson = new Version(txt.ReadToEnd());
                using var cancelAfterDelay = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                var textStream = HttpClient.GetStringAsync("https://cninact.diemoe.net/CN解析/版本.txt").Result;
                var remoteVersion = new Version(textStream);
                return remoteVersion > nowVerson;
            }
            else
            {
                DownloadPlugin(dllPath);
                return true;
            };

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
        var txtinPath = Path.Combine(path, "版本.txt");
        using var downloadStream = HttpClient.GetStreamAsync("https://cninact.diemoe.net/CN解析/FFXIV_ACT_Plugin.dll\r\n").Result;
        using var textStream = HttpClient.GetStreamAsync("https://cninact.diemoe.net/CN解析/版本.txt").Result;
        //await using var downloadStream = await httpClient.GetStreamAsync($"https://github.com/TundraWork/FFXIV_ACT_Plugin_CN/releases/download/{bcd}/FFXIV_ACT_Plugin.dll");
        using var zipFileStream = new FileStream(pluginPath, FileMode.Create);
        downloadStream.CopyTo(zipFileStream);
        zipFileStream.Close();
        using var zipFileStream1 = new FileStream(txtinPath, FileMode.Create);
        textStream.CopyTo(zipFileStream1);
        zipFileStream1.Close();
    }
}
