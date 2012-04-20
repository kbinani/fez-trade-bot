using System;
using System.Drawing;

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
            OpenTradeWindow( screenShot );
        }

        /// <summary>
        /// トレード要請のアイコンをクリックすることで，トレード画面を開く
        /// </summary>
        /// <param name="screenShot"></param>
        private void OpenTradeWindow( Bitmap screenShot ) {
            Rectangle iconArea = window.GetIconAreaRectangle( screenShot );
            window.Click(
                iconArea.Left + iconArea.Width / 2,
                iconArea.Top + iconArea.Height / 2
            );
        }
    }
}
