using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Storage.FileSystem;
using Windows.Win32.System.Memory;

namespace IINACT.Network;

/// <summary>
/// A SigScanner facilitates searching for memory signatures in a given ProcessModule.
/// </summary>
public class MultiSigScanner : IDisposable
{
    private const uint GenericRead = 0x80000000;
    
    private nint moduleCopyPtr;
    private long moduleCopyOffset;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiSigScanner"/> class.
    /// </summary>
    public MultiSigScanner()
    {
        Module = Process.GetCurrentProcess().MainModule!;
        Is32BitProcess = !Environment.Is64BitProcess;
        IsCopy = true;

        SetupSearchSpace();
        SetupCopy();
    }

    /// <summary>
    /// Gets a value indicating whether or not the search on this module is performed on a copy.
    /// </summary>
    public bool IsCopy { get; }

    /// <summary>
    /// Gets a value indicating whether or not the ProcessModule is 32-bit.
    /// </summary>
    public bool Is32BitProcess { get; }

    /// <summary>
    /// Gets the base address of the search area. When copied, this will be the address of the copy.
    /// </summary>
    public nint SearchBase => this.IsCopy ? this.moduleCopyPtr : this.Module.BaseAddress;

    /// <summary>
    /// Gets the base address of the .text section search area.
    /// </summary>
    public nint TextSectionBase => new(this.SearchBase.ToInt64() + this.TextSectionOffset);

    /// <summary>
    /// Gets the offset of the .text section from the base of the module.
    /// </summary>
    public long TextSectionOffset { get; private set; }

    /// <summary>
    /// Gets the size of the text section.
    /// </summary>
    public int TextSectionSize { get; private set; }

    /// <summary>
    /// Gets the base address of the .data section search area.
    /// </summary>
    public nint DataSectionBase => new(this.SearchBase.ToInt64() + this.DataSectionOffset);

    /// <summary>
    /// Gets the offset of the .data section from the base of the module.
    /// </summary>
    public long DataSectionOffset { get; private set; }

    /// <summary>
    /// Gets the size of the .data section.
    /// </summary>
    public int DataSectionSize { get; private set; }

    /// <summary>
    /// Gets the base address of the .rdata section search area.
    /// </summary>
    public nint RDataSectionBase => new(this.SearchBase.ToInt64() + this.RDataSectionOffset);

    /// <summary>
    /// Gets the offset of the .rdata section from the base of the module.
    /// </summary>
    public long RDataSectionOffset { get; private set; }

    /// <summary>
    /// Gets the size of the .rdata section.
    /// </summary>
    public int RDataSectionSize { get; private set; }

    /// <summary>
    /// Gets the ProcessModule on which the search is performed.
    /// </summary>
    public ProcessModule Module { get; }

    private nint TextSectionTop => this.TextSectionBase + this.TextSectionSize;

    /// <summary>
    /// Scan memory for a signature.
    /// </summary>
    /// <param name="baseAddress">The base address to scan from.</param>
    /// <param name="size">The amount of bytes to scan.</param>
    /// <param name="signature">The signature to search for.</param>
    /// <returns>The found offset.</returns>
    public static nint Scan(nint baseAddress, int size, string signature)
    {
        return Scan(baseAddress, size, signature, 1)[0];
    }
    
    /// <summary>
    /// Scan memory for a signature.
    /// </summary>
    /// <param name="baseAddress">The base address to scan from.</param>
    /// <param name="size">The amount of bytes to scan.</param>
    /// <param name="signature">The signature to search for.</param>
    /// <param name="maxAddresses">The maximum number of addresses being searched for.</param>
    /// <returns>The found offset.</returns>
    public static nint[] Scan(nint baseAddress, int size, string signature, int maxAddresses)
    {
        var addrs = new List<nint>();
        var (needle, mask) = ParseSignature(signature);

        var runningBase = baseAddress;
        for (int i = 0; i < maxAddresses; i++)
        {
            var index = IndexOf(runningBase, size, needle, mask);
            Plugin.Log.Debug($"[MultiSigScanner] IndexOf returned {index:X}");
            if (index < 0)
                throw new KeyNotFoundException($"Can't find a signature of {signature}");
            var offset = index + needle.Length;
            addrs.Add(runningBase + index);
            runningBase += offset;
            size -= offset;
        }
        
        return addrs.ToArray();
    }

