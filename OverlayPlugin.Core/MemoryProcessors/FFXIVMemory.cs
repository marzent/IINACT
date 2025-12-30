using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Machina.FFXIV;

namespace RainbowMage.OverlayPlugin.MemoryProcessors
{
    public interface IVersionedMemory
    {
        Version GetVersion();
        void ScanPointers();
        bool IsValid();
    }

    public partial class FFXIVMemory
    {
        private event EventHandler<Process> OnProcessChange;

        private ILogger logger;
        private Process process;
        private IntPtr processHandle;
        private FFXIVRepository repository;

        // The "international" version always uses the most recent.
        private static Version globalVersion = new Version(99, 0);
        private static Version cnVersion = new Version(6, 2);
        private static Version koVersion = new Version(6, 1);

        public FFXIVMemory(TinyIoCContainer container)
        {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();

            repository.RegisterProcessChangedHandler(UpdateProcess);
        }

        [SuppressGCTransition]
        [LibraryImport("SafeMemoryReader.dll")]
        private static partial int ReadMemory(nint dest, nint src, int size);

        public void RegisterOnProcessChangeHandler(EventHandler<Process> handler)
        {
            OnProcessChange += handler;
            handler.Invoke(this, process);
        }

        private void UpdateProcess(Process proc)
        {
            if (processHandle != IntPtr.Zero)
            {
                CloseProcessHandle();
            }

            if (proc == null)
                return;

            if (proc.ProcessName == "ffxiv")
            {
                logger.Log(LogLevel.Error, "{0}", "DX9 is not supported.");
                return;
            }
            else if (proc.ProcessName != "ffxiv_dx11")
            {
                logger.Log(LogLevel.Error, "{0}", "Unknown ffxiv process.");
                return;
            }

            try
            {
                process = proc;
                processHandle = -1;
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Error, "Failed to open FFXIV process: {0}", e);

                process = null;
                processHandle = IntPtr.Zero;
            }

            OnProcessChange?.Invoke(this, process);
        }

        public IntPtr GetBaseAddress()
        {
            return process.MainModule.BaseAddress;
        }

        private void CloseProcessHandle()
        {
            processHandle = IntPtr.Zero;
            process = null;
        }

        public bool IsValid()
        {
            if (processHandle != IntPtr.Zero)
                return true;

            return false;
        }

        public static unsafe string GetStringFromBytes(byte* source, int size, int realSize = 0)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(size);

            fixed (byte* bytesPtr = bytes)
            {
                if (ReadMemory((nint)bytesPtr, (nint)source, size) != 0)
                {
                    ArrayPool<byte>.Shared.Return(bytes);
                    throw new InvalidOperationException("Failed to read memory.");
                }
            }
            
            for (var i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                {
                    continue;
                }

                realSize = i;
                break;
            }
            
