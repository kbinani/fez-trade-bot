using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// トレード相手に個人チャット(/tell)を送るタスク
    /// </summary>
    class ReplyTask {
        private FEZWindow window;
        private TradeResult tradeResult;
        private RuntimeSettings settings;

        public ReplyTask( FEZWindow window, TradeResult tradeResult, RuntimeSettings settings ) {
            this.window = window;
            this.tradeResult = tradeResult;
            this.settings = settings;
        }

        public void Run() {
            var customerNameImage = GetCustomerNameImage( tradeResult.ScreenShot );
            SendLogMessage( customerNameImage );

            if( tradeResult.Status == TradeResult.StatusType.INVENTRY_NO_SPACE ||
                tradeResult.Status == TradeResult.StatusType.SUCCEEDED ||
                tradeResult.Status == TradeResult.StatusType.WEIRED_ITEM_ENTRIED
            ) {
                SendThanksMessage( customerNameImage );
            }
        }

        /// <summary>
        /// 来店ログを自キャラに送信
        /// </summary>
        /// <param name="customerNameImage"></param>
        private void SendLogMessage( Bitmap customerNameImage ) {
            var customerName = "";
            try {
                customerName = TextFinder.FuzzyFind( customerNameImage );
            } catch( ApplicationException e ) {
                Console.WriteLine( e.Message );
                return;
            }

            var mode = "STRICT";
            try {
                TextFinder.Find( customerNameImage );
            } catch( ApplicationException e ) {
                mode = "FUZZY";
            }

            if( settings.AdminPC != "" ) {
                string adminMessage = "/tell " + settings.AdminPC + " " + customerName + " さんが来店, status: " + tradeResult.Status + ", mode: " + mode;
                SendMessage( adminMessage );
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }
        }

        /// <summary>
        /// 取引相手にメッセージを送る
        /// </summary>
        /// <param name="customerNameImage"></param>
        private void SendThanksMessage( Bitmap customerNameImage ) {
            string customerName = "";
            try {
                customerName = TextFinder.Find( customerNameImage );
            } catch( ApplicationException e ) {
                Console.WriteLine( e.Message );
                return;
            }
            if( settings.AdminPC != "" && settings.AdminPC == customerName ) {
                // 自分自身なので個人チャットしなくてよい
                return;
            }

            string statusMessage = "";
            switch( tradeResult.Status ) {
                case TradeResult.StatusType.SUCCEEDED: {
                    statusMessage = settings.TellMessageSucceeded;
                    break;
                }
                case TradeResult.StatusType.INVENTRY_NO_SPACE: {
                    statusMessage = settings.TellMessageInventoryNoSpace;
                    break;
                }
                case TradeResult.StatusType.WEIRED_ITEM_ENTRIED: {
                    statusMessage = settings.TellMessageWeiredItemEntried;
                    break;
                }
            }
            if( statusMessage == "" ) {
                return;
            }

            string message = "/tell " + customerName + " " + statusMessage;
            SendMessage( message );
            Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
        }

        /// <summary>
        /// チャットメッセージを送信する．
        /// </summary>
        /// <param name="message"></param>
        private void SendMessage( string message ) {
            EnableChatTextbox();

            Clipboard.SetText( message );
            WindowsAPI.keybd_event( WindowsAPI.VK_CONTROL, 0, 0, UIntPtr.Zero );
            WindowsAPI.keybd_event( (byte)'V', 0, 0, UIntPtr.Zero );
            WindowsAPI.keybd_event( WindowsAPI.VK_CONTROL, 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );
            WindowsAPI.keybd_event( (byte)'V', 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );

            WindowsAPI.keybd_event( WindowsAPI.VK_RETURN, 0, 0, UIntPtr.Zero );
            WindowsAPI.keybd_event( WindowsAPI.VK_RETURN, 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );
        }

        /// <summary>
        /// チャット入力欄を有効化する．
        /// </summary>
        private void EnableChatTextbox() {
            WindowsAPI.SendMessage( window.Handle, WindowsAPI.WM_KEYDOWN, WindowsAPI.VK_RETURN, 0 );
            WindowsAPI.SendMessage( window.Handle, WindowsAPI.WM_KEYUP, WindowsAPI.VK_RETURN, 0 );
            for( int i = 0; i < 6; i++ ){
                WindowsAPI.keybd_event( WindowsAPI.VK_BACK_SPACE, 0, 0, UIntPtr.Zero );
                WindowsAPI.keybd_event( WindowsAPI.VK_BACK_SPACE, 0, WindowsAPI.KEYEVENTF_KEYUP, UIntPtr.Zero );
            }
        }

        /// <summary>
        /// ゲーム画面全体のスクリーンショットから，トレードウィンドウに表示されたトレード相手の名前部分の画像を取得する
        /// 黒いピクセルのみ取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        private Bitmap GetCustomerNameImage( Bitmap screenShot ) {
            var customerNameGeometry = window.GetTradeWindowCustomerNameGeometry();
            var result = screenShot.Clone(
                customerNameGeometry,
                screenShot.PixelFormat );

            var letterColor = Color.FromArgb( 255, 0, 0, 0 );
            for( int y = 0; y < result.Height; y++ ) {
                for( int x = 0; x < result.Width; x++ ) {
                    Color color = Color.FromArgb( 255, result.GetPixel( x, y ) );
                    if( color == letterColor ) {
                        result.SetPixel( x, y, letterColor );
                    } else {
                        result.SetPixel( x, y, Color.White );
                    }
                }
            }

            return result;
        }
    }
}
