using System.Globalization;
using System.IO.Compression;

namespace FetchDependencies {
    internal static class Costura
    {
        public static bool CheckForPlugin(string name) => 
            name.StartsWith("costura.ffxiv_act_plugin.");

        public static string Fix(string name) =>
            "FFXIV_ACT_" + name.Substring(18, name.Length - 33).ToTitleCase() + ".dll";

        public static void Decompress(Stream stream, string destinationFileName) {
            using var destinationFileStream = File.Create(destinationFileName);
            using var decompressionStream = new DeflateStream(stream, CompressionMode.Decompress);
            decompressionStream.CopyTo(destinationFileStream);
        }

        private static string ToTitleCase(this string title) {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(title);
        }
    }

}
