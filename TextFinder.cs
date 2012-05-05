using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace com.github.kbinani.feztradebot {
    static class TextFinder {
        private const int CHARACTER_WIDTH = 6;
        private const int CHARACTER_HEIGHT = 12;
        private static readonly string FullWidthEmpty = new string( '0', 36 );
        private static readonly string HalfWidthEmpty = new string( '0', 18 );

        private static Dictionary<string, char> map;
        private static System.Text.Encoder encoder;
        private static Dictionary<string, char> dupulicatedKeys;

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
            for( int i = 0; i < textCount; i++ ) {
                int xoffset = CHARACTER_WIDTH * i;
                keys[i] = GetKey( image, xoffset );
            }

            string[] halfWidthMatch = new string[textCount];
            string[] evenFullWidthMatch = new string[textCount / 2];
            string[] oddFullWidthMatch = new string[(textCount - 1) / 2];
            for( int i = 0; i < halfWidthMatch.Length; i++ ) {
                var key = keys[i];
                var c = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
                if( c == '\0' ) {
                    halfWidthMatch[i] = "";
                } else {
                    halfWidthMatch[i] = new string( c, 1 );
                }
            }
            for( int i = 0; i < evenFullWidthMatch.Length; i++ ) {
                var key = keys[i * 2] + keys[i * 2 + 1];
                var c = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
                if( c == '\0' ) {
                    evenFullWidthMatch[i] = "";
                } else {
                    evenFullWidthMatch[i] = new string( c, 1 );
                }
            }
            for( int i = 0; i < oddFullWidthMatch.Length; i++ ) {
                var key = keys[i * 2 + 1] + keys[i * 2 + 2];
                var c = GetCharByKey( key, isFuzzy, ignoreWhiteSpace );
                if( c == '\0' ) {
                    oddFullWidthMatch[i] = "";
                } else {
                    oddFullWidthMatch[i] = new string( c, 1 );
                }
            }

            string[] resultArray = new string[textCount];
            for( int i = 0; i < oddFullWidthMatch.Length; i++ ) {
                if( oddFullWidthMatch[i] != "" ) {
                    resultArray[i * 2 + 1] = oddFullWidthMatch[i];
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
                    halfWidthMatch[i * 2] = "";
                    halfWidthMatch[i * 2 + 1] = "";
                }
            }
            for( int i = 0; i < halfWidthMatch.Length; i++ ) {
                if( halfWidthMatch[i] != "" ) {
                    resultArray[i] = halfWidthMatch[i];
                }
            }

            // 認識に失敗した文字を列挙する
            var failed = new List<string>();
            for( int i = 0; i < resultArray.Length; i++ ) {
                if( resultArray[i] == "" ) {
                    var evenIndex = (i - 1) / 2;
                    if( 0 <= evenIndex && evenFullWidthMatch[evenIndex] == "" ) {
                        var key = keys[evenIndex * 2] + keys[evenIndex * 2 + 1];
                        if( key != FullWidthEmpty ) {
                            failed.Add( key );
                            continue;
                        }
                    }
                    var oddIndex = i / 2 - 1;
                    if( 0 <= oddIndex && oddFullWidthMatch[oddIndex] == "" ) {
                        var key = keys[oddIndex * 2 + 1] + keys[oddIndex * 2 + 2];
                        if( key != FullWidthEmpty ) {
                            failed.Add( key );
                            continue;
                        }
                    }
                    if( keys[i] != HalfWidthEmpty ) {
                        failed.Add( keys[i] );
                    }
                }
            }
            foreach( var failedKey in failed ) {
                WriteLog( failedKey );
            }
            if( 0 < failed.Count ) {
                throw new ApplicationException( "該当する文字が見つからなかった" );
            }

            string result = "";
            for( int i = 0; i < resultArray.Length; i++ ) {
                result += resultArray[i];
            }
            return result.TrimEnd( ' ', '　' );
        }
        /*
        public static string _Find( Bitmap image, bool isFuzzy, bool ignoreWhiteSpace ) {
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
                        if( ignoreWhiteSpace ) {
                            break;
                        } else {
                            result += "  ";
                            searchKey = "";
                        }
                    } else {
                        WriteLog( searchKey );
                        throw new ApplicationException( "該当する文字が見つからなかった: searchKey=" + searchKey );
                    }
                }
            }

            return result;
        }*/

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

        private static System.Text.Encoder GetEncoder() {
            if( encoder == null ) {
                encoder = Encoding.GetEncoding( "Shift_JIS" ).GetEncoder();
            }
            return encoder;
        }

        private static char GetCharByKey( string key, bool isFuzzy, bool ignoreWhiteSpace ) {
            var result = '\0';
            if( !ignoreWhiteSpace ) {
                if( key == HalfWidthEmpty ) {
                    return ' ';
                }
                if( key == FullWidthEmpty ) {
                    return '　';
                }
            }
            if( map.ContainsKey( key ) ) {
                result = map[key];
            }
            if( result == '\0' && isFuzzy ) {
                if( dupulicatedKeys.ContainsKey( key ) ) {
                    result = dupulicatedKeys[key];
                }
            }
            return result;
        }

        private static void WriteLog( string searchKey ){
            string directory = Path.Combine( Path.GetDirectoryName( Application.ExecutablePath ), "text_finder" );
            if( !Directory.Exists( directory ) ) {
                Directory.CreateDirectory( directory );
            }
            string imagePath = Path.Combine( directory, searchKey + ".png" );
            if( File.Exists( imagePath ) ) {
                return;
            }

            var byteCount = searchKey.Length / 2;
            bool[] bits = new bool[byteCount * 8];
            for( int byteIndex = 0; byteIndex < byteCount; byteIndex++ ) {
                var hexString = searchKey.Substring( byteIndex * 2, 2 );
                byte b = Convert.ToByte( hexString, 16 );
                for( int bitIndex = 0; bitIndex < 8; bitIndex++ ) {
                    int i = byteIndex * 8 + bitIndex;
                    byte mask = (byte)((0x80 >> bitIndex) & 0xFF);
                    bits[i] = (b & mask) == mask;
                }
            }

            const int height = 12;
            var width = bits.Length / height;
            var image = new Bitmap( width, height, PixelFormat.Format24bppRgb );
            for( int x = 0; x < width; x++ ) {
                for( int y = 0; y < height; y++ ) {
                    int bitIndex = x * height + y;
                    var c = bits[bitIndex] ? Color.Black : Color.White;
                    image.SetPixel( x, y, c );
                }
            }

            image.Save( imagePath, ImageFormat.Png );
        }
    }
}
