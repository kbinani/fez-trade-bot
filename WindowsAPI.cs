using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace com.github.kbinani.feztradenotify {
    class WindowsAPI {
        public const int SRCCOPY = 13369376;

        [DllImport( "user32.dll", CharSet = CharSet.Auto )]
        public static extern IntPtr FindWindow( string lpClassName, string lpWindowName );

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
        
        [StructLayout( LayoutKind.Sequential )]
        public struct RECT {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