            var ret = System.Text.Encoding.UTF8.GetString(bytes, 0, realSize);
            ArrayPool<byte>.Shared.Return(bytes);
            return ret;
        }

        public static string GetStringFromBytes(byte[] source, int offset = 0, int size = 256)
        {
            var bytes = ArrayPool<byte>.Shared.Rent(size);
            Array.Copy(source, offset, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++)
            {
                if (bytes[i] != 0)
                {
                    continue;
                }

                realSize = i;
                break;
            }

            var ret = System.Text.Encoding.UTF8.GetString(bytes, 0, realSize);
            ArrayPool<byte>.Shared.Return(bytes);
            return ret;
        }

        /// <summary>
        /// バッファの長さだけメモリを読み取ってバッファに格納
        /// </summary>
        public unsafe bool Peek(IntPtr address, byte[] buffer)
        {
            fixed (byte* bufferPtr = buffer)
            {
                var result = ReadMemory((nint)bufferPtr, address, buffer.Length);
                return result == 0;
            }
        }

        /// <summary>
        /// メモリから指定された長さだけ読み取りバイト配列として返す
        /// </summary>
        /// <param name="address">読み取る開始アドレス</param>
        /// <param name="length">読み取る長さ</param>
        /// <returns></returns>
        public byte[] GetByteArray(IntPtr address, int length)
        {
            var data = new byte[length];
            Peek(address, data);
            return data;
        }
        
        /// <summary>
        /// メモリから指定された長さだけ読み取りバイト配列として返す
        /// </summary>
        /// <param name="address">読み取る開始アドレス</param>
        /// <param name="length">読み取る長さ</param>
        /// <returns></returns>
        public byte[] GetByteArrayPooled(IntPtr address, int length)
        {
            var data = ArrayPool<byte>.Shared.Rent(length);
            Peek(address, data);
            return data;
        }

        /// <summary>
        /// メモリから4バイト読み取り32ビットIntegerとして返す
        /// </summary>
        /// <param name="address">読み取る位置</param>
        /// <param name="offset">オフセット</param>
        /// <returns></returns>
        public unsafe int GetInt32(IntPtr address, int offset = 0)
        {
            int ret;
            var value = new byte[4];
            Peek(nint.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(int*)p;
            return ret;
        }

        public unsafe long GetInt64(IntPtr address, int offset = 0)
        {
            long ret;
            var value = new byte[8];
            Peek(nint.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(long*)p;
            return ret;
        }

        /// Reads |count| bytes at |addr| in the |process|. Returns null on error.
        public byte[] Read8(IntPtr addr, int count)
        {
            var data = new byte[count];
            return Peek(addr, data) ? data : null;
        }

        /// Reads |addr| in the |process| and returns it as a 16bit ints. Returns null on error.
        public Int16[] Read16(IntPtr addr, int count)
        {
            var buffer = Read8(addr, count * 2);
            if (buffer == null)
                return null;
            var out_buffer = new Int16[count];
            for (int i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt16(buffer, 2 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit ints. Returns null on error.
        public Int32[] Read32(IntPtr addr, int count)
        {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new Int32[count];
            for (int i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt32(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit uints. Returns null on error.
        public UInt32[] Read32U(IntPtr addr, int count)
        {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new UInt32[count];
            for (int i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToUInt32(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit floats. Returns null on error.
        public float[] ReadSingle(IntPtr addr, int count)
        {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new float[count];
            for (int i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToSingle(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 64bit ints. Returns null on error.
        public Int64[] Read64(IntPtr addr, int count)
        {
            var buffer = Read8(addr, count * 8);
            if (buffer == null)
                return null;
            var out_buffer = new Int64[count];
            for (int i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt64(buffer, 8 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 64bit pointer. Returns 0 on error.
        public unsafe IntPtr ReadIntPtr(IntPtr addr)
        {
            var buffer = Read8(addr, 8);
            if (buffer == null)
                return IntPtr.Zero;
            return new IntPtr(BitConverter.ToInt64(buffer, 0));
        }

        /// <summary>
        /// Signature scan.
        /// Searches the |process| memory for a |pattern|, which can include wildcards. When the
        /// pattern is found, it reads a pointer found at |offset| bytes after the end of the
        /// pattern.
        /// If the pattern is found multiple times, the pointer relative to the end of each
        /// instance is returned.
        ///
        /// Heavily based on code from ACT_EnmityPlugin.
        /// </summary>
        /// <param name="pattern">String containing bytes represented in hex to search for, with "??" as a wildcard.</param>
        /// <param name="offset">The offset from the end of the found pattern to read a pointer from the process memory.</param>
        /// <param name="rip_addressing">Uses x64 RIP relative addressing mode</param>
        /// <returns>A list of pointers read relative to the end of strings in the process memory matching the |pattern|.</returns>
        public List<IntPtr> SigScan(string pattern, int pattern_offset, bool rip_addressing, int rip_offset = 0)
        {
            List<IntPtr> matches_list = new List<IntPtr>();

            if (pattern == null || pattern.Length % 2 != 0)
            {
                logger.Log(LogLevel.Error, "Invalid signature pattern: {0}", pattern);
                return matches_list;
            }

            // Build a byte array from the pattern string. "??" is a wildcard
            // represented as null in the array.
            byte?[] pattern_array = new byte?[pattern.Length / 2];
            for (int i = 0; i < pattern.Length / 2; i++)
            {
                string text = pattern.Substring(i * 2, 2);
                if (text == "??")
                {
                    pattern_array[i] = null;
                }
                else
                {
                    pattern_array[i] = new byte?(Convert.ToByte(text, 16));
                }
            }

            // Read this many bytes at a time. This needs to be a 32bit number as BitConverter pulls
            // from a 32bit offset into the array that we read from the process.
            const Int32 kMaxReadSize = 65536;

            int module_memory_size = process.MainModule.ModuleMemorySize;
            IntPtr process_start_addr = process.MainModule.BaseAddress;
            IntPtr process_end_addr = IntPtr.Add(process_start_addr, module_memory_size);

            IntPtr read_start_addr = process_start_addr;
            byte[] read_buffer = new byte[kMaxReadSize];
            while (read_start_addr.ToInt64() < process_end_addr.ToInt64())
            {
                // Determine how much to read without going off the end of the process.
                Int64 bytes_left = process_end_addr.ToInt64() - read_start_addr.ToInt64();
                IntPtr read_size = (IntPtr)Math.Min(bytes_left, kMaxReadSize);

                IntPtr num_bytes_read = IntPtr.Zero;
                if (NativeMethods.ReadProcessMemory(processHandle, read_start_addr, read_buffer, read_size,
                                                    ref num_bytes_read))
                {
                    int max_search_offset =
                        num_bytes_read.ToInt32() - pattern_array.Length - Math.Max(0, pattern_offset);
                    // With RIP we will read a 4byte pointer at the |offset|, else we read an 8byte pointer. Either
                    // way we can't find a pattern such that the pointer we want to read is off the end of the buffer.
                    if (rip_addressing)
                        max_search_offset -= 4; //  + 1L; ?
                    else
                        max_search_offset -= 8;

                    for (int search_offset = 0; (Int64)search_offset < max_search_offset; ++search_offset)
                    {
                        bool found_pattern = true;
                        for (int pattern_i = 0; pattern_i < pattern_array.Length; pattern_i++)
                        {
                            // Wildcard always matches, otherwise compare to the read_buffer.
                            byte? pattern_byte = pattern_array[pattern_i];
                            if (pattern_byte.HasValue &&
                                pattern_byte.Value != read_buffer[search_offset + pattern_i])
                            {
                                found_pattern = false;
                                break;
                            }
                        }

                        if (found_pattern)
                        {
                            IntPtr pointer;
                            if (rip_addressing)
                            {
                                Int32 rip_ptr_offset =
                                    BitConverter.ToInt32(read_buffer,
                                                         search_offset + pattern_array.Length + pattern_offset);
                                Int64 pattern_start_game_addr = read_start_addr.ToInt64() + search_offset;
                                Int64 pointer_offset_from_pattern_start = pattern_array.Length + pattern_offset;
                                Int64 rip_ptr_base = pattern_start_game_addr + pointer_offset_from_pattern_start + 4 +
                                                     rip_offset;
                                // In RIP addressing, the pointer from the executable is 32bits which we stored as |rip_ptr_offset|. The pointer
                                // is then added to the address of the byte following the pointer, making it relative to that address, which we
                                // stored as |rip_ptr_base|.
                                pointer = new IntPtr((Int64)rip_ptr_offset + rip_ptr_base);
                            }
                            else
                            {
                                // In normal addressing, the 64bits found with the pattern are the absolute pointer.
                                pointer = new IntPtr(
                                    BitConverter.ToInt64(read_buffer,
                                                         search_offset + pattern_array.Length + pattern_offset));
                            }

                            matches_list.Add(pointer);
                        }
                    }
                }

                // Move to the next contiguous buffer to read.
                // TODO: If the pattern lies across 2 buffers, then it would not be found.
                read_start_addr = IntPtr.Add(read_start_addr, kMaxReadSize);
            }

            return matches_list;
        }

        // Returns the best candidate from a list of versioned memory candidates.
        public static T FindCandidate<T>(List<T> candidates, GameRegion region) where T : IVersionedMemory
        {
            // General algorithm, for a given desired version X:
            // 1) return the first element <= X
            // 2) return elements after X in ascending order
            // 3) return elements before X in descending order (just in case)
            //
            // e.g. if X = 6.1 with [5.2, 5.3, 6.0, 6.2] return 6.0, 6.2, 5.3, 5.2

            Version target;
            if (region == GameRegion.Chinese)
                target = cnVersion;
            else if (region == GameRegion.Korean)
                target = koVersion;
            else
                target = globalVersion;

            candidates = candidates.OrderBy(x => x.GetVersion()).ToList();
            int idx = candidates.FindIndex(x => x.GetVersion() > target);

            // If not found, all candidates are <= target version, so walk in descending order.
            // If found, then idx is the first candidate larger than target, so try to
            // start on the candidate before it.
            if (idx == -1)
                idx = candidates.Count;
            else
                idx = Math.Max(idx - 1, 0);

            for (var i = idx; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                candidate.ScanPointers();
                if (candidate.IsValid())
                    return candidate;
            }

            if (idx == 0)
                return default(T);

            for (var i = idx - 1; i >= 0; i--)
            {
                var candidate = candidates[i];
                candidate.ScanPointers();
                if (candidate.IsValid())
                    return candidate;
            }

            return default(T);
        }
    }
}
