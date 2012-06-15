using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace FEZTradeBot {
    public class ImageComparator {
        public static bool Compare( Bitmap image, Bitmap template ) {
            return Compare( image, template, 5 );
        }

        public static bool Compare( Bitmap image, Bitmap template, int thresholdDifferencePercentage ) {
            if( thresholdDifferencePercentage == 0 ) {
                return CompareStrict( image, template );
            } else {
                return CompareLoose( image, template, thresholdDifferencePercentage );
            }
        }

        /// <summary>
        /// 画像imageの内部に、templateと一致する部分を検索し、その左上の座標を返す。
        /// 見つからなかった場合、例外を投げる
        /// </summary>
        /// <param name="image"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static Point Find( Bitmap image, Bitmap template ) {
            var screenWidth = image.Width;
            var screenHeight = image.Height;
            var screen = GetColorArray( image );

            int maskWidth = template.Width;
            int maskHeight = template.Height;
            var mask = GetColorArray( template );
            var maskTransparentColor = mask[0, 0];

            int maxY = screenHeight - maskHeight;
            int maxX = screenWidth - maskWidth;
            var result = Point.Empty;
            try {
                Parallel.ForEach( PixelEnumerator.GetEnumerable( maxX, maxY ), pixel => {
                    if( CompareAt( screen, pixel.X, pixel.Y, mask, maskTransparentColor ) ) {
                        result = pixel;
                        throw new CommonException( "一致する箇所が見つかった。" );
                    }
                } );
            } catch( AggregateException e ) {
                return result;
            }
            throw new CommonException( "一致する部分を見つけられなかった" );
        }

        private static bool CompareAt( Color[,] screen, int offsetX, int offsetY, Color[,] mask, Color maskTransparentColor ) {
            int maskWidth = mask.GetUpperBound( 0 ) + 1;
            int maskHeight = mask.GetUpperBound( 1 ) + 1;
            for( int y = 0; y < maskHeight; y++ ) {
                for( int x = 0; x < maskWidth; x++ ) {
                    var maskColor = mask[x, y];
                    if( maskColor == maskTransparentColor ) {
                        continue;
                    }
                    var screenColor = screen[x + offsetX, y + offsetY];
                    if( maskColor != screenColor ) {
                        return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// 画像imageの内部に、templateと一致する部分を検索し、その左上の座標を返す。
        /// RGBの値それぞれが、templateよりtoleranceずれていた場合でも、そのピクセルはtemplateと同一とみなす
        /// 見つからなかった場合、例外を投げる
        /// </summary>
        /// <param name="image"></param>
        /// <param name="template"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static Point FindWithTolerance( Bitmap image, Bitmap template, int tolerance ) {
            var screenWidth = image.Width;
            var screenHeight = image.Height;
            var screen = GetColorArray( image );

            int maskWidth = template.Width;
            int maskHeight = template.Height;
            var mask = GetColorArray( template );
            var maskTransparentColor = mask[0, 0];

            int maxX = screenWidth - maskWidth;
            int maxY = screenHeight - maskHeight;
            var result = Point.Empty;
            try {
                Parallel.ForEach( PixelEnumerator.GetEnumerable( maxX, maxY ), pixel => {
                    if( CompareWithToleranceAt( screen, pixel.X, pixel.Y, mask, tolerance, maskTransparentColor ) ) {
                        result = pixel;
                        throw new CommonException( "一致する箇所が見つかった" );
                    }
                } );
            } catch( AggregateException e ) {
                return result;
            }
            throw new CommonException( "一致する部分を見つけられなかった" );
        }

        /// <summary>
        /// screenの(offsetX, offsetY)の位置に、画像maskがあるかどうかを調べる。ただし、ピクセルのRGB値の差がtolerance以下であれば、そのピクセルは同じとみなす
        /// </summary>
        /// <param name="screen"></param>
        /// <param name="offsetX"></param>
        /// <param name="offsetY"></param>
        /// <param name="mask"></param>
        /// <param name="tolerance"></param>
        /// <param name="maskTransparentColor"></param>
        /// <returns></returns>
        private static bool CompareWithToleranceAt( Color[,] screen, int offsetX, int offsetY, Color[,] mask, int tolerance, Color maskTransparentColor ) {
            int maskWidth = mask.GetUpperBound( 0 ) + 1;
            int maskHeight = mask.GetUpperBound( 1 ) + 1;
            for( int y = 0; y < maskHeight; y++ ) {
                for( int x = 0; x < maskWidth; x++ ) {
                    var maskColor = mask[x, y];
                    if( maskColor == maskTransparentColor ) {
                        continue;
                    }
                    var screenColor = screen[x + offsetX, y + offsetY];
                    if( Math.Abs( maskColor.R - screenColor.R ) > tolerance ||
                        Math.Abs( maskColor.G - screenColor.G ) > tolerance ||
                        Math.Abs( maskColor.B - screenColor.B ) > tolerance ) {
                        return false;
                    }
                }
            }
            return true;
        }

        private static Color[,] GetColorArray( Bitmap image ) {
            int width = image.Width;
            int height = image.Height;
            var result = new Color[width, height];
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ) {
                    result[x, y] = Color.FromArgb( 255, image.GetPixel( x, y ) );
                }
            }
            return result;
        }

        /// <summary>
        /// 画像を比較する．
        /// </summary>
        /// <param name="image">ゲーム画面からキャプチャした何らかの画像</param>
        /// <param name="template">imageと同じサイズの，比較対象の画像．左上のピクセルをマスク色として利用する</param>
        /// <param name="thresholdDifferencePercentage">色に違いのあるピクセルの割合がパーセンテージ以下であれば，画像は同じと判断する</param>
        /// <returns></returns>
        private static bool CompareLoose( Bitmap image, Bitmap template, int thresholdDifferencePercentage ) {
            Color maskColor = template.GetPixel( 0, 0 );

            int totalPixels = 0;
            int matchPixels = 0;

            for( int y = 0; y < template.Height; y++ ) {
                for( int x = 0; x < template.Width; x++ ) {
                    Color colorOfMask = template.GetPixel( x, y );
                    if( colorOfMask != maskColor ) {
                        Color colorOfActual = image.GetPixel( x, y );
                        totalPixels++;
                        if( colorOfActual == colorOfMask ) {
                            matchPixels++;
                        }
                    }
                }
            }

            // アイコン画像テンプレートとの差があるピクセルの個数が，
            // 全体のピクセル数の指定割合以下であれば，テンプレートと同じとみなす
            double diffPercentage = (totalPixels - matchPixels) * 100.0 / totalPixels;
            if( totalPixels != matchPixels && diffPercentage <= 10.0 ) {
                WriteLog( image, template, totalPixels, matchPixels );
            }
            if( totalPixels == matchPixels ) {
                return true;
            } else {
                return diffPercentage <= thresholdDifferencePercentage;
            }
        }

        public static bool CompareStrict( Bitmap image, Bitmap template, Color maskColor ) {
            for( int y = 0; y < template.Height; y++ ) {
                for( int x = 0; x < template.Width; x++ ) {
                    Color colorOfMask = template.GetPixel( x, y );
                    if( colorOfMask != maskColor ) {
                        Color colorOfActual = image.GetPixel( x, y );
                        if( colorOfActual != colorOfMask ) {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// 1ピクセルの差もなく，2つの画像が同じかどうかを調べる．
        /// </summary>
        /// <param name="image"></param>
        /// <param name="template"></param>
        /// <returns></returns>
        public static bool CompareStrict( Bitmap image, Bitmap template ) {
            Color maskColor = template.GetPixel( 0, 0 );
            return CompareStrict( image, template, maskColor );
        }

        /// <summary>
        /// 画像の比較結果をログに残す
        /// </summary>
        /// <param name="image"></param>
        /// <param name="template"></param>
        private static void WriteLog( Bitmap image, Bitmap template, int totalPixels, int matchPixels ) {
            // ディレクトリ作成
            string executeDirectory = Path.GetDirectoryName( Application.ExecutablePath );
            string directoryName = Path.Combine( executeDirectory, "image_comparator_log" );
            if ( !Directory.Exists( directoryName ) ) {
                Directory.CreateDirectory( directoryName );
            }

            var now = DateTime.Now;
            string date = now.ToString( "yyyy-MM-dd" );
            string subDirectory = Path.Combine( directoryName, date );
            if ( !Directory.Exists( subDirectory ) ) {
                Directory.CreateDirectory( subDirectory );
            }

            string time = DateTime.Now.Ticks.ToString();
            image.Save( Path.Combine( subDirectory, time + "_image.png" ), ImageFormat.Png );
            template.Save( Path.Combine( subDirectory, time + "_template.png" ), ImageFormat.Png );

            double diffPercentage = (totalPixels - matchPixels) * 100.0 / totalPixels;
            using ( StreamWriter writer = new StreamWriter( Path.Combine( subDirectory, time + "_result.txt" ) ) ) {
                writer.WriteLine( "totalPixels=" + totalPixels );
                writer.WriteLine( "matchPixels=" + matchPixels );
                writer.WriteLine( "diffPercentage=" + diffPercentage + "%" );
            }
        }
    }
}
