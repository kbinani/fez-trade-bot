using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// トレードウィンドウの，トレード相手の名前を調べる際に使用するマスク画像を作成する
    /// </summary>
    class StringMaskFactory {
        /// <summary>
        /// 文字の幅（ピクセル単位）
        /// </summary>
        public const int CHARACTER_WIDTH = 6;

        private static System.Text.Encoder encoder = null;
        private static Font font = null;

        public static void ResetMask( int index, int width, Bitmap result ) {
            int clipX = 6 + index * CHARACTER_WIDTH + 1;
            using( var graphics = Graphics.FromImage( result ) ) {
                graphics.FillRectangle(
                    new SolidBrush( Color.FromArgb( 255, 0, 255 ) ),
                    0, 0, result.Width, result.Height
                );
                graphics.FillRectangle(
                    Brushes.White,
                    clipX, 0, width * CHARACTER_WIDTH, FEZWindow.TRADE_WINDOW_CUSTOMER_GEOMETRY_HEIGHT
                );
            }
        }

        /// <summary>
        /// 位置文字分のマスク画像を作る
        /// </summary>
        /// <param name="c"></param>
        /// <param name="index">左から何バイト目の文字であるかを指定する</param>
        /// <param name="howManyBytes">文字がshift_jisに変換した時何バイトか</param>
        /// <returns></returns>
        public static void DrawMask( char c, int index, Bitmap result ) {
            int drawX = 6 + index * CHARACTER_WIDTH;
            int drawY = 2;

            int clipX = drawX + 1;
            //int width = CHARACTER_WIDTH * howManyBytes;
            using( var graphics = Graphics.FromImage( result ) ) {
                graphics.DrawString(
                    new string( c, 1 ), GetFont(), Brushes.Black,
                    drawX, drawY
                );
            }
        }

        /// <summary>
        /// 指定した文字が半角文字かどうかを取得する
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsHalfWidthCharacter( char c ) {
            return GetEncoder().GetByteCount( new char[] { c }, 0, 1, true ) == 1;
        }

        private static System.Text.Encoder GetEncoder() {
            if( encoder == null ) {
                encoder = Encoding.GetEncoding( "Shift_JIS" ).GetEncoder();
            }
            return encoder;
        }

        private static Font GetFont() {
            if( font == null ) {
                font = new Font( "ＭＳ ゴシック", 9 );
            }
            return font;
        }
    }
}
