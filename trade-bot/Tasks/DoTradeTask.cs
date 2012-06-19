using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace FEZTradeBot {
    /// <summary>
    /// トレードを実行するタスク
    /// </summary>
    class DoTradeTask : IDisposable {
        private FEZWindow window;

        public DoTradeTask( FEZWindow window ) {
            this.window = window;
        }

        public TradeResult Run() {
            OpenTradeWindow();
            var initialTradeWindow = window.CaptureWindow();

            // 何も入っていないアイテム枠の画像を取得する
            var emptyItemSlot = GetEmptyItemSlot();

            // トレードで渡すアイテムの位置を取得する
            Point position = Point.Empty;
            try {
                position = FindTradeItem( Resource.beast_blood );
            } catch( FEZBotException e ) {
            }
            if( position == Point.Empty ) {
                CloseTradeWindow();
                return new TradeResult( TradeResult.StatusType.SOLD_OUT, initialTradeWindow );
            }

            try {
                // アイテムをダブルクリック
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
                    return new TradeResult( TradeResult.StatusType.INVENTRY_NO_SPACE, screenShot );
                } else {
                    // トレードウィンドウを閉じ，トレードを終了する
                    var tradeWindowFinalizer = new TradeWinowFinalizer( window, emptyItemSlot );
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
                    var lastScreenShot = tradeWindowFinalizer.LastScreenShot;
                    var weiredItemEntried = tradeWindowFinalizer.IsWeiredItemEntried;

                    if( thread.IsAlive ) {
                        // WAIT_SECONDS 秒間処理してもトレードウィンドウが閉じていない場合，
                        // キャンセルボタンを押してトレードを中断する
                        thread.Abort();
                        CloseTradeWindow();
                        return new TradeResult( TradeResult.StatusType.FAILED, lastScreenShot );
                    } else if( weiredItemEntried ) {
                        CloseTradeWindow();
                        return new TradeResult( TradeResult.StatusType.WEIRED_ITEM_ENTRIED, lastScreenShot );
                    } else {
                        if( tradeWindowFinalizer.LastScreenShot == null ) {
                            // こちらが決定ボタンを押す直前のスクリーンショットが取れなかった
                            // つまり，相手がキャンセルボタンを押したためトレードウィンドウが閉じられた
                            return new TradeResult( TradeResult.StatusType.CANCELLED_BY_CUSTOMER, initialTradeWindow );
                        } else {
                            // トレードが成功
                            return new TradeResult( TradeResult.StatusType.SUCCEEDED, lastScreenShot );
                        }
                    }
                }
            } catch( FEZBotException e ) {
                var screenShot = window.CaptureWindow();
                CloseTradeWindow();
                return new TradeResult( TradeResult.StatusType.FAILED, screenShot );
            }
        }

        public void Dispose() {
            this.window.Dispose();
        }

        /// <summary>
        /// トレード要請を受諾し、トレード画面を開く。
        /// トレード要請のアイコンとPT要請のアイコンは同じ位置に出るので、タイミングによっては
        /// PT要請のアイコンを押してしまう可能性がある。このため、可能な限りキーボードから
        /// 操作するようにした。
        /// </summary>
        private void OpenTradeWindow() {
            VMultiKeyboardClient client = null;
            try {
                client = new VMultiKeyboardClient();
            } catch( FEZBotException e ) {
                Console.Error.WriteLine( e.Message );
            }

            if( client == null ) {
                var iconArea = window.GetIconAreaRectangle();
                int x = iconArea.Left + iconArea.Width / 2;
                int y = iconArea.Top + iconArea.Height / 2;
                var position = new Point( x, y );
                window.Click( position );
            } else {
                client.ClearKey();
                window.Activate();
                client.SetKey( (byte)'T' );
                Thread.Sleep( TimeSpan.FromMilliseconds( 50 ) );
                client.ClearKey();
                client.Dispose();
            }
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
            throw new FEZBotException( "アイテムを見つけられなかった" );
        }

        /// <summary>
        /// トレード相手側のエントリー枠のうち，何も入っていない枠の画像を取得する
        /// 現在は，このメソッドがトレード枠開いた直後に呼ばれる前提とし，相手側エントリー枠の右下の画像を取得する
        /// </summary>
        /// <returns></returns>
        private Bitmap GetEmptyItemSlot() {
            var rightBottomItemSlot = Rectangle.Empty;
            foreach( var geometry in window.GetTradeCustomerEntriedItemGeometryEnumerator() ) {
                rightBottomItemSlot = geometry;
            }
            return window.CaptureWindow( rightBottomItemSlot );
        }
    }
}
