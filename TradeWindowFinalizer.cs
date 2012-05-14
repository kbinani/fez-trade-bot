using System;
using System.Threading;
using System.Drawing;

namespace FEZTradeBot {
    /// <summary>
    /// トレードウィンドウで，アイテムをエントリーした後ウィンドウを閉じるまでの処理を行う
    /// </summary>
    class TradeWinowFinalizer {
        private FEZWindow window;
        private Bitmap screenShot = null;
        private Bitmap emptyItemSlot;
        private bool weiredItemEntried = false;

        /// <summary>
        /// 初期化する
        /// </summary>
        /// <param name="window"></param>
        /// <param name="emptySlot">相手側エントリー枠の，空の状態の枠の画像</param>
        public TradeWinowFinalizer( FEZWindow window, Bitmap emptyItemSlot ) {
            this.window = window;
            this.emptyItemSlot = emptyItemSlot;
        }

        public void Run(){
            var tradeWindowGeometry = window.GetTradeWindowGeometry();

            // 「決定」ボタンがENABLE状態になるまで待機
            var captured = window.CaptureWindow();
            var okButtonGeometry = window.GetTradeWindowOkButtonGeometry();
            var okButtonFound = false;
            while( HasTradeWindow( captured ) ){
                Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );
                var okButtonImage = captured.Clone( okButtonGeometry, captured.PixelFormat );
                if( ImageComparator.Compare( okButtonImage, Resource.trade_ok_active ) ) {
                    okButtonFound = true;
                    break;
                }
                captured = window.CaptureWindow();
            }
            if( !okButtonFound ) {
                return;
            }

            // トレード相手が変なアイテム渡してきてないか確認する
            foreach( var geometry in window.GetTradeCustomerEntriedItemGeometryEnumerator() ) {
                var itemSlot = (Bitmap)captured.Clone( geometry, captured.PixelFormat );
                if( !ImageComparator.Compare( itemSlot, emptyItemSlot ) && !ImageComparator.Compare( itemSlot, Resource.beast_blood ) ) {
                    weiredItemEntried = true;
                    screenShot = captured;
                    return;
                }
            }

            // 「決定」ボタンを押す
            screenShot = captured;
            int x = okButtonGeometry.Left + okButtonGeometry.Width / 2;
            int y = okButtonGeometry.Top + okButtonGeometry.Height / 2;
            window.Click( new Point( x, y ) );

            // トレードウィンドウが消えるまで待つ
            while( HasTradeWindow( window.CaptureWindow() ) ) {
                Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );
            }
        }

        /// <summary>
        /// トレードウィンドウが表示されているかどうかを判定する
        /// </summary>
        private bool HasTradeWindow( Bitmap screenShot ){
            var tradeWindowGeometry = window.GetTradeWindowGeometry();
            var tradeWindow = (Bitmap)screenShot.Clone( tradeWindowGeometry, screenShot.PixelFormat );
            return ImageComparator.Compare( tradeWindow, Resource.trade_window );
        }

        /// <summary>
        /// 完了ボタンをクリックする直前のスクリーンショット画像を返す
        /// </summary>
        /// <returns></returns>
        public Bitmap LastScreenShot {
            get{
                return this.screenShot;
            }
        }

        /// <summary>
        /// トレード相手が野獣の血以外のアイテムを渡してきた場合 true を返す
        /// </summary>
        public bool IsWeiredItemEntried {
            get {
                return this.weiredItemEntried;
            }
        }
    }
}
