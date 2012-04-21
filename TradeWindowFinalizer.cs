using System;
using System.Threading;
using System.Drawing;

namespace com.github.kbinani.feztradenotify {
    /// <summary>
    /// トレードウィンドウで，アイテムをエントリーした後ウィンドウを閉じるまでの処理を行う
    /// </summary>
    class TradeWinowFinalizer {
        private FEZWindow window;
        private Bitmap screenShot = null;

        public TradeWinowFinalizer( FEZWindow window ) {
            this.window = window;
        }

        /// <summary>
        /// トレード画面を閉じる処理を行う
        /// </summary>
        /// <returns></returns>
        public void Run() {
            var okButtonGeometry = window.GetTradeWindowOkButtonPosition();
            var tradeWindowGeometry = window.GetTradeWindowGeometry();
            while( true ) {
                // トレードウィンドウが消えるまで，決定ボタンを押し続ける
                var captured = window.CaptureWindow();
                var tradeWindow = (Bitmap)captured.Clone( tradeWindowGeometry, captured.PixelFormat );
                if( !ImageComparator.Compare( tradeWindow, Resource.trade_window ) ) {
                    break;
                }

                this.screenShot = captured;
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
                //TODO: トレード相手が変なアイテム渡してきてないか確認する
                window.Click( okButtonGeometry );
            }
        }

        /// <summary>
        /// 完了ボタンをクリックする直前のスクリーンショット画像を返す
        /// </summary>
        /// <returns></returns>
        public Bitmap getLastScreenShot() {
            return this.screenShot;
        }
    }
}
