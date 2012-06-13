using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace FEZTradeBot {
    public static class TextFinder {
        /// <summary>
        /// 描画する際のx方向のオフセット
        /// </summary>
        public const int DRAW_OFFSET_X = -2;

        /// <summary>
        /// 描画する際のy方向のオフセット
        /// </summary>
        public const int DRAW_OFFSET_Y = 0;

        public const int CHARACTER_WIDTH = 6;
        public const int CHARACTER_HEIGHT = 12;
        private static readonly string FullWidthEmpty = new string( '0', 36 );
        private static readonly string HalfWidthEmpty = new string( '0', 18 );

        private static Dictionary<string, char> map;
        private static System.Text.Encoder encoder;
        private static Dictionary<string, char> dupulicatedKeys;
        private static Font font;

        /// <summary>
        /// 画像から文字を探す。画像の高さは12ピクセルである必要がある。描かれている文字は座標(0,0)から始まり、
        /// 文字の高さも12ピクセルでなければならない。
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string Find( Bitmap image ) {
            return Find( image, false, true );
        }

        public static string FuzzyFind( Bitmap image ) {
            return Find( image, true, true );
        }

        public static void Initialize() {
            map = new Dictionary<string, char>();
            dupulicatedKeys = new Dictionary<string, char>();

            var image = new Bitmap( 12, 12, PixelFormat.Format24bppRgb );
            using( var g = Graphics.FromImage( image ) ) {
                var font = GetFont();
                foreach( var c in ShiftJISCharacterEnumerator.GetEnumerator() ) {
                    var isHalfWidth = IsHalfWidthCharacter( c );
                    g.FillRectangle( Brushes.White, 0, 0, 12, 12 );
                    if( isHalfWidth ) {
                        g.DrawString( new string( c, 1 ), font, Brushes.Black, DRAW_OFFSET_X, DRAW_OFFSET_Y );
                    } else {
                        g.DrawString( new string( c, 1 ), font, Brushes.Black, DRAW_OFFSET_X, DRAW_OFFSET_Y );
                    }

                    string key = GetKey( image, 0 );
                    if( !isHalfWidth ) {
                        key += GetKey( image, CHARACTER_WIDTH );
                    }

                    if( key == HalfWidthEmpty || key == FullWidthEmpty ) {
                        continue;
                    }

                    if( map.ContainsKey( key ) ) {
                        if( !dupulicatedKeys.ContainsKey( key ) ) {
                            dupulicatedKeys.Add( key, map[key] );
                        }
                        map[key] = '\0';
                    } else {
                        map.Add( key, c );
                    }
                }
            }

            // nul文字が入っていれば削除
            string foundKey = "dummy";
            while( foundKey != "" ) {
                foundKey = "";
                foreach( var key in map.Keys ) {
                    if( map[key] == '\0' ) {
                        foundKey = key;
                        break;
                    }
                }
                map.Remove( foundKey );
            }
        }

        public static string Find( Bitmap image, bool isFuzzy, bool ignoreWhiteSpace ) {
            if( map == null || dupulicatedKeys == null ) {
                Initialize();
            }

            int textCount = image.Width / CHARACTER_WIDTH;

            string[] keys = new string[textCount];
            string[] accepted = new string[textCount];
            for( int i = 0; i < textCount; i++ ) {
                int xoffset = CHARACTER_WIDTH * i;
                keys[i] = GetKey( image, xoffset );
                accepted[i] = HalfWidthEmpty;
            }

            string[] halfWidthMatch = new string[textCount];
            string[] evenFullWidthMatch = new string[textCount / 2];
            string[] oddFullWidthMatch = new string[(textCount - 1) / 2];
            for( int i = 0; i < halfWidthMatch.Length; i++ ) {
                var key = keys[i];
                halfWidthMatch[i] = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
            }
            for( int i = 0; i < evenFullWidthMatch.Length; i++ ) {
                var key = keys[i * 2] + keys[i * 2 + 1];
                evenFullWidthMatch[i] = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
            }
            for( int i = 0; i < oddFullWidthMatch.Length; i++ ) {
                var key = keys[i * 2 + 1] + keys[i * 2 + 2];
                oddFullWidthMatch[i] = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
            }

            string[] resultArray = new string[textCount];
            for( int i = 0; i < oddFullWidthMatch.Length; i++ ) {
                if( oddFullWidthMatch[i] != "" && oddFullWidthMatch[i] != "  " ) {
                    resultArray[i * 2 + 1] = oddFullWidthMatch[i];
                    accepted[i * 2 + 1] = keys[i * 2 + 1];
                    accepted[i * 2 + 2] = keys[i * 2 + 2];
                    halfWidthMatch[i * 2 + 1] = "";
                    halfWidthMatch[i * 2 + 2] = "";
                    evenFullWidthMatch[i] = "";
                    if( i + 1 < evenFullWidthMatch.Length ) {
                        evenFullWidthMatch[i + 1] = "";
                    }
                }
            }
            for( int i = 0; i < evenFullWidthMatch.Length; i++ ) {
                if( evenFullWidthMatch[i] != "" ) {
                    resultArray[i * 2] = evenFullWidthMatch[i];
                    accepted[i * 2] = keys[i * 2];
                    accepted[i * 2 + 1] = keys[i * 2 + 1];
                    halfWidthMatch[i * 2] = "";
                    halfWidthMatch[i * 2 + 1] = "";
                }
            }
            for( int i = 0; i < halfWidthMatch.Length; i++ ) {
                if( halfWidthMatch[i] != "" ) {
                    resultArray[i] = halfWidthMatch[i];
                    accepted[i] = keys[i];
                }
            }

            // 認識に失敗した文字を列挙する
            for( int i = 0; i < textCount; i++ ) {
                if( keys[i] != accepted[i] ) {
                    throw new ApplicationException( "該当する文字が見つからなかった" );
                }
            }

            string result = "";
            for( int i = 0; i < resultArray.Length; i++ ) {
                result += resultArray[i];
            }
            return result.TrimEnd( ' ', '　' );
        }

        /// <summary>
        /// 画像の指定された色と合致するピクセルを黒に、それ以外のピクセルを白に変換した画像を作成する
        /// </summary>
        /// <param name="lineImage"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Bitmap CreateFilteredImage( Bitmap image, Color color ) {
            color = Color.FromArgb( 255, color );
            var result = (Bitmap)image.Clone();
            for( int y = 0; y < image.Height; y++ ) {
                for( int x = 0; x < image.Width; x++ ) {
                    var c = Color.FromArgb( 255, image.GetPixel( x, y ) );
                    if( c == color ) {
                        result.SetPixel( x, y, Color.FromArgb( 255, Color.Black ) );
                    } else {
                        result.SetPixel( x, y, Color.FromArgb( 255, Color.White ) );
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// 画像の指定された位置のビットマップを文字列に変換する．
        /// 長さ72のビット列 { (x,0), (x,1), ... , (x,11), (x+1,0), (x+1,1), ... , (x+5,10), (x+5,11) } を
        /// 16進数の文字列に変換している
        /// </summary>
        /// <param name="image"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        private static string GetKey( Bitmap image, int offsetX ) {
            List<bool> bits = new List<bool>();
            for( int x = 0; x < CHARACTER_WIDTH; x++ ) {
                for( int y = 0; y < CHARACTER_HEIGHT; y++ ) {
                    Color c = image.GetPixel( x + offsetX, y );
                    bits.Add( Color.FromArgb( 255, c ) == Color.FromArgb( 255, Color.Black ) );
                }
            }

            string key = "";
            for( int i = 0; i < bits.Count; i += 8 ) {
                byte b = 0;
                for( int j = 0; j < 8; j++ ) {
                    if( bits[i + j] ) {
                        b = (byte)(b | (0xff & (0x80 >> j)));
                    }
                }
                key += b.ToString( "x02" );
            }

            return key;
        }

        /// <summary>
        /// 指定した文字が半角文字かどうかを取得する
        /// </summary>
        /// <param name="c"></param>
        /// <returns></returns>
        public static bool IsHalfWidthCharacter( char c ) {
            return GetEncoder().GetByteCount( new char[] { c }, 0, 1, true ) == 1;
        }

        public static Font GetFont() {
            if( font == null ) {
                font = new Font( "ＭＳ ゴシック", 9 );
            }
            return font;
        }

        private static System.Text.Encoder GetEncoder() {
            if( encoder == null ) {
                encoder = Encoding.GetEncoding( "Shift_JIS" ).GetEncoder();
            }
            return encoder;
        }

        private static string GetCharByKey( string key, bool isFuzzy, bool ignoreWhiteSpace ) {
            var result = "";
            if( !ignoreWhiteSpace ) {
                if( key == HalfWidthEmpty ) {
                    return " ";
                }
                if( key == FullWidthEmpty ) {
                    return "  ";
                }
            }
            if( map.ContainsKey( key ) ) {
                result = new string( map[key], 1 );
            }
            if( result == "" && isFuzzy ) {
                if( dupulicatedKeys.ContainsKey( key ) ) {
                    result = new string( dupulicatedKeys[key], 1 );
                }
            }
            return result;
        }
    }
}
