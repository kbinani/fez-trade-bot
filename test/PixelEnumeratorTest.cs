using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Collections.Generic;
using NUnit.Framework;
using FEZTradeBot;

namespace FEZTradeBotTest {
    class PixelEnumeratorTest {
        [TestCase]
        public static void Enumerate() {
            int width = 3;
            int height = 2;
            var pixels = PixelEnumerator.GetEnumerable( width, height ).GetEnumerator();
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ) {
                    pixels.MoveNext();
                    Assert.AreEqual( new Point( x, y ), pixels.Current );
                }
            }
            Assert.False( pixels.MoveNext() );
        }
    }
}
