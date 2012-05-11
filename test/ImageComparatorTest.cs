using System;
using FEZTradeBot;
using NUnit.Framework;
using System.Drawing;

namespace FEZTradeBotTest {
    class ImageComparatorStub : ImageComparator {
        new public static bool CompareStrict( Bitmap image, Bitmap template ) {
            return ImageComparator.CompareStrict( image, template );
        }
    }

    class ImageComparatorTest {
        [TestCase]
        public static void FindWithTolerance() {
            var point = ImageComparator.FindWithTolerance( Resource.ImageComparator_Image, Resource.ImageComparator_Template_Gray128, 10 );
            Assert.AreEqual( new Point( 9, 10 ), point );

            try {
                ImageComparator.FindWithTolerance( Resource.ImageComparator_Image, Resource.ImageComparator_Template_Gray128, 9 );
                Assert.Fail( "例外が投げられるはずやった" );
            } catch( FEZTradeBot.ApplicationException e ) {
                Assert.AreEqual( "一致する部分を見つけられなかった", e.Message );
            } catch( Exception e ) {
                Console.WriteLine( e.Message );
                Assert.Fail( "予期しない例外がキタ" );
            }
        }

        [TestCase]
        public static void CompareStrict() {
            int width = 50;
            int height = 50;
            var image = new Bitmap( width, height );
            using( var g = Graphics.FromImage( image ) ){
                g.FillRectangle( new SolidBrush( Color.FromArgb( 255, Color.Red ) ), new Rectangle( 0, 0, width, height ) );
            }
            var template = (Bitmap)image.Clone();
            template.SetPixel( 0, 0, Color.FromArgb( 255, Color.Magenta ) );

            var imageWithDiff = (Bitmap)image.Clone();
            imageWithDiff.SetPixel( 1, 1, Color.FromArgb( 244, Color.Blue ) );

            Assert.True( ImageComparatorStub.CompareStrict( image, template ) );
            Assert.False( ImageComparatorStub.CompareStrict( imageWithDiff, template ) );
        }

        [TestCase]
        public static void Find() {
            var actual = ImageComparator.Find( Resource.ImageComparator_Image, Resource.ImageComparator_Template );
            Assert.AreEqual( new Point( 9, 10 ), actual );

            try {
                ImageComparator.Find( Resource.ImageComparator_Image, Resource.ImageComparator_Template_Gray128 );
                Assert.Fail( "例外となるはずだったのに" );
            } catch( FEZTradeBot.ApplicationException e ) {
                Assert.AreEqual( "一致する部分を見つけられなかった", e.Message );
            } catch {
                Assert.Fail( "想定してない例外" );
            }
        }
    }
}
