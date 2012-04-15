using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;

namespace com.github.kbinani.feztradenotify {
    class Program {
        private static Bitmap iconMask = null;

        static void Main( string[] args ) {
            IntPtr handle = WindowsAPI.FindWindow( null, "Fantasy Earth Zero" );
            if( handle == IntPtr.Zero ) {
                //TODO: 何がしか処理
                return;
            }
            Bitmap screenShot = CaptureWindow( handle );
            Bitmap iconArea = ClipIconArea( screenShot );
            bool result = isTradeIcon( iconArea );
        }


        /// <summary>
        /// アイコン領域の画像の中に，トレード要請を表すアイコンが表示されているかどうかを取得する
        /// </summary>
        /// <returns></returns>
        private static bool isTradeIcon( Bitmap iconArea ) {
            Bitmap mask = getIconMask();
            Color maskColor = mask.GetPixel( 0, 0 );

            int totalPixels = 0;
            int matchPixels = 0;

            for( int y = 0; y < mask.Height; y++ ) {
                for( int x = 0; x < mask.Width; x++ ) {
                    Color colorOfMask = mask.GetPixel( x, y );
                    if( colorOfMask != maskColor ) {
                        Color colorOfActual = iconArea.GetPixel( x, y );
                        totalPixels++;
                        if( colorOfActual == colorOfMask ) {
                            matchPixels++;
                        }
                    }
                }
            }

            // アイコン画像テンプレートとの差があるピクセルの個数が，
            // 全体のピクセル数の 1% 以下であれば，テンプレートと同じとみなす
            double diffPercentage = (totalPixels - matchPixels) * 100.0 / totalPixels;
            return diffPercentage <= 1.0;
        }

        /// <summary>
        /// マスク画像を取得する
        /// </summary>
        /// <returns></returns>
        private static Bitmap getIconMask() {
            if( iconMask == null ) {
                iconMask = Resource.icon_mask;
            }
            return iconMask;
        }

        /// <summary>
        /// ウィンドウ全体のスクリーンショットから，アイコン領域の部分を取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        private static Bitmap ClipIconArea( Bitmap screenShot ) {
            int left = screenShot.Width - 105;
            int top = screenShot.Height - 216;
            int width = 97;
            int height = 57;
            return screenShot.Clone( new Rectangle( left, top, width, height ), screenShot.PixelFormat );
        }

        /// <summary>
        /// 指定されたハンドルのウィンドウについて，スクリーンキャプチャを行う
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private static Bitmap CaptureWindow( IntPtr handle ) {
            //TODO: 既にforegroundだったら，全面に持ってくる処理とsleepをしない，という処理を入れたい
            WindowsAPI.SetForegroundWindow( handle );
            Thread.Sleep( 300 );

            IntPtr winDC = WindowsAPI.GetWindowDC( handle );
            WindowsAPI.RECT winRect = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( handle, ref winRect );
            Bitmap bmp = new Bitmap( winRect.right - winRect.left,
                winRect.bottom - winRect.top );

            Graphics g = Graphics.FromImage( bmp );
            IntPtr hDC = g.GetHdc();
            WindowsAPI.BitBlt( hDC, 0, 0, bmp.Width, bmp.Height,
                winDC, 0, 0, WindowsAPI.SRCCOPY );

            g.ReleaseHdc( hDC );
            g.Dispose();
            WindowsAPI.ReleaseDC( handle, winDC );

            return bmp;
        }
    }
}
