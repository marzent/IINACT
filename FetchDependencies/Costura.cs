using System.Globalization;
using System.IO.Compression;

namespace FetchDependencies;

internal static class Costura
{
    public static bool CheckForPlugin(string name)
    {
        return name.Contains("act") || name.Contains("machina");
    }

    public static string Fix(string name)
    {
        if (name.Contains("act"))
            return "FFXIV_ACT_" + name.Substring(18, name.Length - 33).ToTitleCase() + ".dll";
        if (name.Contains("machina.ffxiv"))
            return "Machina.FFXIV.dll";
        if (name.Contains("machina"))
            return "Machina.dll";
        return name.Substring(8, name.Length - 23).ToTitleCase() + ".dll";
    }

    public static void Decompress(Stream stream, string destinationFileName)
    {
        using var destinationFileStream = File.Create(destinationFileName);
        using var decompressionStream = new DeflateStream(stream, CompressionMode.Decompress);
        decompressionStream.CopyTo(destinationFileStream);
    }

    private static string ToTitleCase(this string title)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);
    }
}
