using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;

namespace FEZTradeBot {
    /// <summary>
    /// 狩り対象のMOBを攻撃するための操作を行う
    /// </summary>
    class AttackWorker {
        private bool isRunning;
        private FEZWindow window;

        public AttackWorker( FEZWindow window ) {
            this.window = window;
            this.isRunning = false;
        }

        public void Stop() {
            isRunning = false;
        }

        public void Resume() {
            isRunning = true;
        }

        public void Run() {
            while( true ) {
                if( isRunning ) {
                    var position = DetectMobPosition();
                    if( position != Point.Empty ) {
                        window.Click( position );
                    }
                }
                Thread.Sleep( TimeSpan.FromMilliseconds( 200 ) );
            }
        }

        /// <summary>
        /// 画面上の、MOBが表示されている位置を検出する。
        /// カメラの視線は、地面を見ている前提で、画面の中にMOBの体色と同じ色が一定割合で表示されていれば、
        /// そこにMOBがいると判定する
        /// </summary>
        /// <returns>MOBが表示されている位置。MOBが見つからなければPoint.Emptyを返す</returns>
        private Point DetectMobPosition() {
            // 画面をgridSize x gridSizeピクセルのグリッドに分割する
            const int gridSize = 50;
            int horizontalGridCount = ((int)Math.Ceiling( (window.Width / 2.0) / gridSize )) * 2;
            int verticalGridCount = ((int)Math.Ceiling( (window.Height / 2.0) / gridSize )) * 2;

            // 各グリッドの全ピクセルのうち、MOBの体色のピクセルが占める割合
            double[,] percentage = new double[horizontalGridCount, verticalGridCount];
            for( int y = 0; y < verticalGridCount; y++ ) {
                for( int x = 0; x < horizontalGridCount; x++ ) {
                    percentage[x, y] = 0.0;
                }
            }

            // 各グリッドについて、MOBの体色のピクセルを調べる
            var screenImage = window.CaptureWindow();
            int width = screenImage.Width;
            int height = screenImage.Height;
            Color[,] screen = new Color[width, height];
            for( int y = 0; y < height; y++ ) {
                for( int x = 0; x < width; x++ ){
                    screen[x, y] = screenImage.GetPixel( x, y );
                }
            }

            int centerX = width / 2;
            int centerY = height / 2;
            Parallel.ForEach( PixelEnumerator.GetEnumerable( horizontalGridCount, verticalGridCount ), grid => {
                int left = centerX - ((horizontalGridCount / 2) - grid.X) * gridSize;
                int top = centerY - ((verticalGridCount / 2) - grid.Y) * gridSize;
                int match = 0;

                int startX = left < 0 ? 0 : left;
                int endX = width < left + gridSize ? width : left + gridSize;
                int startY = top < 0 ? 0 : top;
                int endY = height < top + gridSize ? height : top + gridSize;

                int total = 0;
                for( int y = startY; y < endY; y++ ) {
                    for( int x = startX; x < endX; x++ ) {
                        var color = screen[x, y];
                        if( color.R == 0 &&
                            3 <= color.G && color.G <= 148 &&
                            18 <= color.B && color.B <= 159 ) {
                            match++;
                        }
                        total++;
                    }
                }
                lock( percentage ) {
                    percentage[grid.X, grid.Y] = match / (double)total * 100.0;
                }
            } );

            // MOBの体色の割合が30%を超えていて、かつ最も割合の大きいグリッドの位置を、MOBの位置とみなす
            int gridX = 0;
            int gridY = 0;
            double max = 0.0;
            for( int x = 0; x < horizontalGridCount; x++ ) {
                for( int y = 0; y < verticalGridCount; y++ ) {
                    if( percentage[x, y] > 30 && max < percentage[x, y] ) {
                        max = percentage[x, y];
                        gridX = x;
                        gridY = y;
                    }
                }
            }
            if( max == 0.0 ) {
                return Point.Empty;
            } else {
                int left = centerX - ((horizontalGridCount / 2) - gridX) * gridSize;
                int top = centerY - ((verticalGridCount / 2) - gridY) * gridSize;
                return new Point( left + gridSize / 2, top + gridSize / 2 );
            }
        }
    }
}
