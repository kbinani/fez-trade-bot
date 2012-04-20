using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;

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
        /// アイコン領域の画像の中に，トレード要請を表すアイコンが表示されているかどうかを取得する
        /// </summary>
        /// <returns></returns>
        public bool HasTradeIcon( Bitmap screenShot ) {
            Bitmap iconArea = (Bitmap)screenShot.Clone( GetIconAreaRectangle( screenShot ), screenShot.PixelFormat );
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
        public Rectangle GetIconAreaRectangle( Bitmap screenShot ) {
            int left = screenShot.Width - 105;
            int top = screenShot.Height - 216;
            int width = 97;
            int height = 57;
            return new Rectangle( left, top, width, height );
        }

        /// <summary>
        /// トレード要求をしてきたユーザー名が表示されている領域を取得する
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        public Rectangle GetTradeUserNameRectangle( Bitmap screenShot ) {
            Rectangle result = GetIconAreaRectangle( screenShot );
            result.Height = 11;
            return result;
        }

        /// <summary>
        /// 指定されたハンドルのウィンドウについて，スクリーンキャプチャを行う
        /// </summary>
        /// <param name="handle"></param>
        /// <returns></returns>
        public Bitmap CaptureWindow(){
            IntPtr winDC = WindowsAPI.GetWindowDC( this.windowHandle );
            WindowsAPI.RECT winRect = new WindowsAPI.RECT();
            if( !WindowsAPI.GetWindowRect( this.windowHandle, ref winRect ) ) {
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
            WindowsAPI.ReleaseDC( this.windowHandle, winDC );

            return bmp;
        }

        /// <summary>
        /// 画面の指定した位置をクリックする．座標には，ゲーム画面に対する相対座標を指定する
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        public void Click( int x, int y ) {
            var geometry = new WindowsAPI.RECT();
            WindowsAPI.GetWindowRect( this.windowHandle, ref geometry );
            var clickPosition = new Point();
            clickPosition.X = geometry.left + x;
            clickPosition.Y = geometry.top + y;
            WindowsAPI.SetCursorPos( clickPosition.X, clickPosition.Y );

            WindowsAPI.mouse_event( WindowsAPI.LEFTDOWN, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
            Thread.Sleep( 200 );
            WindowsAPI.mouse_event( WindowsAPI.LEFTUP, (uint)clickPosition.X, (uint)clickPosition.Y, 0, UIntPtr.Zero );
        }

        /// <summary>
        /// ゲーム画面のウィンドウハンドルを取得する
        /// </summary>
        public IntPtr Handle {
            get {
                return this.windowHandle;
            }
        }
    }
}