    /// <summary>
    /// Try scanning memory for a signature.
    /// </summary>
    /// <param name="baseAddress">The base address to scan from.</param>
    /// <param name="size">The amount of bytes to scan.</param>
    /// <param name="signature">The signature to search for.</param>
    /// <param name="result">The offset, if found.</param>
    /// <returns>true if the signature was found.</returns>
    public static bool TryScan(nint baseAddress, int size, string signature, out nint result)
    {
        try
        {
            result = Scan(baseAddress, size, signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = nint.Zero;
            return false;
        }
    }

    /// <summary>
    /// Scan for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>An nint to the static memory location.</returns>
    public nint GetStaticAddressFromSig(string signature, int offset = 0)
    {
        var instrAddr = this.ScanText(signature);
        instrAddr = nint.Add(instrAddr, offset);
        var bAddr = (long)this.Module.BaseAddress;
        long num;

        do
        {
            instrAddr = nint.Add(instrAddr, 1);
            num = Marshal.ReadInt32(instrAddr) + (long)instrAddr + 4 - bAddr;
        }
        while (!(num >= this.DataSectionOffset && num <= this.DataSectionOffset + this.DataSectionSize)
               && !(num >= this.RDataSectionOffset && num <= this.RDataSectionOffset + this.RDataSectionSize));

        return nint.Add(instrAddr, Marshal.ReadInt32(instrAddr) + 4);
    }

    /// <summary>
    /// Try scanning for a .data address using a .text function.
    /// This is intended to be used with IDA sigs.
    /// Place your cursor on the line calling a static address, and create and IDA sig.
    /// </summary>
    /// <param name="signature">The signature of the function using the data.</param>
    /// <param name="result">An nint to the static memory location, if found.</param>
    /// <param name="offset">The offset from function start of the instruction using the data.</param>
    /// <returns>true if the signature was found.</returns>
    public bool TryGetStaticAddressFromSig(string signature, out nint result, int offset = 0)
    {
        try
        {
            result = this.GetStaticAddressFromSig(signature, offset);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = nint.Zero;
            return false;
        }
    }

    /// <summary>
    /// Scan for a byte signature in the .data section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    public nint ScanData(string signature)
    {
        var scanRet = Scan(this.DataSectionBase, this.DataSectionSize, signature);

        if (this.IsCopy)
            scanRet = new nint(scanRet.ToInt64() - this.moduleCopyOffset);

        return scanRet;
    }

    /// <summary>
    /// Try scanning for a byte signature in the .data section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    public bool TryScanData(string signature, out nint result)
    {
        try
        {
            result = this.ScanData(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = nint.Zero;
            return false;
        }
    }

    /// <summary>
    /// Scan for a byte signature in the whole module search area.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    public nint ScanModule(string signature)
    {
        var scanRet = Scan(this.SearchBase, this.Module.ModuleMemorySize, signature);

        if (this.IsCopy)
            scanRet = new nint(scanRet.ToInt64() - this.moduleCopyOffset);

        return scanRet;
    }

    /// <summary>
    /// Try scanning for a byte signature in the whole module search area.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    public bool TryScanModule(string signature, out nint result)
    {
        try
        {
            result = this.ScanModule(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = nint.Zero;
            return false;
        }
    }

    /// <summary>
    /// Resolve a RVA address.
    /// </summary>
    /// <param name="nextInstAddr">The address of the next instruction.</param>
    /// <param name="relOffset">The relative offset.</param>
    /// <returns>The calculated offset.</returns>
    public nint ResolveRelativeAddress(nint nextInstAddr, int relOffset)
    {
        if (this.Is32BitProcess) throw new NotSupportedException("32 bit is not supported.");
        return nextInstAddr + relOffset;
    }

    /// <summary>
    /// Scan for a byte signature in the .text section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <returns>The real offset of the found signature.</returns>
    public nint ScanText(string signature)
    {
        return ScanText(signature, 1)[0];
    }
    
    /// <summary>
    /// Scan for a byte signature in the .text section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="maxAddresses">The maximum number of addresses being searched for.</param>
    /// <returns>The real offset of the found signature.</returns>
    public nint[] ScanText(string signature, int maxAddresses)
    {
        var mBase = IsCopy ? moduleCopyPtr : TextSectionBase;
        var scanRet = Scan(mBase, TextSectionSize, signature, maxAddresses);

        for (int i = 0; i < scanRet.Length; i++)
        {
            if (IsCopy)
                scanRet[i] = new nint(scanRet[i].ToInt64() - moduleCopyOffset);

            var insnByte = Marshal.ReadByte(scanRet[i]);

            if (insnByte == 0xE8 || insnByte == 0xE9)
                scanRet[i] = ReadJmpCallSig(scanRet[i]);    
        }
        
        return scanRet;
    }

    /// <summary>
    /// Try scanning for a byte signature in the .text section.
    /// </summary>
    /// <param name="signature">The signature.</param>
    /// <param name="result">The real offset of the signature, if found.</param>
    /// <returns>true if the signature was found.</returns>
    public bool TryScanText(string signature, out nint result)
    {
        try
        {
            result = this.ScanText(signature);
            return true;
        }
        catch (KeyNotFoundException)
        {
            result = nint.Zero;
            return false;
        }
    }

    /// <summary>
    /// Free the memory of the copied module search area on object disposal, if applicable.
    /// </summary>
    public void Dispose()
    {
        Marshal.FreeHGlobal(moduleCopyPtr);
    }

    /// <summary>
    /// Helper for ScanText to get the correct address for IDA sigs that mark the first JMP or CALL location.
    /// </summary>
    /// <param name="sigLocation">The address the JMP or CALL sig resolved to.</param>
    /// <returns>The real offset of the signature.</returns>
    private static nint ReadJmpCallSig(nint sigLocation)
    {
        var jumpOffset = Marshal.ReadInt32(sigLocation, 1);
        return nint.Add(sigLocation, 5 + jumpOffset);
    }

    private static (byte[] Needle, bool[] Mask) ParseSignature(string signature)
    {
        signature = signature.Replace(" ", string.Empty);
        if (signature.Length % 2 != 0)
            throw new ArgumentException(@"Signature without whitespaces must be divisible by two.", nameof(signature));

        var needleLength = signature.Length / 2;
        var needle = new byte[needleLength];
        var mask = new bool[needleLength];
        for (var i = 0; i < needleLength; i++)
        {
            var hexString = signature.Substring(i * 2, 2);
            if (hexString == "??" || hexString == "**")
            {
                needle[i] = 0;
                mask[i] = true;
                continue;
            }

            needle[i] = byte.Parse(hexString, NumberStyles.AllowHexSpecifier);
            mask[i] = false;
        }

        return (needle, mask);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static unsafe int IndexOf(nint bufferPtr, int bufferLength, byte[] needle, bool[] mask)
    {
        if (needle.Length > bufferLength) return -1;
        var badShift = BuildBadCharTable(needle, mask);
        var last = needle.Length - 1;
        var offset = 0;
        var maxoffset = bufferLength - needle.Length;
        var buffer = (byte*)bufferPtr;

        while (offset <= maxoffset)
        {
            int position;
            for (position = last; needle[position] == *(buffer + position + offset) || mask[position]; position--)
            {
                if (position == 0)
                    return offset;
            }

            offset += badShift[*(buffer + offset + last)];
        }

        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int[] BuildBadCharTable(byte[] needle, bool[] mask)
    {
        int idx;
        var last = needle.Length - 1;
        var badShift = new int[256];
        for (idx = last; idx > 0 && !mask[idx]; --idx)
        {
        }

        var diff = last - idx;
        if (diff == 0) diff = 1;

        for (idx = 0; idx <= 255; ++idx)
            badShift[idx] = diff;
        for (idx = last - diff; idx < last; ++idx)
            badShift[needle[idx]] = last - idx;
        return badShift;
    }
    
    private void SetupSearchSpace()
    {
        var baseAddress = Module.BaseAddress;

        // We don't want to read all of IMAGE_DOS_HEADER or IMAGE_NT_HEADER stuff so we cheat here.
        var ntNewOffset = Marshal.ReadInt32(baseAddress, 0x3C);
        var ntHeader = baseAddress + ntNewOffset;

        // IMAGE_NT_HEADER
        var fileHeader = ntHeader + 4;
        var numSections = Marshal.ReadInt16(ntHeader, 6);

        // IMAGE_OPTIONAL_HEADER
        var optionalHeader = fileHeader + 20;

        nint sectionHeader;
        if (this.Is32BitProcess) // IMAGE_OPTIONAL_HEADER32
            sectionHeader = optionalHeader + 224;
        else // IMAGE_OPTIONAL_HEADER64
            sectionHeader = optionalHeader + 240;

        // IMAGE_SECTION_HEADER
        var sectionCursor = sectionHeader;
        for (var i = 0; i < numSections; i++)
        {
            var sectionName = Marshal.ReadInt64(sectionCursor);

            // .text
            switch (sectionName)
            {
                case 0x747865742E: // .text
                    this.TextSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.TextSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                    break;
                case 0x617461642E: // .data
                    this.DataSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.DataSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                    break;
                case 0x61746164722E: // .rdata
                    this.RDataSectionOffset = Marshal.ReadInt32(sectionCursor, 12);
                    this.RDataSectionSize = Marshal.ReadInt32(sectionCursor, 8);
                    break;
            }

            sectionCursor += 40;
        }
        
        Plugin.Log.Debug($"[MultiSigScanner] TextSectionOffset: 0x{this.TextSectionOffset:X}");
        Plugin.Log.Debug($"[MultiSigScanner] TextSectionSize: 0x{this.TextSectionSize:X}");
        Plugin.Log.Debug($"[MultiSigScanner] DataSectionOffset: 0x{this.DataSectionOffset:X}");
        Plugin.Log.Debug($"[MultiSigScanner] DataSectionSize: 0x{this.DataSectionSize:X}");
        Plugin.Log.Debug($"[MultiSigScanner] RDataSectionOffset: 0x{this.RDataSectionOffset:X}");
        Plugin.Log.Debug($"[MultiSigScanner] RDataSectionSize: 0x{this.RDataSectionSize:X}");
    }
    
    private static unsafe string ByteString(byte* data, int offset, int length)
    {
        var sb = new StringBuilder();
        for (var i = offset; i < length; i++)
        {
            sb.Append($"{data[i]:X2}");
        }
        return sb.ToString();
    }

    private void SetupCopy()
    {
        var handle = PInvoke.CreateFile(Module.FileName,
                GenericRead,
                FILE_SHARE_MODE.FILE_SHARE_READ, 
                null,
                FILE_CREATION_DISPOSITION.OPEN_EXISTING,
                FILE_FLAGS_AND_ATTRIBUTES.FILE_ATTRIBUTE_NORMAL,
                null);

        if (handle.IsInvalid)
        {
            Plugin.Log.Error($"[MultiSigScanner] Failed to open file handle for {Module.FileName}");
            return;
        }
        
        var map = PInvoke.CreateFileMapping(handle,
            null,
            PAGE_PROTECTION_FLAGS.PAGE_READONLY | PAGE_PROTECTION_FLAGS.SEC_IMAGE,
            0,
            0,
            null);
        
        if (map.IsInvalid)
        {
            Plugin.Log.Error($"[MultiSigScanner] Failed to create file mapping for {Module.FileName}");
            return;
        }

        var view = PInvoke.MapViewOfFile(map,
            FILE_MAP.FILE_MAP_COPY,
            0,
            0,
            0);
        
        // .text
        this.moduleCopyPtr = Marshal.AllocHGlobal(this.Module.ModuleMemorySize);
        
        unsafe
        {
            if (view.Value == null)
            {
                Plugin.Log.Error($"[MultiSigScanner] Failed to map view of file for {Module.FileName}");
                Plugin.Log.Error($"[MultiSigScanner] Marshal.GetLastWin32Error(): {Marshal.GetLastWin32Error()}");
                Plugin.Log.Error($"[MultiSigScanner] Marshal.GetLastPInvokeError(): {Marshal.GetLastPInvokeError()}");
                Plugin.Log.Error($"[MultiSigScanner] Marshal.GetLastPInvokeErrorMessage(): {Marshal.GetLastPInvokeErrorMessage()}");
                return;
            }
            
            Buffer.MemoryCopy(
                (byte*)view.Value,
                this.moduleCopyPtr.ToPointer(),
                this.Module.ModuleMemorySize,
                this.Module.ModuleMemorySize);
            Plugin.Log.Debug($"[MultiSigScanner] First 16 bytes of data: {ByteString((byte*)moduleCopyPtr, 0, 16)}");
        }

        this.moduleCopyOffset = this.moduleCopyPtr.ToInt64() - this.Module.BaseAddress.ToInt64();
        Plugin.Log.Debug($"[MultiSigScanner] Offset is 0x{this.moduleCopyOffset:X} ({moduleCopyPtr.ToInt64()} - {Module.BaseAddress.ToInt64()})");
        Plugin.Log.Debug("[MultiSigScanner] Unmapping.");
        unsafe
        {
            PInvoke.UnmapViewOfFile((MEMORY_MAPPED_VIEW_ADDRESS)view.Value);   
        }
        Plugin.Log.Debug("[MultiSigScanner] Unmapped. Closing handles.");
        PInvoke.CloseHandle((HANDLE)handle.DangerousGetHandle());
        PInvoke.CloseHandle((HANDLE)map.DangerousGetHandle());
        Plugin.Log.Debug("[MultiSigScanner] Handles closed.");
    }
}
