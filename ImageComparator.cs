using System.Drawing;

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

            // アイコン画像テンプレートとの差があるピクセルの個数が，
            // 全体のピクセル数の 5% 以下であれば，テンプレートと同じとみなす
            double diffPercentage = (totalPixels - matchPixels) * 100.0 / totalPixels;
            return diffPercentage <= 5.0;
        }
    }
}
