using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using Growl.Connector;
using Growl.CoreLibrary;

namespace com.github.kbinani.feztradenotify {
    class DaemonRunner {
        private const string APPLICATION_NAME = "FEZ trade notify";

        private Bitmap iconMask = null;
        private GrowlConnector connector = null;
        private Growl.Connector.Application application = null;
        private NotificationType notificationType;

        private string host;
        private string pass;
        private int port;

        public DaemonRunner( string host, string pass, int port ) {
            this.host = host;
            this.pass = pass;
            this.port = port;
        }

        public void Run() {
            while( true ) {
                IntPtr handle = WindowsAPI.FindWindow( null, "Fantasy Earth Zero" );
                if( handle != IntPtr.Zero ) {
                    Bitmap screenShot = CaptureWindow( handle );
                    Bitmap iconArea = ClipIconArea( screenShot );
                    if( IsTradeIcon( iconArea ) ) {
                        SendNotify( iconArea );
                    }
                }
                GC.Collect();
                Thread.Sleep( 1000 );
            }
        }

        private GrowlConnector GetConnector() {
            if( connector == null ) {
                connector = new GrowlConnector( this.pass, this.host, this.port );

                application = new Growl.Connector.Application( APPLICATION_NAME );
                notificationType = new NotificationType( "FEZ_TRADE_NOTIFICATION", "Trade Notification" );
                connector.Register( application, new NotificationType[] { notificationType } );

                connector.EncryptionAlgorithm = Cryptography.SymmetricAlgorithmType.PlainText;
            }
            return connector;
        }

        private void SendNotify( Bitmap screenShot ) {
            var connector = GetConnector();
            CallbackContext callbackContext = new CallbackContext( "some fake information", "fake data" );

            Notification notification = new Notification(
                application.Name, notificationType.Name, DateTime.Now.Ticks.ToString(),
                "Trade Notification", "trade request received", screenShot, false, Priority.Normal, "0" );
            connector.Notify( notification, callbackContext );
        }

        /// <summary>
        /// アイコン領域の画像の中に，トレード要請を表すアイコンが表示されているかどうかを取得する
        /// </summary>
        /// <returns></returns>
        private bool IsTradeIcon( Bitmap iconArea ) {
            Bitmap mask = GetIconMask();
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
        private Bitmap GetIconMask() {
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
        private Bitmap ClipIconArea( Bitmap screenShot ) {
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
        private Bitmap CaptureWindow( IntPtr handle ) {
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
