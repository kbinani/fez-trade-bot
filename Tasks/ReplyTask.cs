using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows.Forms;
using System.IO;

namespace FEZTradeBot {
    /// <summary>
    /// トレード相手に個人チャット(/tell)を送るタスク
    /// </summary>
    class ReplyTask {
        private FEZWindow window;
        private TradeResult tradeResult;
        private RuntimeSettings settings;
        /// <summary>
        /// チャットウィンドウが、半角で最大何文字分表示されるか
        /// </summary>
        private const int CHAT_LINE_WIDTH = 52;

        public ReplyTask( FEZWindow window, TradeResult tradeResult, RuntimeSettings settings ) {
            this.window = window;
            this.tradeResult = tradeResult;
            this.settings = settings;
        }

        public void Run() {
            var customerNameImage = GetCustomerNameImage( tradeResult.ScreenShot );
            string strictCustomerName;
            string fuzzyCustomerName;
            GetCustomerName( customerNameImage, out strictCustomerName, out fuzzyCustomerName );
            var isStrict = strictCustomerName != "";

            if( (tradeResult.Status == TradeResult.StatusType.INVENTRY_NO_SPACE ||
                tradeResult.Status == TradeResult.StatusType.SUCCEEDED ||
                tradeResult.Status == TradeResult.StatusType.WEIRED_ITEM_ENTRIED) &&
                isStrict
            ) {
                SendThanksMessage( strictCustomerName );
            }

            if( strictCustomerName != "" || fuzzyCustomerName != "" ) {
                var customerName = (strictCustomerName != "") ? strictCustomerName : fuzzyCustomerName;
                SendLogMessage( customerName, isStrict );
            }
        }

        /// <summary>
        /// 来店ログを自キャラに送信
        /// </summary>
        /// <param name="customerName"></param>
        /// <param name="isStrict"
        private void SendLogMessage( string customerName, bool isStrict ) {
            if( settings.AdminPC != "" ) {
                string[] lines = new string[] {
                    customerName + " さんが来店",
                    "status: " + tradeResult.Status,
                    "mode: " + (isStrict ? "STRICT" : "FUZZY")
                };
                string adminMessage = GetFormattedTellMessage( lines, settings.LoginCharacterName, settings.AdminPC );
                window.SendMessage( adminMessage );
                string message = "";
                foreach( var line in lines ){
                    if( message != "" ){
                        message += ", ";
                    }
                    message += line;
                }
                Irc.SendMessage( message );
                Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
            }
        }

        /// <summary>
        /// 取引相手にメッセージを送る
        /// </summary>
        /// <param name="customerName"></param>
        private void SendThanksMessage( string customerName ) {
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
            window.SendMessage( message );
            Thread.Sleep( TimeSpan.FromSeconds( 1 ) );
        }

        /// <summary>
        /// ゲーム画面全体のスクリーンショットから，トレードウィンドウに表示されたトレード相手の名前部分の画像を取得する
        /// 黒いピクセルのみ取り出す
        /// </summary>
        /// <param name="screenShot"></param>
        /// <returns></returns>
        private Bitmap GetCustomerNameImage( Bitmap screenShot ) {
            if( screenShot == null ) {
                return null;
            }
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

        /// <summary>
        /// 文字列の判定に失敗したものをログに残す
        /// </summary>
        /// <param name="customerNameImage"></param>
        private void WriteLog( Bitmap customerNameImage, Bitmap detectedResult = null ) {
            var directory = Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "reply_task" );
            if( !Directory.Exists( directory ) ) {
                Directory.CreateDirectory( directory );
            }

            string fileName = Path.GetRandomFileName();
            customerNameImage.Save( Path.Combine( directory, fileName + ".source.png" ), ImageFormat.Png );
            if( detectedResult != null ) {
                detectedResult.Save( Path.Combine( directory, fileName + ".detected.png" ), ImageFormat.Png );
            }
        }

        private void GetCustomerName( Bitmap customerNameImage, out string strictCustomerName, out string fuzzyCustomerName ) {
            strictCustomerName = "";
            fuzzyCustomerName = "";

            try {
                strictCustomerName = TextFinder.Find( customerNameImage );
            } catch( ApplicationException e ) {
            }

            // 検出結果を描画し、同じになってるか確認する
            var image = (Bitmap)customerNameImage.Clone();
            using( var g = Graphics.FromImage( image ) ) {
                g.FillRectangle( new SolidBrush( Color.FromArgb( 255, Color.White ) ), 0, 0, image.Width, image.Height );
                g.DrawString(
                    strictCustomerName, TextFinder.GetFont(), new SolidBrush( Color.FromArgb( 255, Color.Black ) ),
                    TextFinder.DRAW_OFFSET_X, TextFinder.DRAW_OFFSET_Y
                );
            }
            image.SetPixel( 0, 0, Color.FromArgb( 255, Color.White ) );
            if( !ImageComparator.Compare( customerNameImage, image, 0 ) ) {
                WriteLog( customerNameImage, image );
            }

            if( strictCustomerName == "" ) {
                try {
                    fuzzyCustomerName = TextFinder.FuzzyFind( customerNameImage );
                } catch( ApplicationException e ) {
                }
            }

            if( strictCustomerName == "" && fuzzyCustomerName == "" ) {
                WriteLog( customerNameImage );
            }
        }

        /// <summary>
        /// メッセージが相手に届いた際、メッセージの各行がきれいに改行されるよう、フォーマットする
        /// </summary>
        /// <param name="lines"></param>
        /// <param name="myName"></param>
        /// <param name="targetName"></param>
        /// <returns>"/tell {targetName} ～"などに整形された文字列</returns>
        public static string GetFormattedTellMessage( string[] lines, string myName, string targetName ) {
            // チャットは、相手に届いた際に、"{myName} <<: "という接頭辞が付く
            int prefixLength = 0;
            string prefix = myName + " <<: ";
            foreach( var c in prefix.ToCharArray() ) {
                prefixLength += (TextFinder.IsHalfWidthCharacter( c ) ? 1 : 2);
            }

            // チャットに表示されるのは、一行あたり半角で52文字
            string result = "/tell " + targetName + " ";
            foreach( var line in lines ) {
                result += GetLineMessage( line, prefixLength );
            }
            return result;
        }

        private static string GetLineMessage( string line, int prefixLength ) {
            int remain = line.Length;
            string result = "";
            while( 0 < remain ) {
                int lineRemain = CHAT_LINE_WIDTH - prefixLength;
                var count = 0;
                for( int i = 0; i < line.Length; i++ ){
                    var c = line[i];
                    int width = TextFinder.IsHalfWidthCharacter( c ) ? 1 : 2;
                    if( lineRemain < width ) {
                        break;
                    }
                    result += c;
                    count = i + 1;
                    lineRemain -= width;
                }
                line = line.Substring( count );
                remain = line.Length;

                while( 1 < lineRemain ) {
                    result += "　";
                    lineRemain -= 2;
                }
                while( 0 < lineRemain ) {
                    result += " ";
                    lineRemain -= 1;
                }
            }
            return result;
        }
    }
}
