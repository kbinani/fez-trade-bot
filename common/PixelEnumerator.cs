using System;
using System.Collections.Generic;
using System.Drawing;

namespace FEZTradeBot {
    /// <summary>
    /// 画像などのピクセルを順に返す反復子
    /// </summary>
    public class PixelEnumerator {
        public static IEnumerable<Point> GetEnumerable( int width, int height ) {
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ) {
                    yield return new Point( x, y );
                }
            }
        }
    }
}
