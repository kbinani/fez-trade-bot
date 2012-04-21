using System;
using System.Threading;
using System.Drawing;

namespace com.github.kbinani.feztradenotify {
    /// <summary>
    /// トレードウィンドウで，アイテムをエントリーした後ウィンドウを閉じるまでの処理を行う
    /// </summary>
    class TradeWinowFinalizer {
        private FEZWindow window;

        public TradeWinowFinalizer( FEZWindow window ) {
            this.window = window;
        }

        public void Run() {
            var okButtonGeometry = window.GetTradeWindowOkButtonPosition();
            var tradeWindowGeometry = window.GetTradeWindowGeometry();
            while( true ) {
                // トレードウィンドウが消えるまで，決定ボタンを押し続ける
                var tradeWindow = (Bitmap)window.CaptureWindow( tradeWindowGeometry );
                if( !ImageComparator.Compare( tradeWindow, Resource.trade_window ) ) {
                    break;
                }

                window.Click( okButtonGeometry.X, okButtonGeometry.Y );
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            }
        }
    }
}
