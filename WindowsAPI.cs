using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace com.github.kbinani.feztradebot {
    class WindowsAPI {
        public const int SRCCOPY = 13369376;
        public const uint LEFTDOWN = 0x00000002;
        public const uint LEFTUP = 0x00000004;
        public const uint MIDDLEDOWN = 0x00000020;
        public const uint MIDDLEUP = 0x00000040;
        public const uint MOVE = 0x00000001;
        public const uint ABSOLUTE = 0x00008000;
        public const uint RIGHTDOWN = 0x00000008;
        public const uint RIGHTUP = 0x00000010;
        public const uint WHEEL = 0x00000800;
        public const uint XDOWN = 0x00000080;
        public const uint XUP = 0x00000100;

        public const byte VK_ESCAPE = 0x1B;
        public const byte VK_ENTER = 10;
        public const byte VK_RETURN = 0xD;
        public const byte VK_CONTROL = 17;
        public const byte VK_BACK_SPACE = 8;
        public const byte KEYEVENTF_KEYUP = 0x0002;

        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_KEYUP = 0x0101;

        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        public static extern IntPtr FindWindow( string lpClassName, string lpWindowName );

        [DllImport( "user32.dll", SetLastError = true )]
        public static extern IntPtr FindWindowEx( IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle );

        [DllImport( "user32.dll" )]
        public static extern bool GetWindowRect( IntPtr hWnd, ref RECT rect );

        [DllImport( "user32.dll" )]
        public static extern IntPtr GetWindowDC( IntPtr hwnd );

        [DllImport( "gdi32.dll" )]
        public static extern int BitBlt(
            IntPtr hDestDC,
            int x, int y, int nWidth, int nHeight,
            IntPtr hSrcDC,
            int xSrc, int ySrc, int dwRop );

        [DllImport( "user32.dll" )]
        public static extern IntPtr ReleaseDC( IntPtr hwnd, IntPtr hdc );

        [DllImport( "user32" )]
        public static extern bool SetForegroundWindow( IntPtr hwnd );

        [DllImport( "user32.dll" )]
        public static extern bool SetCursorPos( int X, int Y );

        [DllImport( "user32.dll" )]
        public static extern void mouse_event( uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo );

        [DllImport( "user32.dll" )]
        public static extern void keybd_event( byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo );

        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        public static extern IntPtr SendMessage( IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam );

        [DllImport( "user32.dll" )]
        public static extern IntPtr SendMessage( IntPtr hWnd, UInt32 Msg, Int32 wParam, Int32 lParam );

        [StructLayout( LayoutKind.Sequential )]
        public struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
