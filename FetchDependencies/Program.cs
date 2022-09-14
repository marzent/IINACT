using System.IO.Compression;
using FetchDependencies;

var solutionDir = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.Parent!.Parent!.FullName;
var dependenciesDir = Path.Combine(solutionDir, "external_dependencies");
var pluginZipPath = Path.Combine(dependenciesDir, "FFXIV_ACT_Plugin.zip");

var httpClient = new HttpClient();
await using var downloadStream = await httpClient.GetStreamAsync("https://advancedcombattracker.com/download.php?id=73");
await using var zipFileStream = new FileStream(pluginZipPath, FileMode.Create);
await downloadStream.CopyToAsync(zipFileStream);
zipFileStream.Close();

ZipFile.ExtractToDirectory(pluginZipPath, dependenciesDir, overwriteFiles: true);
File.Delete(pluginZipPath);

var patcher = new Patcher(dependenciesDir);
patcher.mainPlugin();
patcher.logFilePlugin();

