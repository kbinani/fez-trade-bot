using System;
using System.Drawing;
using System.IO;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace com.github.kbinani.feztradenotify {
    class ImageComparator {
        /// <summary>
        /// 画像を比較する．
        /// </summary>
        /// <param name="image">ゲーム画面からキャプチャした何らかの画像</param>
        /// <param name="template">imageと同じサイズの，比較対象の画像．左上のピクセルをマスク色として利用する</param>
        /// <returns></returns>
        public static bool Compare( Bitmap image, Bitmap template ) {
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

            if ( totalPixels != matchPixels ) {
                WriteLog( image, template, totalPixels, matchPixels );
            }

            // アイコン画像テンプレートとの差があるピクセルの個数が，
            // 全体のピクセル数の 5% 以下であれば，テンプレートと同じとみなす
            double diffPercentage = (totalPixels - matchPixels) * 100.0 / totalPixels;
            return diffPercentage <= 5.0;
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
