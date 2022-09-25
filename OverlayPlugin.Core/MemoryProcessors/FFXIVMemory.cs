using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin.MemoryProcessors {
    public class FFXIVMemory {
        public event EventHandler OnProcessChange;

        private ILogger logger;
        private Process process;
        private IntPtr processHandle;
        private FFXIVRepository repository;

        public FFXIVMemory(TinyIoCContainer container) {
            logger = container.Resolve<ILogger>();
            repository = container.Resolve<FFXIVRepository>();

            repository.RegisterProcessChangedHandler(UpdateProcess);
        }

        private void UpdateProcess(Process proc) {
            if (processHandle != IntPtr.Zero) {
                CloseProcessHandle();
            }

            if (proc == null || proc.HasExited)
                return;

            if (proc.ProcessName == "ffxiv") {
                logger.Log(LogLevel.Error, "{0}", "DX9 is not supported.");
                return;
            } else if (proc.ProcessName != "ffxiv_dx11") {
                logger.Log(LogLevel.Error, "{0}", "Unknown ffxiv process.");
                return;
            }

            try {
                process = proc;
                processHandle = NativeMethods.OpenProcess(ProcessAccessFlags.VirtualMemoryRead, false, proc.Id);
            }
            catch (Exception e) {
                logger.Log(LogLevel.Error, "Failed to open FFXIV process: {0}", e);

                process = null;
                processHandle = IntPtr.Zero;
            }

            OnProcessChange?.Invoke(this, null);
        }

        private void CloseProcessHandle() {
            NativeMethods.CloseHandle(processHandle);
            processHandle = IntPtr.Zero;
            process = null;
        }

        public bool IsValid() {
            if (process != null && process.HasExited) {
                CloseProcessHandle();
                OnProcessChange?.Invoke(this, null);
            }

            if (processHandle != IntPtr.Zero)
                return true;

            return false;
        }

        public unsafe static string GetStringFromBytes(byte* source, int size) {
            var bytes = new byte[size];
            Marshal.Copy((IntPtr)source, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++) {
                if (bytes[i] != 0) {
                    continue;
                }
                realSize = i;
                break;
            }
            Array.Resize(ref bytes, realSize);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        public static string GetStringFromBytes(byte[] source, int offset = 0, int size = 256) {
            var bytes = new byte[size];
            Array.Copy(source, offset, bytes, 0, size);
            var realSize = 0;
            for (var i = 0; i < size; i++) {
                if (bytes[i] != 0) {
                    continue;
                }
                realSize = i;
                break;
            }
            Array.Resize(ref bytes, realSize);
            return System.Text.Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// バッファの長さだけメモリを読み取ってバッファに格納
        /// </summary>
        public bool Peek(IntPtr address, byte[] buffer) {
            var zero = IntPtr.Zero;
            var nSize = new IntPtr(buffer.Length);
            return NativeMethods.ReadProcessMemory(processHandle, address, buffer, nSize, ref zero);
        }

        /// <summary>
        /// メモリから指定された長さだけ読み取りバイト配列として返す
        /// </summary>
        /// <param name="address">読み取る開始アドレス</param>
        /// <param name="length">読み取る長さ</param>
        /// <returns></returns>
        public byte[] GetByteArray(IntPtr address, int length) {
            var data = new byte[length];
            Peek(address, data);
            return data;
        }

        /// <summary>
        /// メモリから4バイト読み取り32ビットIntegerとして返す
        /// </summary>
        /// <param name="address">読み取る位置</param>
        /// <param name="offset">オフセット</param>
        /// <returns></returns>
        public unsafe int GetInt32(IntPtr address, int offset = 0) {
            int ret;
            var value = new byte[4];
            Peek(IntPtr.Add(address, offset), value);
            fixed (byte* p = &value[0]) ret = *(int*)p;
            return ret;
        }

        /// Reads |count| bytes at |addr| in the |process|. Returns null on error.
        public byte[] Read8(IntPtr addr, int count) {
            var buffer_len = 1 * count;
            var buffer = new byte[buffer_len];
            var bytes_read = IntPtr.Zero;
            var ok = NativeMethods.ReadProcessMemory(processHandle, addr, buffer, new IntPtr(buffer_len), ref bytes_read);
            if (!ok || bytes_read.ToInt32() != buffer_len)
                return null;
            return buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 16bit ints. Returns null on error.
        public Int16[] Read16(IntPtr addr, int count) {
            var buffer = Read8(addr, count * 2);
            if (buffer == null)
                return null;
            var out_buffer = new Int16[count];
            for (var i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt16(buffer, 2 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit ints. Returns null on error.
        public Int32[] Read32(IntPtr addr, int count) {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new Int32[count];
            for (var i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt32(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit uints. Returns null on error.
        public UInt32[] Read32U(IntPtr addr, int count) {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new UInt32[count];
            for (var i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToUInt32(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 32bit floats. Returns null on error.
        public float[] ReadSingle(IntPtr addr, int count) {
            var buffer = Read8(addr, count * 4);
            if (buffer == null)
                return null;
            var out_buffer = new float[count];
            for (var i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToSingle(buffer, 4 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 64bit ints. Returns null on error.
        public Int64[] Read64(IntPtr addr, int count) {
            var buffer = Read8(addr, count * 8);
            if (buffer == null)
                return null;
            var out_buffer = new Int64[count];
            for (var i = 0; i < count; ++i)
                out_buffer[i] = BitConverter.ToInt64(buffer, 8 * i);
            return out_buffer;
        }

        /// Reads |addr| in the |process| and returns it as a 64bit pointer. Returns 0 on error.
        public IntPtr ReadIntPtr(IntPtr addr) {
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
        public List<IntPtr> SigScan(string pattern, int pattern_offset, bool rip_addressing, int rip_offset = 0) {
            var matches_list = new List<IntPtr>();

            if (pattern == null || pattern.Length % 2 != 0) {
                logger.Log(LogLevel.Error, "Invalid signature pattern: {0}", pattern);
                return matches_list;
            }

            // Build a byte array from the pattern string. "??" is a wildcard
            // represented as null in the array.
            var pattern_array = new byte?[pattern.Length / 2];
            for (var i = 0; i < pattern.Length / 2; i++) {
                var text = pattern.Substring(i * 2, 2);
                if (text == "??") {
                    pattern_array[i] = null;
                } else {
                    pattern_array[i] = new byte?(Convert.ToByte(text, 16));
                }
            }

            // Read this many bytes at a time. This needs to be a 32bit number as BitConverter pulls
            // from a 32bit offset into the array that we read from the process.
            const Int32 kMaxReadSize = 65536;

            var module_memory_size = process.MainModule.ModuleMemorySize;
            var process_start_addr = process.MainModule.BaseAddress;
            var process_end_addr = IntPtr.Add(process_start_addr, module_memory_size);

            var read_start_addr = process_start_addr;
            var read_buffer = new byte[kMaxReadSize];
            while (read_start_addr.ToInt64() < process_end_addr.ToInt64()) {
                // Determine how much to read without going off the end of the process.
                var bytes_left = process_end_addr.ToInt64() - read_start_addr.ToInt64();
                var read_size = (IntPtr)Math.Min(bytes_left, kMaxReadSize);

                var num_bytes_read = IntPtr.Zero;
                if (NativeMethods.ReadProcessMemory(processHandle, read_start_addr, read_buffer, read_size, ref num_bytes_read)) {
                    var max_search_offset = num_bytes_read.ToInt32() - pattern_array.Length - Math.Max(0, pattern_offset);
                    // With RIP we will read a 4byte pointer at the |offset|, else we read an 8byte pointer. Either
                    // way we can't find a pattern such that the pointer we want to read is off the end of the buffer.
                    if (rip_addressing)
                        max_search_offset -= 4;  //  + 1L; ?
                    else
                        max_search_offset -= 8;

                    for (var search_offset = 0; (Int64)search_offset < max_search_offset; ++search_offset) {
                        var found_pattern = true;
                        for (var pattern_i = 0; pattern_i < pattern_array.Length; pattern_i++) {
                            // Wildcard always matches, otherwise compare to the read_buffer.
                            var pattern_byte = pattern_array[pattern_i];
                            if (pattern_byte.HasValue &&
                                pattern_byte.Value != read_buffer[search_offset + pattern_i]) {
                                found_pattern = false;
                                break;
                            }
                        }
                        if (found_pattern) {
                            IntPtr pointer;
                            if (rip_addressing) {
                                var rip_ptr_offset = BitConverter.ToInt32(read_buffer, search_offset + pattern_array.Length + pattern_offset);
                                var pattern_start_game_addr = read_start_addr.ToInt64() + search_offset;
                                Int64 pointer_offset_from_pattern_start = pattern_array.Length + pattern_offset;
                                var rip_ptr_base = pattern_start_game_addr + pointer_offset_from_pattern_start + 4 + rip_offset;
                                // In RIP addressing, the pointer from the executable is 32bits which we stored as |rip_ptr_offset|. The pointer
                                // is then added to the address of the byte following the pointer, making it relative to that address, which we
                                // stored as |rip_ptr_base|.
                                pointer = new IntPtr((Int64)rip_ptr_offset + rip_ptr_base);
                            } else {
                                // In normal addressing, the 64bits found with the pattern are the absolute pointer.
                                pointer = new IntPtr(BitConverter.ToInt64(read_buffer, search_offset + pattern_array.Length + pattern_offset));
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
    }
}
