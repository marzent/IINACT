using System.Diagnostics;
using System.IO.Compression;
using Mono.Cecil;

namespace FetchDependencies;

public class FetchDependencies
{
    private Version IinactApiVersion => new(1,0, 0);
    private string DependenciesDir { get; }
    private HttpClient HttpClient { get; }

    public FetchDependencies(string assemblyDir, HttpClient httpClient)
    {
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

        ZipFile.ExtractToDirectory(pluginZipPath, DependenciesDir, true);
        File.Delete(pluginZipPath);

        var patcher = new Patcher(DependenciesDir);
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

            if (!IinactApiVersionMatches(plugin))
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

    private bool IinactApiVersionMatches(TargetAssembly targetAssembly)
    {
        var namespaceIdentifier = $"IINACT_V{IinactApiVersion.ToString().Replace(".", "_")}";
        
        foreach (var type in targetAssembly.Assembly.MainModule.Types)
            if (type.Namespace == namespaceIdentifier && type.Name == "WasHere")
                return true;

        Trace.WriteLine($"[PatchWasHere] Adding type {namespaceIdentifier}.WasHere");
        var wasHere = new TypeDefinition(namespaceIdentifier, "WasHere", TypeAttributes.Public | TypeAttributes.Class) {
            BaseType = targetAssembly.Assembly.MainModule.TypeSystem.Object
        };
        targetAssembly.Assembly.MainModule.Types.Add(wasHere);
        return false;
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
