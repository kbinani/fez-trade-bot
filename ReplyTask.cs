using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// トレード相手に個人チャット(/tell)を送るタスク
    /// </summary>
    class ReplyTask {
        private FEZWindow window;
        private TradeResult tradeResult;

        public ReplyTask( FEZWindow window, TradeResult tradeResult ) {
            this.window = window;
            this.tradeResult = tradeResult;
        }

        public void Run() {
            if( tradeResult.Status != TradeResult.StatusType.INVENTRY_NO_SPACE &&
                tradeResult.Status != TradeResult.StatusType.SUCCEEDED &&
                tradeResult.Status != TradeResult.StatusType.WEIRED_ITEM_ENTRIED
            ) {
                return;
            }

            var customerNameImage = GetCustomerNameImage( tradeResult.ScreenShot );
            string targetName = "";
            try {
                targetName = GetCustomerName( customerNameImage );
            } catch( ApplicationException e ) {
                Console.WriteLine( e.Message );
            }

            {//TODO:
                Console.WriteLine( "targetName=" + targetName );
            }
        }

        private string GetCustomerName( Bitmap customerNameImage ) {
            string result = "";
            int index = 0;
            while( index < 16 ) {
                int howManyBytes = 0;
                char found = '\0';

                if( ImageComparator.Compare( customerNameImage, StringMaskFactory.GetEmpty( index, 2 ), 0 ) &&
                    ImageComparator.Compare( customerNameImage, StringMaskFactory.GetEmpty( index, 1 ), 0 ) ) {
                    break;
                }

                foreach( var c in ShiftJISCharacterEnumerator.GetEnumerator() ) {
                    var mask = StringMaskFactory.Get( c, index, out howManyBytes );
                    if( ImageComparator.Compare( customerNameImage, mask, 0 ) ) {
                        found = c;
                        index += howManyBytes;
                        break;
                    }
                }

                if( found == '\0' ) {
                    throw new ApplicationException( "画像からキャラクタ名を推定できなかった; result=" + result );
                } else {
                    result += new string( found, 1 );
                }
            }

            return result;
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
