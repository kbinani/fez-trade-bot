using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace com.github.kbinani.feztradenotify {
    /// <summary>
    /// FEZのゲーム画面からの情報取得処理と操作処理を行うクラス
    /// </summary>
    class FEZWindow {
        private IntPtr windowHandle;

        /// <summary>
        /// FEZのゲーム画面のウィンドウハンドルを指定し，初期化する
        /// </summary>
        /// <param name="handle"></param>
        public FEZWindow( IntPtr handle ) {
            this.windowHandle = handle;
        }

        /// <summary>
        /// トレードボタンの位置を取得する
        /// </summary>
        /// <returns></returns>
        public Point GetTradeIconLocation() {
            Bitmap screenShot = screenShot = CaptureWindow( this.windowHandle );
            Rectangle iconAreaRectangle = GetIconAreaRectangle( screenShot );
            Bitmap iconArea = screenShot.Clone( iconAreaRectangle, screenShot.PixelFormat );
            if( IsTradeIcon( iconArea ) ) {
                var windowRect = new WindowsAPI.RECT();
                WindowsAPI.GetWindowRect( this.windowHandle, ref windowRect );
                int x = windowRect.left + iconAreaRectangle.Left + iconAreaRectangle.Width / 2;
                int y = windowRect.top + iconAreaRectangle.Top + iconAreaRectangle.Height / 2;
                return new Point( x, y );
            } else {
                throw new ApplicationException( "トレードボタンの位置を取得できなかった" );
            }
        }

        /// <summary>
        /// 取引アイコンが出る位置の画像を取得する
        /// </summary>
        /// <returns></returns>
        public Bitmap GetTradeIcon() {
            Bitmap screenShot = CaptureWindow( this.windowHandle );
            Rectangle iconAreaRectangle = GetIconAreaRectangle( screenShot );
            return screenShot.Clone( iconAreaRectangle, screenShot.PixelFormat );
        }

        /// <summary>
        /// アイコン領域の画像の中に，トレード要請を表すアイコンが表示されているかどうかを取得する
        /// </summary>
        /// <returns></returns>
        private bool IsTradeIcon( Bitmap iconArea ) {
            Bitmap mask = Resource.icon_mask;
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
            return diffPercentage <= 5.0;
        }

        /// <summary>
        /// ウィンドウ全体のスクリーンショットから，アイコン領域の部分を取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        private Rectangle GetIconAreaRectangle( Bitmap screenShot ) {
            int left = screenShot.Width - 105;
            int top = screenShot.Height - 216;
            int width = 97;
            int height = 57;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// 指定されたハンドルのウィンドウについて，スクリーンキャプチャを行う
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        private Bitmap CaptureWindow( IntPtr handle ) {
            IntPtr winDC = WindowsAPI.GetWindowDC( handle );
            WindowsAPI.RECT winRect = new WindowsAPI.RECT();
            if( !WindowsAPI.GetWindowRect( handle, ref winRect ) ) {
                throw new ApplicationException( "ウィンドウサイズを取得できなかった" );
            }
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
