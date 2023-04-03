using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;

namespace OtterGui;

public static class Functions
{
    // Iterate through a list executing actions on each element by its mode.
    public static void IteratePairwise<T>(IReadOnlyList<T> list, Action<T> action1, Action inBetween, Action<T>? action2 = null)
    {
        var odd  = (list.Count & 1) == 1;
        var size = list.Count - 1;
        action2 ??= action1;
        for (var i = 0; i < size; i += 2)
        {
            action1(list[i]);
            inBetween();
            action2(list[i + 1]);
        }

        if (odd)
            action1(list[size]);
    }

    // Iterate through a list executing bool returning functions on each element by its mode and return the or'd result.
    public static bool IteratePairwise<T>(IReadOnlyList<T> list, Func<T, bool> func1, Action inBetween, Func<T, bool>? func2 = null)
    {
        var odd  = (list.Count & 1) == 1;
        var size = list.Count - 1;
        func2 ??= func1;
        var ret = false;
        for (var i = 0; i < size; i += 2)
        {
            ret |= func1(list[i]);
            inBetween();
            ret |= func2(list[i + 1]);
        }

        return ret | (odd && func1(list[size]));
    }

    // Split a uint into four bytes, e.g. for RGBA colors.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static (byte Lowest, byte Second, byte Third, byte Highest) SplitBytes(uint value)
    {
        var byte4 = (byte)(value >> 24);
        var byte3 = (byte)(value >> 16);
        var byte2 = (byte)(value >> 8);
        var byte1 = (byte)value;
        return (byte1, byte2, byte3, byte4);
    }

    // Obtain a descriptive hex-string of a RGBA color.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string ColorBytes(uint color)
    {
        var (r, g, b, a) = SplitBytes(color);
        return $"#{r:X2}{g:X2}{b:X2}{a:X2}";
    }

    // Reorder a ABGR color to RGBA.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint ReorderColor(uint seColor)
    {
        var (a, b, g, r) = SplitBytes(seColor);
        return r | ((uint)g << 8) | ((uint)b << 16) | ((uint)a << 24);
    }

    // Average two given colors.
    public static uint AverageColor(uint c1, uint c2)
    {
        var (r1, g1, b1, a1) = SplitBytes(c1);
        var (r2, g2, b2, a2) = SplitBytes(c2);
        var r = (uint)(r1 + r2) / 2;
        var g = (uint)(g1 + g2) / 2;
        var b = (uint)(b1 + b2) / 2;
        var a = (uint)(a1 + a2) / 2;
        return r | (g << 8) | (b << 16) | (a << 24);
    }

    // Remove a single bit, moving all further bits one down.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint RemoveBit(uint config, int bit)
    {
        var lowMask  = (1u << bit) - 1u;
        var highMask = ~((1u << (bit + 1)) - 1u);
        var low      = config & lowMask;
        var high     = (config & highMask) >> 1;
        return low | high;
    }

    // Move a bit in an uint from its position to another, shifting other bits accordingly.
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static uint MoveBit(uint config, int bit1, int bit2)
    {
        var enabled = (config & (1 << bit1)) != 0 ? 1u << bit2 : 0u;
        config = RemoveBit(config, bit1);
        var lowMask = (1u << bit2) - 1u;
        var low     = config & lowMask;
        var high    = (config & ~lowMask) << 1;
        return low | enabled | high;
    }

    // Return a human readable form of the size using the given format (which should be a float identifier followed by a placeholder).
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    public static string HumanReadableSize(long size, string format = "{0:0.#} {1}")
    {
        var    order = 0;
        double s     = size;
        while (s >= 1024 && order < ByteAbbreviations.Length - 1)
        {
            order++;
            s /= 1024;
        }

        return string.Format(format, s, ByteAbbreviations[order]);
    }

    private static readonly string[] ByteAbbreviations =
    {
        "B",
        "KB",
        "MB",
        "GB",
        "TB",
        "PB",
        "EB",
    };

    // Compress any type to a base64 encoding of its compressed json representation, prepended with a version byte.
    // Returns an empty string on failure.
    public static unsafe string ToCompressedBase64<T>(T data, byte version)
    {
        try
        {
            var       json             = JsonConvert.SerializeObject(data, Formatting.None);
            var       bytes            = Encoding.UTF8.GetBytes(json);
            using var compressedStream = new MemoryStream();
            using (var zipStream = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                zipStream.Write(new ReadOnlySpan<byte>(&version, 1));
                zipStream.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(compressedStream.ToArray());
        }
        catch
        {
            return string.Empty;
        }
    }

    // Decompress a base64 encoded string to the given type and a prepended version byte if possible.
    // On failure, data will be default and version will be byte.MaxValue.
    public static byte FromCompressedBase64<T>(string base64, out T? data)
    {
        var version = byte.MaxValue;
        try
        {
            var       bytes            = Convert.FromBase64String(base64);
            using var compressedStream = new MemoryStream(bytes);
            using var zipStream        = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var resultStream     = new MemoryStream();
            zipStream.CopyTo(resultStream);
            bytes   = resultStream.ToArray();
            version = bytes[0];
            var json = Encoding.UTF8.GetString(bytes, 1, bytes.Length - 1);
            data = JsonConvert.DeserializeObject<T>(json);
        }
        catch
        {
            data = default;
        }

        return version;
    }

    // Try to obtain the list of Quick Access folders from your system.
    public static bool GetQuickAccessFolders(out List<(string Name, string Path)> folders)
    {
        folders = new List<(string Name, string Path)>();
        try
        {
            var shellAppType = Type.GetTypeFromProgID("Shell.Application");
            if (shellAppType == null)
                return false;

            var shell = Activator.CreateInstance(shellAppType);

            var obj = shellAppType.InvokeMember("NameSpace", BindingFlags.InvokeMethod, null, shell, new object[]
            {
                "shell:::{679f85cb-0220-4080-b29b-5540cc05aab6}",
            });
            if (obj == null)
                return false;


            foreach (var fi in ((dynamic)obj).Items())
            {
                if (!fi.IsLink && !fi.IsFolder)
                    continue;

                folders.Add((fi.Name, fi.Path));
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    // Try to obtain the Downloads folder from your system.
    public static bool GetDownloadsFolder(out string folder)
    {
        folder = string.Empty;
        try
        {
            var guid = new Guid("374DE290-123F-4565-9164-39C4925E467B");
            folder = SHGetKnownFolderPath(guid, 0);
            return folder.Length > 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("shell32", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
    private static extern string SHGetKnownFolderPath([MarshalAs(UnmanagedType.LPStruct)] Guid rfid, uint dwFlags, nint hToken = 0);
}
