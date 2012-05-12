using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;

namespace FEZTradeBot {
    /// <summary>
    /// 地図画像から、不透明な部分を抽出するタスク
    /// mapディレクトリ下のファイルを読み込み、不透明でない部分をマスク塗りつぶす
    /// </summary>
    class MapCaptureTask {
        private string id;
        private Rectangle mapImageGeometry;

        public MapCaptureTask( Rectangle mapImageGeometry ) {
            this.mapImageGeometry = mapImageGeometry;
            Reset();
        }

        public void Reset() {
            id = Path.GetRandomFileName();
        }

        public void Run( Bitmap screenShot ) {
            if( !Directory.Exists( "map" ) ) {
                Directory.CreateDirectory( "map" );
            }
            Bitmap mask = null;
            var maskFileName = Path.Combine( "map", id + ".png" );
            if( File.Exists( maskFileName ) ) {
                using( var stream = new FileStream( maskFileName, FileMode.Open, FileAccess.Read ) ) {
                    mask = new Bitmap( stream );
                }
            }

            var mapImage = screenShot.Clone( mapImageGeometry, screenShot.PixelFormat );
            var maskColor = Color.FromArgb( 255, 255, 0, 255 );
            if( mask == null ) {
                mask = mapImage;
                mask.SetPixel( 0, 0, maskColor );
                mask.Save( maskFileName, ImageFormat.Png );
            } else {
                for( int y = 0; y < mapImage.Height; y++ ) {
                    for( int x = 0; x < mapImage.Width; x++ ) {
                        var cMask = Color.FromArgb( 255, mask.GetPixel( x, y ) );
                        var cMap = Color.FromArgb( 255, mapImage.GetPixel( x, y ) );
                        if( cMask != maskColor && cMask != cMap ) {
                            mask.SetPixel( x, y, maskColor );
                        }
                    }
                }
                mask.Save( maskFileName, ImageFormat.Png );
            }
        }
    }
}
