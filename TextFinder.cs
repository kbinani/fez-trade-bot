using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;

namespace com.github.kbinani.feztradebot {
    static class TextFinder {
        private const int CHARACTER_WIDTH = 6;
        private const int CHARACTER_HEIGHT = 12;
        private static readonly string FullWidthEmpty = new string( '0', 36 );
        private static readonly string HalfWidthEmpty = new string( '0', 18 );

        private static Dictionary<string, char> map;
        private static System.Text.Encoder encoder;
        private static Dictionary<char, string> dupulicatedKeys;

        /// <summary>
        /// 画像から文字を探す。画像の高さは12ピクセルである必要がある。描かれている文字は座標(0,0)から始まり、
        /// 文字の高さも12ピクセルでなければならない。
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        public static string Find( Bitmap image ) {
            return FindImplementation( image, false );
        }

        public static string FuzzyFind( Bitmap image ) {
            return FindImplementation( image, true );
        }

        public static void Initialize() {
            map = new Dictionary<string, char>();
            dupulicatedKeys = new Dictionary<char, string>();

            var image = new Bitmap( 12, 12, PixelFormat.Format24bppRgb );
            using( var g = Graphics.FromImage( image ) ) {
                var font = new Font( "ＭＳ ゴシック", 9 );
                foreach( var c in ShiftJISCharacterEnumerator.GetEnumerator() ) {
                    var isHalfWidth = IsHalfWidthCharacter( c );
                    g.FillRectangle( Brushes.White, 0, 0, 12, 12 );
                    if( isHalfWidth ) {
                        g.DrawString( new string( c, 1 ), font, Brushes.Black, -2, 0 );
                    } else {
                        g.DrawString( new string( c, 1 ), font, Brushes.Black, -2, 0 );
                    }

                    string key = GetKey( image, 0 );
                    if( !isHalfWidth ) {
                        key += GetKey( image, CHARACTER_WIDTH );
                    }

                    if( key == HalfWidthEmpty || key == FullWidthEmpty ) {
                        continue;
                    }

                    if( map.ContainsKey( key ) ) {
                        dupulicatedKeys.Add( map[key], key );
                        dupulicatedKeys.Add( c, key );
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

        private static string FindImplementation( Bitmap image, bool isFuzzy ) {
            if( map == null || dupulicatedKeys == null ) {
                Initialize();
            }

            string result = "";
            int textCount = image.Width / CHARACTER_WIDTH;
            string searchKey = "";
            for( int i = 0; i < textCount; i++ ) {
                int xoffset = CHARACTER_WIDTH * i;
                string key = GetKey( image, xoffset );

                if( searchKey == "" ) {
                    var c = GetCharByKey( key, isFuzzy );
                    if( c == '\0' ) {
                        searchKey = key;
                    } else {
                        result += new string( c, 1 );
                    }
                } else {
                    searchKey += key;
                    var c = GetCharByKey( searchKey, isFuzzy );
                    if( c != '\0' ) {
                        result += new string( c, 1 );
                        searchKey = "";
                    } else if( searchKey == FullWidthEmpty ) {
                        break;
                    } else {
                        throw new ApplicationException( "該当する文字が見つからなかった" );
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
        private static bool IsHalfWidthCharacter( char c ) {
            return GetEncoder().GetByteCount( new char[] { c }, 0, 1, true ) == 1;
        }

        private static System.Text.Encoder GetEncoder() {
            if( encoder == null ) {
                encoder = Encoding.GetEncoding( "Shift_JIS" ).GetEncoder();
            }
            return encoder;
        }

        private static char GetCharByKey( string key, bool isFuzzy ) {
            var result = '\0';
            if( map.ContainsKey( key ) ) {
                result = map[key];
            }
            if( result == '\0' && isFuzzy ) {
                foreach( var c in dupulicatedKeys.Keys ) {
                    if( dupulicatedKeys[c] == key ) {
                        result = c;
                        break;
                    }
                }
            }
            return result;
        }
    }
}
