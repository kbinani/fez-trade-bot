using System;
using System.Drawing;
using System.Threading;
using System.Drawing.Imaging;

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

        public void Run() {
            OpenTradeWindow();
            try {
                Point position = FindTradeItem( Resource.beast_blood );

                // アイテムをダブルクリック
                //TODO:

                // エントリーボタンを押す
                //TODO:

                // 決定ボタンがハイライト状態になるまで，エントリーボタンを押し続ける
                //TODO:

                // トレードウィンドウが消えるまで，決定ボタンを押し続ける
                //TODO:
            } catch( ApplicationException e ) {
                CloseTradeWindow();
            }
        }

        /// <summary>
        /// トレード要請のアイコンをクリックすることで，トレード画面を開く
        /// </summary>
        /// <param name="screenShot"></param>
        private void OpenTradeWindow() {
            Rectangle iconArea = window.GetIconAreaRectangle();
            window.Click(
                iconArea.Left + iconArea.Width / 2,
                iconArea.Top + iconArea.Height / 2
            );
            Thread.Sleep( 3000 );
        }

        /// <summary>
        /// ウィンドウを閉じる
        /// </summary>
        private void CloseTradeWindow() {
            Point cancelButtonPosition = window.GetTradeWindowCancelButtonPosition();
            window.Click( cancelButtonPosition.X, cancelButtonPosition.Y );
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
