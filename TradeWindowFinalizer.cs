using System;
using System.Threading;
using System.Drawing;

namespace com.github.kbinani.feztradebot {
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

        /// <summary>
        /// トレード画面を閉じる処理を行う
        /// </summary>
        /// <returns></returns>
        public void Run() {
            var okButtonGeometry = window.GetTradeWindowOkButtonPosition();
            var tradeWindowGeometry = window.GetTradeWindowGeometry();
            while( true ) {
                Thread.Sleep( TimeSpan.FromMilliseconds( 500 ) );

                // トレードウィンドウが消えるまで，決定ボタンを押し続ける
                var captured = window.CaptureWindow();
                var tradeWindow = (Bitmap)captured.Clone( tradeWindowGeometry, captured.PixelFormat );
                if( !ImageComparator.Compare( tradeWindow, Resource.trade_window ) ) {
                    break;
                }
                this.screenShot = captured;

                // トレード相手が変なアイテム渡してきてないか確認する
                foreach( var geometry in window.GetTradeCustomerEntriedItemGeometryEnumerator() ) {
                    var itemSlot = (Bitmap)captured.Clone( geometry, captured.PixelFormat );
                    if( !ImageComparator.Compare( itemSlot, emptyItemSlot ) && !ImageComparator.Compare( itemSlot, Resource.beast_blood ) ) {
                        weiredItemEntried = true;
                        return;
                    }
                }

                window.Click( okButtonGeometry );
            }
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
