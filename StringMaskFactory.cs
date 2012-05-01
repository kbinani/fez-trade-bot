using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// トレードウィンドウの，トレード相手の名前を調べる際に使用するマスク画像を作成する
    /// </summary>
    class StringMaskFactory {
        public static Bitmap GetEmpty( int index, int width ) {
            var result = new Bitmap(
                FEZWindow.TRADE_WINDOW_CUSTOMER_GEOMETRY_WIDTH,
                FEZWindow.TRADE_WINDOW_CUSTOMER_GEOMETRY_HEIGHT,
                PixelFormat.Format24bppRgb
            );
            int clipX = 6 + index * 6 + 1;
            using( var graphics = Graphics.FromImage( result ) ) {
                graphics.FillRectangle(
                    new SolidBrush( Color.FromArgb( 255, 0, 255 ) ),
                    0, 0, result.Width, result.Height
                );
                graphics.FillRectangle(
                    Brushes.White,
                    clipX, 0, width * 6, FEZWindow.TRADE_WINDOW_CUSTOMER_GEOMETRY_HEIGHT
                );
            }
            return result;
        }

        /// <summary>
        /// 位置文字分のマスク画像を作る
        /// </summary>
        /// <param name="c"></param>
        /// <param name="index">左から何バイト目の文字であるかを指定する</param>
        /// <param name="howManyBytes">文字がshift_jisに変換した時何バイトか</param>
        /// <returns></returns>
        public static Bitmap Get( char c, int index, out int howManyBytes ) {
            howManyBytes = Encoding.GetEncoding( "Shift_JIS" ).GetEncoder().GetByteCount( new char[] { c }, 0, 1, true );
            int drawX = 6 + index * 6;
            int drawY = 2;

            int clipX = drawX + 1;
            int width = 6 * howManyBytes;
            var result = GetEmpty( index, howManyBytes );
            using( var graphics = Graphics.FromImage( result ) ) {
                graphics.DrawString(
                    new string( c, 1 ), new Font( "ＭＳ ゴシック", 9 ), Brushes.Black,
                    drawX, drawY
                );
            }

            return result;
        }
    }
}
