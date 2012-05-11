using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Collections.Generic;

namespace FEZTradeBot {
    /// <summary>
    /// 自キャラの現在位置を検出する
    /// </summary>
    class CurrentPositionDetector {
        private Point mapHeaderPosition;

        public CurrentPositionDetector( Point mapHeaderPosition ){
            this.mapHeaderPosition = mapHeaderPosition;
        }

        public PointF Detect( Bitmap screenShot ) {
            var mapImage = (Bitmap)screenShot.Clone( FEZWindow.GetMapGeometry( mapHeaderPosition ), screenShot.PixelFormat );
            var markerPosition = ImageComparator.FindWithTolerance( mapImage, Resource.map_azelwood_marker_a, 50 );
            int x = 121 - markerPosition.X;
            int y = 17 - markerPosition.Y;
            return new PointF( x, y );
        }
    }
}
