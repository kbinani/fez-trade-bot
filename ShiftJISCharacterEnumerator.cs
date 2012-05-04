using System;
using System.Collections.Generic;
using System.Text;

namespace com.github.kbinani.feztradebot {
    /// <summary>
    /// Shift_JISの範囲内の文字を順に返す反復子
    /// </summary>
    static class ShiftJISCharacterEnumerator {
        public static IEnumerable<char> GetEnumerator() {
            var characters = new Dictionary<char, int>();
            // ASCII
            for( byte b = 0x21; b <= 0x7E; b++ ) {
                characters.Add( (char)b, 1 );
                yield return (char)b;
            }
            for( byte b = 0xA1; b <= 0xDF; b++ ) {
                characters.Add( (char)b, 1 );
                yield return (char)b;
            }

            // 0x81～0x9F
            var encoding = Encoding.GetEncoding( "Shift_JIS" );
            byte[] buffer = new byte[2];
            for( byte first = 0x81; first <= 0x9F; first++ ) {
                buffer[0] = first;
                for( byte second = 0x40; second <= 0x7E; second++ ) {
                    buffer[1] = second;
                    var c = encoding.GetChars( buffer, 0, 2 )[0];
                    if( !characters.ContainsKey( c ) ) {
                        characters.Add( c, 1 );
                        yield return c;
                    }
                }
                for( byte second = 0x80; second <= 0xFC; second++ ) {
                    buffer[1] = second;
                    var c = encoding.GetChars( buffer, 0, 2 )[0];
                    if( !characters.ContainsKey( c ) ) {
                        characters.Add( c, 1 );
                        yield return c;
                    }
                }
            }
            for( byte first = 0xE0; first <= 0xEF; first++ ) {
                buffer[0] = first;
                for( byte second = 0x40; second <= 0x7E; second++ ) {
                    buffer[1] = second;
                    var c = encoding.GetChars( buffer, 0, 2 )[0];
                    if( !characters.ContainsKey( c ) ) {
                        characters.Add( c, 1 );
                        yield return c;
                    }
                }
                for( byte second = 0x80; second <= 0xFC; second++ ) {
                    buffer[1] = second;
                    var c = encoding.GetChars( buffer, 0, 2 )[0];
                    if( !characters.ContainsKey( c ) ) {
                        characters.Add( c, 1 );
                        yield return c;
                    }
                }
            }
        }
    }
}
