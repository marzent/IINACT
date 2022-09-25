﻿using System;
using System.Runtime.InteropServices;

namespace RainbowMage.OverlayPlugin {
    /// <summary>
    /// ネイティブ関数を提供します。
    /// </summary>
    internal class NativeMethods {
        private WinEventDelegate dele = null;

        public NativeMethods(TinyIoCContainer container) {
            ActiveWindowHandle = GetForegroundWindow();

            dele = new WinEventDelegate(WinEventProc);
            var result = SetWinEventHook(EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            if (result == IntPtr.Zero)
                container.Resolve<ILogger>().Log(LogLevel.Error, "Failed to register window foreground hook!");

            result = SetWinEventHook(EVENT_SYSTEM_MINIMIZEEND, EVENT_SYSTEM_MINIMIZEEND, IntPtr.Zero, dele, 0, 0, WINEVENT_OUTOFCONTEXT);
            if (result == IntPtr.Zero)
                container.Resolve<ILogger>().Log(LogLevel.Error, "Failed to register window minimized hook!");
        }

        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_FOREGROUND = 3;
        private const uint EVENT_SYSTEM_MINIMIZEEND = 0x0017;

        public event EventHandler<IntPtr> ActiveWindowChanged;
        public IntPtr ActiveWindowHandle;
        private void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime) {
            //ActiveWindowHandle = hwnd;
            //ActiveWindowChanged?.Invoke(null, hwnd);
        }

        // C# compiler can't track assignments in unmanaged code and thus complains about variables that
        // are never assigned to. Disable the warning here since it's pointless.
#pragma warning disable 0649
        public struct BlendFunction {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        public const byte AC_SRC_ALPHA = 1;
        public const byte AC_SRC_OVER = 0;

        public struct Point {
            public int X;
            public int Y;
        }

        public struct Size {
            public int Width;
            public int Height;
        }

        //public struct Rect
        //{
        //    public int Left;
        //    public int Top;
        //    public int Right;
        //    public int Bottom;
        //}

        [DllImport("user32")]
        public static extern bool UpdateLayeredWindow(
            IntPtr hWnd,
            IntPtr hdcDst,
            [In] ref Point pptDst,
            [In] ref Size pSize,
            IntPtr hdcSrc,
            [In] ref Point pptSrc,
            int crKey,
            [In] ref BlendFunction pBlend,
            uint dwFlags);

        public const int ULW_ALPHA = 2;

        [DllImport("gdi32")]
        public static extern IntPtr SelectObject(
            IntPtr hdc,
            IntPtr hgdiobj);

        [DllImport("gdi32")]
        public static extern bool DeleteObject(
            IntPtr hObject);

        [DllImport("gdi32")]
        public static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32")]
        public static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32")]
        public static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct BitmapInfo {
            public BitmapInfoHeader bmiHeader;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 1, ArraySubType = UnmanagedType.Struct)]
            public RgbQuad[] bmiColors;
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct BitmapInfoHeader {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public BitmapCompressionMode biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;

            public void Init() {
                biSize = (uint)Marshal.SizeOf(this);
            }
        }

        public enum BitmapCompressionMode : uint {
            BI_RGB = 0,
            BI_RLE8 = 1,
            BI_RLE4 = 2,
            BI_BITFIELDS = 3,
            BI_JPEG = 4,
            BI_PNG = 5
        }

        [StructLayoutAttribute(LayoutKind.Sequential)]
        public struct RgbQuad {
            public byte rgbBlue;
            public byte rgbGreen;
            public byte rgbRed;
            public byte rgbReserved;
        }

        [DllImport("gdi32")]
        public static extern IntPtr CreateDIBSection(
            IntPtr hdc,
            [In] ref BitmapInfo pbmi,
            uint iUsage,
            out IntPtr ppvBits,
            IntPtr hSection,
            uint dwOffset);

        public const int DIB_RGB_COLORS = 0x0000;

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", EntryPoint = "GetWindowLong", SetLastError = true)]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        public const int GWL_EXSTYLE = -20;
        public const int WS_EX_TRANSPARENT = 0x00000020;
        public const int WS_EX_TOOLWINDOW = 0x00000080;

        [DllImport("kernel32")]
        public static extern void CopyMemory(IntPtr dest, IntPtr src, uint count);


        // For hide from ALT+TAB preview

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        private static int ToIntPtr32(IntPtr intPtr) {
            return unchecked((int)intPtr.ToInt64());
        }

        public static IntPtr SetWindowLongA(IntPtr hWnd, int nIndex, IntPtr dwNewLong) {
            int error;
            IntPtr result;

            SetLastError(0);

            if (IntPtr.Size == 4) {
                var result32 = SetWindowLong(hWnd, nIndex, ToIntPtr32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(result32);
            } else {
                result = SetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0)) {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindow(
            IntPtr hWnd,  // 元ウィンドウのハンドル
            uint uCmd     // 関係
        );

        public const uint GW_HWNDPREV = 0x0003;

        [DllImport("user32.dll")]
        public static extern bool SetWindowPos(
            IntPtr hWnd,             // ウィンドウのハンドル
            IntPtr hWndInsertAfter,  // 配置順序のハンドル
            int X,                   // 横方向の位置
            int Y,                   // 縦方向の位置
            int cx,                  // 幅
            int cy,                  // 高さ
            uint uFlags              // ウィンドウ位置のオプション
        );

        public static readonly IntPtr HWND_TOP = new IntPtr(0);
        public static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        public const uint SWP_NOSIZE = 0x0001;
        public const uint SWP_NOMOVE = 0x0002;
        public const uint SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        public const int WM_KEYDOWN = 0x0100;
        public const int WM_KEYUP = 0x0101;
        public const int WM_CHAR = 0x0102;
        public const int WM_SYSKEYDOWN = 0x0104;
        public const int WM_SYSKEYUP = 0x0105;
        public const int WM_SYSCHAR = 0x0106;

        // Constants for extern calls to various scrollbar functions
        public const int SB_HORZ = 0x0;
        public const int SB_VERT = 0x1;
        public const int WM_HSCROLL = 0x114;
        public const int WM_VSCROLL = 0x115;
        public const int SB_THUMBPOSITION = 4;
        public const int SB_BOTTOM = 7;
        public const int SB_OFFSET = 13;

        [DllImport("user32.dll")]
        public static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern int GetScrollPos(IntPtr hWnd, int nBar);
        [DllImport("user32.dll")]
        public static extern bool PostMessageA(IntPtr hWnd, int nBar, int wParam, int lParam);
        [DllImport("user32.dll")]
        public static extern bool GetScrollRange(IntPtr hWnd, int nBar, out int lpMinPos, out int lpMaxPos);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(ProcessAccessFlags processAccess, bool bInheritHandle, int processId);
        [DllImport("kernel32.dll")]
        public static extern int CloseHandle(IntPtr hObject);
        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize, ref IntPtr lpNumberOfBytesRead);

#pragma warning restore 0649
    }

    [Flags]
    public enum ProcessAccessFlags : uint {
        All = 0x1F0FFF,
        Terminate = 0x1,
        CreateThread = 0x2,
        VirtualMemoryOperation = 0x8,
        VirtualMemoryRead = 0x10,
        VirtualMemoryWrite = 0x20,
        DuplicateHandle = 0x40,
        CreateProcess = 0x80,
        SetQuota = 0x100,
        SetInformation = 0x200,
        QueryInformation = 0x400,
        QueryLimitedInformation = 0x1000,
        Synchronize = 0x100000
    }
}
