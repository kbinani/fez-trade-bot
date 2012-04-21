using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace com.github.kbinani.feztradenotify {
    /// <summary>
    /// トレードを実行するタスク
    /// </summary>
    class DoTradeTask {
        private FEZWindow window;
        private Bitmap screenShot;

        public DoTradeTask( FEZWindow window, Bitmap screenShot ) {
            this.window = window;
            this.screenShot = screenShot;
        }

        public TradeResult Run() {
            OpenTradeWindow();
            try {
                // アイテムをダブルクリック
                var position = FindTradeItem( Resource.beast_blood );
                window.DoubleClick( position );

                // エントリーボタンを押す
                var entryButtonPosition = window.GetTradeWindowEntryButtonPosition();
                window.DoubleClick( entryButtonPosition );
                Thread.Sleep( 500 );
                var inventryErrorDialogGeometry = window.GetTradeErrorDialogGeometry();
                var inventryErrorDialog = (Bitmap)window.CaptureWindow( inventryErrorDialogGeometry );
                if( ImageComparator.Compare( inventryErrorDialog, Resource.trade_error_dialog ) ) {
                    // 相手のカバンがいっぱいの場合ダイアログが出るので，閉じてキャンセルする
                    var inventryErrorDialogOKButtonPosition = window.GetTradeErrorDialogOKButtonPosition();
                    window.Click( inventryErrorDialogOKButtonPosition );
                    Bitmap screenShot = window.CaptureWindow();
                    Thread.Sleep( 200 );
                    CloseTradeWindow();
                    return new TradeResult( TradeResult.StatusType.INVENTRY_NO_SPACE, DateTime.Now, screenShot, "" );
                } else {
                    // トレードウィンドウを閉じ，トレードを終了する
                    var tradeWindowFinalizer = new TradeWinowFinalizer( window );
                    Thread thread = new Thread( new ThreadStart( tradeWindowFinalizer.Run ) );
                    thread.Start();

                    // 最大 WAIT_SECONDS 秒間，スレッドが終了していないかチェックし，終了していれば処理を続行する
                    const int WAIT_SECONDS = 10;
                    const int WAIT_UNIT_MILLI_SECONDS = 200;
                    for( var i = 0; i < WAIT_SECONDS * 1000 / WAIT_UNIT_MILLI_SECONDS; i++ ) {
                        Thread.Sleep( TimeSpan.FromMilliseconds( WAIT_UNIT_MILLI_SECONDS ) );
                        if( !thread.IsAlive ) {
                            break;
                        }
                    }
                    var lastScreenShot = tradeWindowFinalizer.getLastScreenShot();

                    if( thread.IsAlive ) {
                        // WAIT_SECONDS 秒間処理してもトレードウィンドウが閉じていない場合，
                        // キャンセルボタンを押してトレードを中断する
                        thread.Abort();
                        CloseTradeWindow();
                        return new TradeResult( TradeResult.StatusType.FAILED, DateTime.Now, lastScreenShot, "" );
                    } else {
                        // トレードが成功
                        // インベントリを開いて，ソートする
                        var itemButtonPosition = window.GetItemButtonPosition();
                        window.Click( itemButtonPosition );
                        Thread.Sleep( TimeSpan.FromSeconds( 2 ) );

                        var sortButtonPosition = window.GetInventorySortButtonPosition();
                        window.Click( sortButtonPosition );
                        Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );

                        var closeButtonPosition = window.GetInventoryCloseButtonPosition();
                        window.Click( closeButtonPosition );
                        Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
                        return new TradeResult( TradeResult.StatusType.SUCCEEDED, DateTime.Now, lastScreenShot, "" );
                    }
                }
            } catch( ApplicationException e ) {
                var screenShot = window.CaptureWindow();
                CloseTradeWindow();
                return new TradeResult( TradeResult.StatusType.FAILED, DateTime.Now, screenShot, e.Message );
            }
        }

        /// <summary>
        /// トレード要請のアイコンをクリックすることで，トレード画面を開く
        /// </summary>
        /// <param name="screenShot"></param>
        private void OpenTradeWindow() {
            var iconArea = window.GetIconAreaRectangle();
            int x = iconArea.Left + iconArea.Width / 2;
            int y = iconArea.Top + iconArea.Height / 2;
            var position = new Point( x, y );
            window.Click( position );
            Thread.Sleep( 3000 );
        }

        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        private void CloseTradeWindow() {
            Point cancelButtonPosition = window.GetTradeWindowCancelButtonPosition();
            window.Click( cancelButtonPosition );
        }

        /// <summary>
        /// トレード画面の自分側のアイテム一覧から，指定された画像に一致するアイテムを探す
        /// </summary>
        /// <param name="itemTemplate"></param>
        /// <returns></returns>
        private Point FindTradeItem( Bitmap itemTemplate ) {
            var screenShot = window.CaptureWindow();
            foreach( Rectangle geometry in window.GetTradeItemGeometryEnumerator() ) {
                var item = (Bitmap)screenShot.Clone( geometry, screenShot.PixelFormat );
                if( ImageComparator.Compare( item, itemTemplate ) ) {
                    return new Point( geometry.Left + geometry.Width / 2, geometry.Top + geometry.Height / 2 );
                }
            }
            throw new ApplicationException( "アイテムを見つけられなかった" );
        }
    }
}
